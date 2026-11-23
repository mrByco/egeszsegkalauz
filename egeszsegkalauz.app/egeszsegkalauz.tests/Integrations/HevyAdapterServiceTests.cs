using System.Net;
using System.Security.Cryptography;
using System.Text;
using egeszsegkalauz.integrations.Common;
using egeszsegkalauz.integrations.Hevy;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace egeszsegkalauz.tests.Integrations;

public class HevyAdapterServiceTests
{
    private const string Secret = "client-secret";

    private readonly List<(HttpRequestMessage Request, string? Body)> requests = [];
    private Dictionary<string, Func<HttpResponseMessage>> routes = [];
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 22, 10, 0, 0, TimeSpan.Zero));

    [SetUp]
    public void Reset()
    {
        requests.Clear();
        routes = [];
    }

    [Test]
    public void TheAuthorizationUrlCarriesClientRedirectAndState()
    {
        var url = CreateAdapter().BuildAuthorizationUrl("a b/c");

        Assert.That(url, Is.EqualTo("https://hevy.test/oauth/authorize?client_id=client-id"
                                    + "&redirect_uri=http%3A%2F%2Flocalhost%3A4200%2Fintegraciok%2Fhevy%2Fcallback&state=a%20b%2Fc"));
    }

    [Test]
    public async Task ExchangesTheCodeForTokens()
    {
        routes["/api/token"] = () => Json("""
            {"access_token":"at","token_type":"Bearer","expires_in":3600,"refresh_token":"rt",
             "user_id":"u-1","username":"jane_lifts","profile_pic":null}
            """);

        var tokens = await CreateAdapter().ExchangeCodeAsync("code-1", CancellationToken.None);

        Assert.That((tokens.AccessToken, tokens.RefreshToken, tokens.ExternalUserId, tokens.AccountName),
            Is.EqualTo(("at", "rt", (string?)"u-1", (string?)"jane_lifts")));
        Assert.That(tokens.ExpiresAtUtc, Is.EqualTo(time.GetUtcNow().UtcDateTime.AddHours(1)));
        var body = requests.Single().Body;
        Assert.That(body, Does.Contain("grant_type=authorization_code").And.Contain("code=code-1")
            .And.Contain("client_id=client-id").And.Contain("client_secret=client-secret"));
    }

    [Test]
    public async Task ARejectedCodeIsReportedAsSuch()
    {
        routes["/api/token"] = () => new HttpResponseMessage(HttpStatusCode.BadRequest);

        Assert.That(await CatchAsync(() => CreateAdapter().ExchangeCodeAsync("old", CancellationToken.None)),
            Is.InstanceOf<IntegrationKeyRejectedException>());
    }

    [Test]
    public async Task AFullSyncPagesThroughWorkoutsTemplatesAndMeasurements()
    {
        routes["/v1/workouts?page=1&pageSize=10"] = () => Json($$"""{"page":1,"page_count":2,"workouts":[{{HevySamples.Workout("w1")}}]}""");
        routes["/v1/workouts?page=2&pageSize=10"] = () => Json($$"""{"page":2,"page_count":2,"workouts":[{{HevySamples.Workout("w2")}}]}""");
        routes["/v1/exercise_templates?page=1&pageSize=100"] = () =>
            Json($$"""{"page":1,"page_count":1,"exercise_templates":[{{HevySamples.Template}}]}""");
        routes["/v1/body_measurements?page=1&pageSize=10"] = () =>
            Json($$"""{"page":1,"page_count":1,"body_measurements":[{{HevySamples.BodyMeasurement}}]}""");
        routes["/v1/user/info"] = () => Json("""{"data":{"id":"u-1","weight_unit":"kg"}}""");

        var records = (await CreateAdapter().SyncAsync("token", null, CancellationToken.None).ToListAsync())
            .SelectMany(x => x.Records)
            .ToList();

        var workouts = records.Where(x => x.Endpoint == HevyAdapterService.WorkoutEndpoint).ToList();
        Assert.That(workouts.Select(x => x.Key), Is.EqualTo(new[] { "w1", "w2" }));
        Assert.That(workouts[0].Date, Is.EqualTo(new DateOnly(2026, 11, 18)));
        Assert.That(workouts[0].Json, Does.Contain("\"rpe\":8.5"));
        Assert.That(records.Single(x => x.Endpoint == HevyAdapterService.ExerciseTemplateEndpoint).Key, Is.EqualTo("T1"));
        Assert.That(records.Single(x => x.Endpoint == HevyAdapterService.BodyMeasurementEndpoint).Key, Is.EqualTo("2026-11-20"));
        Assert.That(records.Single(x => x.Endpoint == HevyAdapterService.UserInfoEndpoint).Key, Is.EqualTo("profile"));
        Assert.That(requests.All(x => x.Request.Headers.Authorization?.ToString() == "Bearer token"), Is.True);
    }

    [Test]
    public async Task AnIncrementalSyncReadsEventsSinceTheCursor()
    {
        routes["/v1/workouts/events?page=1&pageSize=10&since=2026-11-21T09%3A00%3A00.0000000Z"] = () => Json($$"""
            {"page":1,"page_count":1,"events":[
              {"type":"updated","workout":{{HevySamples.Workout("w1")}}},
              {"type":"deleted","id":"w9","deleted_at":"2026-11-21T10:00:00Z"}]}
            """);

        var chunks = await CreateAdapter()
            .SyncAsync("token", new DateTime(2026, 11, 21, 9, 0, 0, DateTimeKind.Utc), CancellationToken.None)
            .ToListAsync();

        Assert.That(chunks.SelectMany(x => x.Records).Where(x => x.Endpoint == "workout").Select(x => x.Key), Is.EqualTo(new[] { "w1" }));
        Assert.That(chunks.SelectMany(x => x.Deleted).Select(x => (x.Endpoint, x.Key)), Is.EqualTo(new[] { ("workout", "w9") }));
        Assert.That(requests.Any(x => x.Request.RequestUri?.AbsolutePath == "/v1/workouts"), Is.False);
    }

    [Test]
    public async Task RejectedTokensAndRateLimitsAreReported()
    {
        var notification = new WebhookNotification { Type = "workout.created", ExternalUserId = "u-1", ItemId = "w1" };
        routes["/v1/workouts/w1"] = () => new HttpResponseMessage(HttpStatusCode.Unauthorized);
        Assert.That(await CatchAsync(() => CreateAdapter().FetchAsync("token", notification, CancellationToken.None)),
            Is.InstanceOf<IntegrationKeyRejectedException>());

        routes["/v1/workouts/w1"] = () => new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        var error = await CatchAsync(() => CreateAdapter().FetchAsync("token", notification, CancellationToken.None));
        Assert.That(error, Is.InstanceOf<IntegrationRateLimitedException>());
        Assert.That(((IntegrationRateLimitedException)error).Pause, Is.EqualTo(TimeSpan.FromMinutes(5)));
    }

    [Test]
    public async Task AWorkoutThatNoLongerExistsComesBackAsDeleted()
    {
        routes["/v1/workouts/w1"] = () => new HttpResponseMessage(HttpStatusCode.NotFound);

        var chunk = await CreateAdapter().FetchAsync("token",
            new WebhookNotification { Type = "workout.created", ExternalUserId = "u-1", ItemId = "w1" }, CancellationToken.None);

        Assert.That(chunk.Deleted.Select(x => x.Key), Is.EqualTo(new[] { "w1" }));
    }

    [Test]
    public void WebhookSignaturesAreCheckedAgainstTheClientSecret()
    {
        const string body = """{"type":"workout.created","workoutId":"w1","userId":"u-1"}""";
        var signature = "sha256=" + Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(body)));
        var adapter = CreateAdapter();

        Assert.That(adapter.IsSignatureValid(body, signature), Is.True);
        Assert.That(adapter.IsSignatureValid(body + " ", signature), Is.False);
        Assert.That(adapter.IsSignatureValid(body, null), Is.False);
        var notification = adapter.Parse(body);
        Assert.That((notification?.ExternalUserId, notification?.ItemId), Is.EqualTo(((string?)"u-1", (string?)"w1")));
    }

    private HevyAdapterService CreateAdapter()
    {
        var handler = new StubHandler(request =>
        {
            requests.Add((request, request.Content?.ReadAsStringAsync().Result));
            var key = request.RequestUri?.Host == "hevy.test" ? request.RequestUri.AbsolutePath : request.RequestUri?.PathAndQuery ?? "";
            return routes.TryGetValue(key, out var respond) ? respond() : new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var options = Options.Create(new HevyOptions
        {
            ApiBaseUrl = new Uri("https://api.hevy.example"),
            AuthorizeUrl = new Uri("https://hevy.test/oauth/authorize"),
            TokenUrl = new Uri("https://hevy.test/api/token"),
            ClientId = "client-id",
            ClientSecret = Secret,
            RedirectUri = "http://localhost:4200/integraciok/hevy/callback",
            RequestInterval = TimeSpan.Zero,
        });
        return new HevyAdapterService(new HttpClient(handler) { BaseAddress = options.Value.ApiBaseUrl }, options, time);
    }

    private static HttpResponseMessage Json(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private static async Task<Exception> CatchAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception e)
        {
            return e;
        }

        throw new AssertionException("Expected an exception.");
    }

    private class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            this.respond = respond;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(respond(request));
        }
    }
}
