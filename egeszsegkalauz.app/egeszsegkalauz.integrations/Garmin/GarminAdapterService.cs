using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using egeszsegkalauz.integrations.Common;
using Microsoft.Extensions.Options;
using Polly.Timeout;

namespace egeszsegkalauz.integrations.Garmin;

public class GarminAdapterService : IGarminAdapterService
{
    public const string HeartRateQuery = "heart-rate";
    public const string UserSummaryQuery = "user-summary";
    public const string SleepQuery = "sleep";
    public const string DailySummariesEndpoint = "daily-summaries";
    public const string SleepSummariesEndpoint = "sleep-summaries";
    public const string BodyBatteryEndpoint = "body-battery";
    public const string Vo2MaxEndpoint = "vo2max";
    public const string WeightEndpoint = "weight";
    public const string ActivityEndpoint = "activity";

    private const string TokenPath = "/personalapitoken-service/personal-api/personal-api-key/token";
    private const int MaxYearsOfHistory = 20;
    private const int ActivityPageSize = 100;

    private static readonly RangeEndpoint[] RangeEndpoints =
    [
        new(DailySummariesEndpoint, 365, (s, e) => $"/usersummary-service/v2/dailysummaries?startDate={s}&endDate={e}", "data"),
        new(SleepSummariesEndpoint, 31, (s, e) => $"/sleep-service/sleep/dailySleepSummariesByDate?startDate={s}&endDate={e}"),
        new(BodyBatteryEndpoint, 31, (s, e) => $"/wellness-service/wellness/bodyBattery/reports/daily?startDate={s}&endDate={e}&chunks=96"),
        new("stress", 28, (s, e) => $"/usersummary-service/stats/stress/daily/{s}/{e}"),
        new("steps", 28, (s, e) => $"/usersummary-service/stats/steps/daily/{s}/{e}"),
        new(Vo2MaxEndpoint, 28, (s, e) => $"/metrics-service/metrics/maxmet/daily/{s}/{e}"),
        new("wellness-activity", 28, (s, e) => $"/wellnessactivity-service/activity/summary/range/{s}/{e}"),
        new(WeightEndpoint, 364, (s, e) => $"/weight-service/weight/all/{s}/{e}"),
    ];

    private readonly HttpClient httpClient;
    private readonly GarminTokenCache tokenCache;
    private readonly GarminOptions options;

    public GarminAdapterService(HttpClient httpClient, GarminTokenCache tokenCache, IOptions<GarminOptions> options)
    {
        this.httpClient = httpClient;
        this.tokenCache = tokenCache;
        this.options = options.Value;
    }

    public IReadOnlyList<string> DailyQueryKinds { get; } = [HeartRateQuery, UserSummaryQuery, SleepQuery];

    public DailyImportPolicy Policy => new()
    {
        MinimumInterval = options.DailyRequestInterval,
        MaxAttempts = options.MaxAttempts,
        RetryDelay = attempt => TimeSpan.FromSeconds(30 * Math.Pow(2, attempt - 1)),
    };

    public async Task<bool> IsApiKeyValidAsync(string apiKey, CancellationToken cancellationToken)
    {
        tokenCache.Remove(apiKey);
        return await TryGetTokenAsync(apiKey, cancellationToken) is not null;
    }

    public async Task<DateOnly?> FindFirstDataDateAsync(string apiKey, DateOnly today, CancellationToken cancellationToken)
    {
        DateOnly? first = null;
        var end = today;
        for (var year = 0; year < MaxYearsOfHistory; year++)
        {
            var start = end.AddDays(-364);
            var json = await GetAsync(apiKey, RangeEndpoints[0].Path(Format(start), Format(end)), cancellationToken);
            var dates = json is null
                ? []
                : Items(json, RangeEndpoints[0].ItemsProperty).Select(DateOf).OfType<DateOnly>().ToList();
            if (dates.Count == 0 && first is not null)
                break;
            if (dates.Count > 0)
                first = dates.Min();

            end = start.AddDays(-1);
            await PauseAsync(options.SummaryRequestInterval, cancellationToken);
        }

        return first;
    }

    public async IAsyncEnumerable<SyncChunk> ImportSummariesAsync(string apiKey, DateOnly from, DateOnly to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var endpoint in RangeEndpoints)
        {
            for (var start = from; start <= to; start = start.AddDays(endpoint.Days))
            {
                var end = Min(start.AddDays(endpoint.Days - 1), to);
                var json = await GetAsync(apiKey, endpoint.Path(Format(start), Format(end)), cancellationToken);
                if (json is not null)
                    yield return new SyncChunk { Records = SplitRecords(endpoint, json, $"{Format(start)}..{Format(end)}") };
                await PauseAsync(options.SummaryRequestInterval, cancellationToken);
            }
        }

        await foreach (var chunk in ImportActivitiesAsync(apiKey, from, cancellationToken))
            yield return chunk;

