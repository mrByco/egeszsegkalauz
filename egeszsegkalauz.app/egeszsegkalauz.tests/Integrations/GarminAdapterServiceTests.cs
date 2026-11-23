using System.Net;
using System.Text;
using egeszsegkalauz.integrations.Common;
using egeszsegkalauz.integrations.Garmin;
using Microsoft.Extensions.Options;

namespace egeszsegkalauz.tests.Integrations;

public class GarminAdapterServiceTests
{
    private const string ValidKey = "valid-key";

    private static readonly DateOnly Day = new(2026, 11, 20);

    private readonly List<HttpRequestMessage> requests = [];
    private Dictionary<string, Func<HttpResponseMessage>> routes = [];
    private int tokensIssued;
    private GarminTokenCache tokenCache = new(TimeProvider.System);

    [SetUp]
    public void Reset()
    {
        requests.Clear();
        routes = [];
        tokensIssued = 0;
        tokenCache = new GarminTokenCache(TimeProvider.System);
    }

    [Test]
    public async Task AcceptsAKeyGarminExchangesForAToken()
    {
        Assert.That(await CreateAdapter().IsApiKeyValidAsync(ValidKey, CancellationToken.None), Is.True);
        Assert.That(requests.Single().Headers.Authorization?.ToString(), Is.EqualTo($"ApiKey {ValidKey}"));
        Assert.That(await CreateAdapter().IsApiKeyValidAsync("wrong-key", CancellationToken.None), Is.False);
    }

    [Test]
    public async Task HeartRateQueryKeepsTheRawDayAndDropsEmptyReadings()
    {
        Route("/wellness-service/wellness/dailyHeartRate?date=2026-11-20", """
            {"calendarDate":"2026-11-20","restingHeartRate":52.0,"minHeartRate":48,"maxHeartRate":131,
             "heartRateValues":[[1795129200000,60],[1795129320000,null],[1795129440000,64]]}
            """);

        var result = await CreateAdapter().RunDailyQueryAsync(ValidKey, GarminAdapterService.HeartRateQuery, Day, CancellationToken.None);

        Assert.That((result.Raw.Endpoint, result.Raw.Key, result.Raw.Date), Is.EqualTo(("heart-rate", "2026-11-20", (DateOnly?)Day)));
        Assert.That(result.Raw.Json, Does.Contain("heartRateValues"));
        Assert.That(result.HeartRate?.RestingBpm, Is.EqualTo(52));
        Assert.That(result.HeartRate?.Readings.Select(x => x.Bpm), Is.EqualTo(new[] { 60, 64 }));
        Assert.That(result.HeartRate?.Readings[0].TimestampUtc, Is.EqualTo(new DateTime(2026, 11, 19, 23, 0, 0, DateTimeKind.Utc)));
        Assert.That(requests.Last().Headers.Authorization?.ToString(), Is.EqualTo("Bearer token-1"));
    }

    [Test]
    public async Task UserSummaryQueryMapsTheDailyTotals()
    {
        Route("/usersummary-service/usersummary/daily?calendarDate=2026-11-20", """
            {"calendarDate":"2026-11-20","totalSteps":8123.0,"vigorousIntensityMinutes":12,
             "bodyBatteryHighestValue":91,"averageStressLevel":34,"averageSpo2":null}
            """);

        var result = await CreateAdapter().RunDailyQueryAsync(ValidKey, GarminAdapterService.UserSummaryQuery, Day, CancellationToken.None);

        Assert.That(result.Activity?.Steps, Is.EqualTo(8123));
        Assert.That(result.Activity?.VigorousIntensityMinutes, Is.EqualTo(12));
        Assert.That(result.Activity?.BodyBatteryHighest, Is.EqualTo(91));
        Assert.That(result.Activity?.AverageSpo2, Is.Null);
    }

    [Test]
    public async Task ADayWithoutDataIsStoredAsNull()
    {
        routes["/sleep-service/sleep/dailySleep?date=2026-11-20"] = () => new HttpResponseMessage(HttpStatusCode.NoContent);

        var result = await CreateAdapter().RunDailyQueryAsync(ValidKey, GarminAdapterService.SleepQuery, Day, CancellationToken.None);

        Assert.That(result.Raw.Json, Is.EqualTo("null"));
    }

