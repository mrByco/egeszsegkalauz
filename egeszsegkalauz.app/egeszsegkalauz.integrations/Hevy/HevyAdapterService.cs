using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using egeszsegkalauz.integrations.Common;
using Microsoft.Extensions.Options;
using Polly.Timeout;

namespace egeszsegkalauz.integrations.Hevy;

public class HevyAdapterService : IHevyAdapterService
{
    public const string WorkoutEndpoint = "workout";
    public const string ExerciseTemplateEndpoint = "exercise-template";
    public const string BodyMeasurementEndpoint = "body-measurement";
    public const string UserInfoEndpoint = "user-info";

    private const int WorkoutPageSize = 10;
    private const int TemplatePageSize = 100;
    private const int MeasurementPageSize = 10;

    private readonly HttpClient httpClient;
    private readonly HevyOptions options;
    private readonly TimeProvider timeProvider;

    public HevyAdapterService(HttpClient httpClient, IOptions<HevyOptions> options, TimeProvider timeProvider)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    public string SignatureHeader => "Hevy-Signature";

    public string BuildAuthorizationUrl(string state)
    {
        var query = string.Join("&",
            $"client_id={Uri.EscapeDataString(options.ClientId)}",
            $"redirect_uri={Uri.EscapeDataString(options.RedirectUri)}",
            $"state={Uri.EscapeDataString(state)}");
        return $"{options.AuthorizeUrl}?{query}";
    }