        foreach (var (endpoint, path) in new[]
                 {
                     ("hr-zones", "/biometric-service/heartRateZones/v2"),
                     ("fitness-age", $"/fitnessage-service/fitnessage/v2/{Format(to)}?fullHistoryScan=true"),
                 })
        {
            var json = await GetAsync(apiKey, path, cancellationToken);
            if (json is not null)
                yield return new SyncChunk { Records = [Raw(endpoint, Format(to), to, json)] };
            await PauseAsync(options.SummaryRequestInterval, cancellationToken);
        }
    }

    public async Task<SyncChunk> RunDailyQueryAsync(string apiKey, string kind, DateOnly date, CancellationToken cancellationToken)
    {
        var path = kind switch
        {
            HeartRateQuery => $"/wellness-service/wellness/dailyHeartRate?date={Format(date)}",
            UserSummaryQuery => $"/usersummary-service/usersummary/daily?calendarDate={Format(date)}",
            SleepQuery => $"/sleep-service/sleep/dailySleep?date={Format(date)}",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        var json = await GetAsync(apiKey, path, cancellationToken) ?? "null";
        return new SyncChunk { Records = [Raw(kind, Format(date), date, json)] };
    }

    private async IAsyncEnumerable<SyncChunk> ImportActivitiesAsync(string apiKey, DateOnly from,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var fromMs = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeMilliseconds();
        for (var start = 0; ; start += ActivityPageSize)
        {
            var json = await GetAsync(apiKey,
                $"/activitylist-service/activities/search/activities?start={start}&limit={ActivityPageSize}", cancellationToken);
            var items = json is null ? [] : Items(json, null).ToList();
            if (items.Count == 0)
                yield break;

            var records = new List<RawRecord>();
            foreach (var item in items)
            {
                var activity = item.Deserialize<GarminActivity>(GarminJson.Options);
                if (activity?.BeginTimestamp is { } begin && begin >= fromMs)
                {
                    records.Add(Raw(ActivityEndpoint, activity.ActivityId.ToString(CultureInfo.InvariantCulture),
                        DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds((long)begin).UtcDateTime),
                        item.GetRawText()));
                }
            }

            yield return new SyncChunk { Records = records };
            if (records.Count < items.Count || items.Count < ActivityPageSize)
                yield break;
            await PauseAsync(options.SummaryRequestInterval, cancellationToken);
        }
    }

    private async Task<string?> GetAsync(string apiKey, string path, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            var token = await TryGetTokenAsync(apiKey, cancellationToken)
                        ?? throw new IntegrationKeyRejectedException("Garmin rejected the API key.");

            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
            {
                tokenCache.Remove(apiKey);
                continue;
            }

            if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound)
                return null;
            EnsureSuccess(response);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(json) ? null : json;
        }
    }

    private async Task<string?> TryGetTokenAsync(string apiKey, CancellationToken cancellationToken)
    {
        var cached = tokenCache.Get(apiKey);
        if (cached is not null)
            return cached;

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenPath);
        request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", apiKey);

        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest)
            return null;
        EnsureSuccess(response);

        var token = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
        if (token.Length == 0)
            return null;

        tokenCache.Set(apiKey, token);
        return token;
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
            throw new IntegrationUnavailableException("Garmin Connect is unreachable.", e);
        }
    }

    private void EnsureSuccess(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new IntegrationRateLimitedException("Garmin Connect rate limit reached.",
                response.Headers.RetryAfter?.Delta ?? options.RateLimitPause);
        if (!response.IsSuccessStatusCode)
            throw new IntegrationUnavailableException(
                $"Garmin Connect answered {(int)response.StatusCode} for {response.RequestMessage?.RequestUri?.AbsolutePath}.");
    }

    private static List<RawRecord> SplitRecords(RangeEndpoint endpoint, string json, string rangeKey)
    {
        var records = Items(json, endpoint.ItemsProperty)
            .Select((item, index) => Raw(endpoint.Name, KeyOf(item) ?? $"{rangeKey}#{index}", DateOf(item), item.GetRawText()))
            .ToList();
        if (endpoint.ItemsProperty is not null && EnvelopeWithout(json, endpoint.ItemsProperty) is { } envelope)
            records.Add(Raw($"{endpoint.Name}-envelope", rangeKey, null, envelope));
        return records;
    }

    private static async Task PauseAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        if (interval > TimeSpan.Zero)
            await Task.Delay(interval, cancellationToken);
    }

    private static IEnumerable<JsonElement> Items(string json, string? itemsProperty)
    {
        var root = JsonDocument.Parse(json).RootElement;
        if (itemsProperty is not null && root.ValueKind == JsonValueKind.Object
                                      && root.TryGetProperty(itemsProperty, out var nested))
            root = nested;
        return root.ValueKind switch
        {
            JsonValueKind.Array => root.EnumerateArray().ToList(),
            JsonValueKind.Object => [root],
            _ => [],
        };
    }

    private static string? EnvelopeWithout(string json, string itemsProperty)
    {
        if (JsonNode.Parse(json) is not JsonObject envelope)
            return null;
        envelope.Remove(itemsProperty);
        return envelope.Count == 0 ? null : envelope.ToJsonString();
    }

    private static string? KeyOf(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
            return null;
        foreach (var name in new[] { "timestampGMT", "activityId" })
        {
            if (item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number)
                return value.GetRawText();
        }

        return DateOf(item)?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static DateOnly? DateOf(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
            return null;
        foreach (var container in new[] { item, item.TryGetProperty("generic", out var generic) ? generic : default })
        {
            if (container.ValueKind != JsonValueKind.Object)
                continue;
            foreach (var name in new[] { "calendarDate", "date" })
            {
                if (container.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                    && DateOnly.TryParse(value.GetString(), CultureInfo.InvariantCulture, out var date))
                    return date;
            }
        }

        return null;
    }

    private static RawRecord Raw(string endpoint, string key, DateOnly? date, string json) =>
        new() { Endpoint = endpoint, Key = key, Date = date, Json = json };

    private static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;

    private class RangeEndpoint
    {
        public RangeEndpoint(string name, int days, Func<string, string, string> path, string? itemsProperty = null)
        {
            Name = name;
            Days = days;
            Path = path;
            ItemsProperty = itemsProperty;
        }

        public string Name { get; }
        public int Days { get; }
        public Func<string, string, string> Path { get; }
        public string? ItemsProperty { get; }
    }
}