    [Test]
    public async Task ReusesTheTokenAndRenewsItWhenGarminRejectsIt()
    {
        var adapter = CreateAdapter();
        var dataCalls = 0;
        routes["/sleep-service/sleep/dailySleep?date=2026-11-20"] = () =>
            ++dataCalls == 2 ? new HttpResponseMessage(HttpStatusCode.Unauthorized) : Json("{}");

        await adapter.RunDailyQueryAsync(ValidKey, GarminAdapterService.SleepQuery, Day, CancellationToken.None);
        await adapter.RunDailyQueryAsync(ValidKey, GarminAdapterService.SleepQuery, Day, CancellationToken.None);

        Assert.That(tokensIssued, Is.EqualTo(2));
        Assert.That(requests.Last().Headers.Authorization?.ToString(), Is.EqualTo("Bearer token-2"));
    }

    [Test]
    public async Task RateLimitingPausesForRetryAfterOrTheConfiguredPause()
    {
        routes["/sleep-service/sleep/dailySleep?date=2026-11-20"] = () => new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        var error = await CatchAsync(() => CreateAdapter()
            .RunDailyQueryAsync(ValidKey, GarminAdapterService.SleepQuery, Day, CancellationToken.None));

        Assert.That(error, Is.InstanceOf<IntegrationRateLimitedException>());
        Assert.That(((IntegrationRateLimitedException)error).Pause, Is.EqualTo(TimeSpan.FromMinutes(3)));
    }

    [Test]
    public async Task ARejectedKeyFailsTheQuery()
    {
        Assert.That(await CatchAsync(() => CreateAdapter()
                .RunDailyQueryAsync("wrong-key", GarminAdapterService.SleepQuery, Day, CancellationToken.None)),
            Is.InstanceOf<IntegrationKeyRejectedException>());
    }

    [Test]
    public async Task SummariesAreSplitIntoOneRawRecordPerDayAndParsed()
    {
        Route("/sleep-service/sleep/dailySleepSummariesByDate?startDate=2026-11-19&endDate=2026-11-20", """
            [{"calendarDate":"2026-11-19","sleepTimeSeconds":27000,"deepSleepSeconds":5400.0,
              "sleepStartTimestampGMT":1795125600000,"sleepScores":{"overall":{"value":81,"qualifierKey":"GOOD"}}},
             {"calendarDate":"2026-11-20","sleepTimeSeconds":21000}]
            """);
        Route("/weight-service/weight/all/2026-11-19/2026-11-20", """
            [{"calendarDate":"2026-11-20","timestampGMT":1795248882000,"weight":70839.0,"boneMass":4730,"bodyFat":18.1}]
            """);
        Route("/biometric-service/heartRateZones/v2", """[{"sport":"DEFAULT","zone1Floor":100}]""");
        Route("/fitnessage-service/fitnessage/v2/2026-11-20?fullHistoryScan=true", """{"fitnessAge":22.3}""");

        var chunks = await CreateAdapter()
            .ImportSummariesAsync(ValidKey, Day.AddDays(-1), Day, CancellationToken.None)
            .ToListAsync();

        var sleep = chunks.Single(x => x.Endpoint == "sleep-summaries");
        Assert.That(sleep.Raw.Select(x => x.Key), Is.EqualTo(new[] { "2026-11-19", "2026-11-20" }));
        Assert.That(sleep.Sleep[0].Score, Is.EqualTo(81));
        Assert.That(sleep.Sleep[0].ScoreQualifier, Is.EqualTo("GOOD"));
        Assert.That(sleep.Sleep[0].DeepSeconds, Is.EqualTo(5400));
        Assert.That(sleep.Sleep[0].StartUtc, Is.EqualTo(new DateTime(2026, 11, 19, 22, 0, 0, DateTimeKind.Utc)));

        var weight = chunks.Single(x => x.Endpoint == "weight").BodyComposition.Single();
        Assert.That(weight.WeightKg, Is.EqualTo(70.839).Within(0.0001));
        Assert.That(weight.BoneMassKg, Is.EqualTo(4.73).Within(0.0001));

        Assert.That(chunks.Select(x => x.Endpoint), Does.Contain("hr-zones").And.Contain("fitness-age"));
    }