    public async Task<OAuthTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await RequestTokensAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
        }, cancellationToken);
    }

    public async Task<OAuthTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return await RequestTokensAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
        }, cancellationToken);
    }

    public async IAsyncEnumerable<SyncChunk> SyncAsync(string accessToken, DateTime? since,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (since is null)
        {
            await foreach (var page in PagesAsync(accessToken, "/v1/workouts", WorkoutPageSize, null, cancellationToken))
                yield return new SyncChunk { Records = (page.Workouts ?? []).Select(WorkoutRecord).OfType<RawRecord>().ToList() };
        }
        else
        {
            var sinceParameter = $"since={Uri.EscapeDataString(since.Value.ToString("O", CultureInfo.InvariantCulture))}";
            await foreach (var page in PagesAsync(accessToken, "/v1/workouts/events", WorkoutPageSize, sinceParameter,
                               cancellationToken))
                yield return EventsChunk(page.Events ?? []);
        }

        await foreach (var page in PagesAsync(accessToken, "/v1/exercise_templates", TemplatePageSize, null, cancellationToken))
        {
            yield return new SyncChunk
            {
                Records = (page.ExerciseTemplates ?? [])
                    .Select(x => IdOf(x) is { } id ? Raw(ExerciseTemplateEndpoint, id, null, x.GetRawText()) : null)
                    .OfType<RawRecord>()
                    .ToList(),
            };
        }

        await foreach (var page in PagesAsync(accessToken, "/v1/body_measurements", MeasurementPageSize, null, cancellationToken))
        {
            yield return new SyncChunk
            {
                Records = (page.BodyMeasurements ?? [])
                    .Select(x => x.Deserialize<HevyBodyMeasurement>()?.Date is { } date
                        ? Raw(BodyMeasurementEndpoint, date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), date, x.GetRawText())
                        : null)
                    .OfType<RawRecord>()
                    .ToList(),
            };
        }

        var userInfo = await GetAsync(accessToken, "/v1/user/info", cancellationToken);
        if (userInfo is not null)
            yield return new SyncChunk { Records = [Raw(UserInfoEndpoint, "profile", null, userInfo)] };
    }

    public IReadOnlyList<string> CompletelyListedEndpoints(bool fullSync)
    {
        return fullSync
            ? [WorkoutEndpoint, ExerciseTemplateEndpoint, BodyMeasurementEndpoint]
            : [ExerciseTemplateEndpoint, BodyMeasurementEndpoint];
    }

    public async Task<SyncChunk> FetchAsync(string accessToken, WebhookNotification notification, CancellationToken cancellationToken)
    {
        if (notification.ItemId is not { } workoutId)
            return new SyncChunk();

        var json = await GetAsync(accessToken, $"/v1/workouts/{Uri.EscapeDataString(workoutId)}", cancellationToken);
        if (json is not null && WorkoutRecord(JsonDocument.Parse(json).RootElement) is { } record)
            return new SyncChunk { Records = [record] };
        return new SyncChunk { Deleted = [new RawRecordKey { Endpoint = WorkoutEndpoint, Key = workoutId }] };
    }

    public bool IsSignatureValid(string body, string? signature)
    {
        if (string.IsNullOrEmpty(options.ClientSecret) || signature is null)
            return false;

        var expected = "sha256=" + Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.ClientSecret), Encoding.UTF8.GetBytes(body)));
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature));
    }

    public WebhookNotification? Parse(string body)
    {
        try
        {
            var webhook = JsonSerializer.Deserialize<HevyWebhook>(body);
            return webhook is { Type: { } type, UserId: { } userId }
                ? new WebhookNotification { Type = type, ExternalUserId = userId, ItemId = webhook.WorkoutId }
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async IAsyncEnumerable<HevyPage> PagesAsync(string accessToken, string path, int pageSize, string? extraQuery,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var page = 1; ; page++)
        {
            var query = $"page={page}&pageSize={pageSize}" + (extraQuery is null ? "" : "&" + extraQuery);
            var json = await GetAsync(accessToken, $"{path}?{query}", cancellationToken);
            if (json is null)
                yield break;

            var result = JsonSerializer.Deserialize<HevyPage>(json) ?? new HevyPage();
            yield return result;
            if (page >= result.PageCount)
                yield break;
        }
    }

    private async Task<string?> GetAsync(string accessToken, string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return null;
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new IntegrationKeyRejectedException("Hevy rejected the access token.");
        EnsureSuccess(response);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (options.RequestInterval > TimeSpan.Zero)
            await Task.Delay(options.RequestInterval, cancellationToken);
        return string.IsNullOrWhiteSpace(json) ? null : json;
    }

    private async Task<OAuthTokens> RequestTokensAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenUrl) { Content = new FormUrlEncodedContent(form) };
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new IntegrationKeyRejectedException("Hevy rejected the authorization.");
        EnsureSuccess(response);

        var tokens = await JsonSerializer.DeserializeAsync<HevyTokenResponse>(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (tokens is not { AccessToken: { } accessToken, RefreshToken: { } refreshToken })
            throw new IntegrationUnavailableException("Hevy returned no tokens.");

        return new OAuthTokens
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(tokens.ExpiresIn ?? 3600),
            ExternalUserId = tokens.UserId,
            AccountName = tokens.Username,
        };
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception e) when (e is HttpRequestException or TimeoutRejectedException
                                      || (e is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new IntegrationUnavailableException("Hevy is unreachable.", e);
        }
    }

    private void EnsureSuccess(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new IntegrationRateLimitedException("Hevy rate limit reached.", options.RateLimitPause);
        if (!response.IsSuccessStatusCode)
            throw new IntegrationUnavailableException(
                $"Hevy answered {(int)response.StatusCode} for {response.RequestMessage?.RequestUri?.AbsolutePath}.");
    }

    private static SyncChunk EventsChunk(IEnumerable<JsonElement> items)
    {
        var chunk = new SyncChunk();
        foreach (var item in items)
        {
            var hevyEvent = item.Deserialize<HevyEvent>();
            if (hevyEvent?.Type == "deleted" && hevyEvent.Id is { } id)
                chunk.Deleted.Add(new RawRecordKey { Endpoint = WorkoutEndpoint, Key = id });
            else if (hevyEvent?.Type == "updated" && hevyEvent.Workout is { ValueKind: JsonValueKind.Object } workout
                     && WorkoutRecord(workout) is { } record)
                chunk.Records.Add(record);
        }

        return chunk;
    }

    private static RawRecord? WorkoutRecord(JsonElement item)
    {
        return item.Deserialize<HevyWorkout>() is { Id: { } id } workout
            ? Raw(WorkoutEndpoint, id, workout.StartTime is { } start ? DateOnly.FromDateTime(start.UtcDateTime) : null,
                item.GetRawText())
            : null;
    }

    private static string? IdOf(JsonElement item) =>
        item.ValueKind == JsonValueKind.Object && item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String
            ? id.GetString()
            : null;

    private static RawRecord Raw(string endpoint, string key, DateOnly? date, string json) =>
        new() { Endpoint = endpoint, Key = key, Date = date, Json = json };
}