    [Test]
    public async Task ActivitiesStopAtTheStartOfTheRange()
    {
        Route("/activitylist-service/activities/search/activities?start=0&limit=100", """
            [{"activityId":2,"activityName":"Futás","activityType":{"typeKey":"running"},"beginTimestamp":1795212000000,
              "duration":1800.5,"averageHR":150.0,"hrTimeInZone_3":600.0},
             {"activityId":1,"activityName":"Régi","beginTimestamp":1700000000000}]
            """);

        var chunks = await CreateAdapter()
            .ImportSummariesAsync(ValidKey, Day.AddDays(-1), Day, CancellationToken.None)
            .ToListAsync();

        var activity = chunks.Single(x => x.Endpoint == "activities").Activities.Single();
        Assert.That((activity.SourceActivityId, activity.Type, activity.HeartRateZone3Seconds), Is.EqualTo(("2", "running", (double?)600.0)));
        Assert.That(requests.Count(x => x.RequestUri?.AbsolutePath.Contains("activities") == true), Is.EqualTo(1));
    }

    [Test]
    public async Task KeepsTheDailySummariesEnvelopeNextToItsItems()
    {
        Route("/usersummary-service/v2/dailysummaries?startDate=2026-11-19&endDate=2026-11-20",
            """{"data":[{"calendarDate":"2026-11-20","movement":{"steps":5}}],"userProfilePk":7}""");

        var chunks = await CreateAdapter()
            .ImportSummariesAsync(ValidKey, Day.AddDays(-1), Day, CancellationToken.None)
            .ToListAsync();

        var raw = chunks.Single(x => x.Endpoint == "daily-summaries").Raw;
        Assert.That(raw.Select(x => x.Endpoint), Is.EquivalentTo(new[] { "daily-summaries", "daily-summaries-envelope" }));
        Assert.That(raw.Single(x => x.Endpoint == "daily-summaries-envelope").Json, Is.EqualTo("""{"userProfilePk":7}"""));
    }

    [Test]
    public async Task FindsTheFirstDayWithDataByWalkingBackYearByYear()
    {
        Route("/usersummary-service/v2/dailysummaries?startDate=2025-11-21&endDate=2026-11-20",
            """{"data":[{"calendarDate":"2026-01-05"},{"calendarDate":"2026-11-20"}]}""");
        Route("/usersummary-service/v2/dailysummaries?startDate=2024-11-21&endDate=2025-11-20",
            """{"data":[{"calendarDate":"2025-03-01"}]}""");
        Route("/usersummary-service/v2/dailysummaries?startDate=2023-11-22&endDate=2024-11-20", """{"data":[]}""");

        var first = await CreateAdapter().FindFirstDataDateAsync(ValidKey, Day, CancellationToken.None);

        Assert.That(first, Is.EqualTo(new DateOnly(2025, 3, 1)));
    }

    private void Route(string pathAndQuery, string json)
    {
        routes[pathAndQuery] = () => Json(json);
    }

    private GarminAdapterService CreateAdapter()
    {
        var handler = new StubHandler(request =>
        {
            requests.Add(request);
            var pathAndQuery = request.RequestUri?.PathAndQuery ?? "";
            if (pathAndQuery.EndsWith("/token"))
            {
                return request.Headers.Authorization?.Parameter == ValidKey
                    ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"token-{++tokensIssued}") }
                    : new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return routes.TryGetValue(pathAndQuery, out var respond) ? respond() : new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var options = Options.Create(new GarminOptions
        {
            SummaryRequestInterval = TimeSpan.Zero,
            RateLimitPause = TimeSpan.FromMinutes(3),
        });
        return new GarminAdapterService(new HttpClient(handler) { BaseAddress = new Uri("https://garmin.test") },
            tokenCache, options);
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
