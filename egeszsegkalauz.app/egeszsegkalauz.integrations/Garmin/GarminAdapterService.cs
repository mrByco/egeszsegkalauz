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

    private const string TokenPath = "/personalapitoken-service/personal-api/personal-api-key/token";
    private const int MaxYearsOfHistory = 20;
    private const int ActivityPageSize = 100;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly RangeEndpoint[] RangeEndpoints =
    [
        new("daily-summaries", 365, (s, e) => $"/usersummary-service/v2/dailysummaries?startDate={s}&endDate={e}", "data"),
        new("sleep-summaries", 31, (s, e) => $"/sleep-service/sleep/dailySleepSummariesByDate?startDate={s}&endDate={e}"),
        new("body-battery", 31, (s, e) => $"/wellness-service/wellness/bodyBattery/reports/daily?startDate={s}&endDate={e}&chunks=96"),
        new("stress", 28, (s, e) => $"/usersummary-service/stats/stress/daily/{s}/{e}"),
        new("steps", 28, (s, e) => $"/usersummary-service/stats/steps/daily/{s}/{e}"),
        new("vo2max", 28, (s, e) => $"/metrics-service/metrics/maxmet/daily/{s}/{e}"),
        new("wellness-activity", 28, (s, e) => $"/wellnessactivity-service/activity/summary/range/{s}/{e}"),
        new("weight", 364, (s, e) => $"/weight-service/weight/all/{s}/{e}"),
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
                : Items(json, RangeEndpoints[0].ItemsProperty).Select(x => DateOf(x)).OfType<DateOnly>().ToList();
            if (dates.Count == 0 && first is not null)
                break;
            if (dates.Count > 0)
                first = dates.Min();

            end = start.AddDays(-1);
            await PauseAsync(options.SummaryRequestInterval, cancellationToken);
        }

        return first;
    }

    public async IAsyncEnumerable<SummaryChunk> ImportSummariesAsync(string apiKey, DateOnly from, DateOnly to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var endpoint in RangeEndpoints)
        {
            for (var start = from; start <= to; start = start.AddDays(endpoint.Days))
            {
                var end = Min(start.AddDays(endpoint.Days - 1), to);
                var json = await GetAsync(apiKey, endpoint.Path(Format(start), Format(end)), cancellationToken);
                if (json is not null)
                    yield return ToChunk(endpoint, json, $"{Format(start)}..{Format(end)}");
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
                yield return new SummaryChunk { Endpoint = endpoint, Raw = [Raw(endpoint, Format(to), to, json)] };
            await PauseAsync(options.SummaryRequestInterval, cancellationToken);
        }
    }

    public async Task<DailyQueryResult> RunDailyQueryAsync(string apiKey, string kind, DateOnly date,
        CancellationToken cancellationToken)
    {
        var path = kind switch
        {
            HeartRateQuery => $"/wellness-service/wellness/dailyHeartRate?date={Format(date)}",
            UserSummaryQuery => $"/usersummary-service/usersummary/daily?calendarDate={Format(date)}",
            SleepQuery => $"/sleep-service/sleep/dailySleep?date={Format(date)}",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        var json = await GetAsync(apiKey, path, cancellationToken) ?? "null";
        var hasData = json != "null";
        return new DailyQueryResult
        {
            Raw = Raw(kind, Format(date), date, json),
            HeartRate = kind == HeartRateQuery && hasData ? ToHeartRate(date, Parse<GarminDailyHeartRate>(json)) : null,
            Activity = kind == UserSummaryQuery && hasData ? ToDailyActivity(date, Parse<GarminUserSummary>(json)) : null,
        };
    }

    private async IAsyncEnumerable<SummaryChunk> ImportActivitiesAsync(string apiKey, DateOnly from,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        for (var start = 0; ; start += ActivityPageSize)
        {
            var json = await GetAsync(apiKey,
                $"/activitylist-service/activities/search/activities?start={start}&limit={ActivityPageSize}", cancellationToken);
            var items = json is null ? [] : Items(json, null).ToList();
            if (items.Count == 0)
                yield break;

            var activities = new List<(JsonElement Element, ActivityRecord Activity)>();
            foreach (var item in items)
            {
                if (ToActivity(item.Deserialize<GarminActivity>(Json)) is { } activity && activity.StartUtc >= fromUtc)
                    activities.Add((item, activity));
            }

            yield return new SummaryChunk
            {
                Endpoint = "activities",
                Raw = activities
                    .Select(x => Raw("activity", x.Activity.SourceActivityId, DateOnly.FromDateTime(x.Activity.StartUtc),
                        x.Element.GetRawText()))
                    .ToList(),
                Activities = activities.Select(x => x.Activity).ToList(),
            };

            if (activities.Count < items.Count || items.Count < ActivityPageSize)
                yield break;
            await PauseAsync(options.SummaryRequestInterval, cancellationToken);
        }
    }

    private SummaryChunk ToChunk(RangeEndpoint endpoint, string json, string rangeKey)
    {
        var items = Items(json, endpoint.ItemsProperty).ToList();
        var raw = items
            .Select((item, index) => Raw(endpoint.Name, KeyOf(item) ?? $"{rangeKey}#{index}", DateOf(item), item.GetRawText()))
            .ToList();
        if (endpoint.ItemsProperty is not null && EnvelopeWithout(json, endpoint.ItemsProperty) is { } envelope)
            raw.Add(Raw($"{endpoint.Name}-envelope", rangeKey, null, envelope));

        return endpoint.Name switch
        {
            "sleep-summaries" => new SummaryChunk
            {
                Endpoint = endpoint.Name, Raw = raw,
                Sleep = items.Select(x => ToSleep(x.Deserialize<GarminSleepSummary>(Json))).OfType<SleepSession>().ToList(),
            },
            "body-battery" => new SummaryChunk
            {
                Endpoint = endpoint.Name, Raw = raw,
                BodyBattery = items.Select(x => ToBodyBattery(x.Deserialize<GarminBodyBatteryReport>(Json)))
                    .OfType<BodyBatteryDay>().ToList(),
            },
            "vo2max" => new SummaryChunk
            {
                Endpoint = endpoint.Name, Raw = raw,
                Vo2Max = items.SelectMany(x => ToVo2Max(x.Deserialize<GarminVo2MaxDay>(Json))).ToList(),
            },
            "weight" => new SummaryChunk
            {
                Endpoint = endpoint.Name, Raw = raw,
                BodyComposition = items.Select(x => ToBodyComposition(x.Deserialize<GarminWeight>(Json)))
                    .OfType<BodyCompositionReading>().ToList(),
            },
            _ => new SummaryChunk { Endpoint = endpoint.Name, Raw = raw },
        };
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
        var root = JsonNode.Parse(json);
        if (root is not JsonObject envelope)
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

    private static T Parse<T>(string json) => JsonSerializer.Deserialize<T>(json, Json) ?? throw new JsonException($"Empty {typeof(T).Name}.");

    private static RawRecord Raw(string endpoint, string key, DateOnly? date, string json) =>
        new() { Endpoint = endpoint, Key = key, Date = date, Json = json };

    private static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;

    private static DateTime FromEpochMs(double ms) => DateTimeOffset.FromUnixTimeMilliseconds((long)ms).UtcDateTime;

    private static int? ToInt(double? value) => value is null ? null : (int)Math.Round(value.Value);

    private static DailyHeartRate ToHeartRate(DateOnly date, GarminDailyHeartRate response)
    {
        return new DailyHeartRate
        {
            Date = response.CalendarDate ?? date,
            RestingBpm = ToInt(response.RestingHeartRate),
            MinBpm = ToInt(response.MinHeartRate),
            MaxBpm = ToInt(response.MaxHeartRate),
            Readings = (response.HeartRateValues ?? [])
                .SelectMany(x => x is [{ } timestamp, { } bpm]
                    ? [new HeartRateReading { TimestampUtc = FromEpochMs(timestamp), Bpm = (int)bpm }]
                    : Array.Empty<HeartRateReading>())
                .ToList(),
        };
    }

    private static DailyActivity ToDailyActivity(DateOnly date, GarminUserSummary summary)
    {
        return new DailyActivity
        {
            Date = summary.CalendarDate ?? date,
            Steps = ToInt(summary.TotalSteps),
            StepGoal = ToInt(summary.DailyStepGoal),
            DistanceMeters = summary.TotalDistanceMeters,
            TotalKilocalories = summary.TotalKilocalories,
            ActiveKilocalories = summary.ActiveKilocalories,
            BmrKilocalories = summary.BmrKilocalories,
            FloorsAscended = summary.FloorsAscended,
            FloorsDescended = summary.FloorsDescended,
            ModerateIntensityMinutes = ToInt(summary.ModerateIntensityMinutes),
            VigorousIntensityMinutes = ToInt(summary.VigorousIntensityMinutes),
            HighlyActiveSeconds = ToInt(summary.HighlyActiveSeconds),
            ActiveSeconds = ToInt(summary.ActiveSeconds),
            SedentarySeconds = ToInt(summary.SedentarySeconds),
            SleepingSeconds = ToInt(summary.SleepingSeconds),
            RestingHeartRate = ToInt(summary.RestingHeartRate),
            MinHeartRate = ToInt(summary.MinHeartRate),
            MaxHeartRate = ToInt(summary.MaxHeartRate),
            AverageStressLevel = ToInt(summary.AverageStressLevel),
            MaxStressLevel = ToInt(summary.MaxStressLevel),
            LowStressSeconds = ToInt(summary.LowStressDuration),
            MediumStressSeconds = ToInt(summary.MediumStressDuration),
            HighStressSeconds = ToInt(summary.HighStressDuration),
            RestStressSeconds = ToInt(summary.RestStressDuration),
            BodyBatteryHighest = ToInt(summary.BodyBatteryHighestValue),
            BodyBatteryLowest = ToInt(summary.BodyBatteryLowestValue),
            BodyBatteryCharged = ToInt(summary.BodyBatteryChargedValue),
            BodyBatteryDrained = ToInt(summary.BodyBatteryDrainedValue),
            BodyBatteryAtWake = ToInt(summary.BodyBatteryAtWakeTime),
            AverageSpo2 = summary.AverageSpo2,
            LowestSpo2 = summary.LowestSpo2,
            AverageWakingRespiration = summary.AvgWakingRespirationValue,
        };
    }

    private static SleepSession? ToSleep(GarminSleepSummary? sleep)
    {
        if (sleep?.CalendarDate is not { } date)
            return null;

        return new SleepSession
        {
            Date = date,
            StartUtc = sleep.SleepStartTimestampGMT is { } start ? FromEpochMs(start) : null,
            EndUtc = sleep.SleepEndTimestampGMT is { } end ? FromEpochMs(end) : null,
            SleepSeconds = ToInt(sleep.SleepTimeSeconds),
            NapSeconds = ToInt(sleep.NapTimeSeconds),
            DeepSeconds = ToInt(sleep.DeepSleepSeconds),
            LightSeconds = ToInt(sleep.LightSleepSeconds),
            RemSeconds = ToInt(sleep.RemSleepSeconds),
            AwakeSeconds = ToInt(sleep.AwakeSleepSeconds),
            UnmeasurableSeconds = ToInt(sleep.UnmeasurableSleepSeconds),
            AwakeCount = ToInt(sleep.AwakeCount),
            Score = ToInt(sleep.SleepScores?.Overall?.Value),
            ScoreQualifier = sleep.SleepScores?.Overall?.QualifierKey,
            AverageSleepStress = sleep.AvgSleepStress,
            AverageRespiration = sleep.AverageRespirationValue,
            LowestRespiration = sleep.LowestRespirationValue,
            HighestRespiration = sleep.HighestRespirationValue,
        };
    }

    private static BodyBatteryDay? ToBodyBattery(GarminBodyBatteryReport? report)
    {
        if (report?.Date is not { } date)
            return null;

        return new BodyBatteryDay
        {
            Date = date,
            Charged = ToInt(report.Charged),
            Drained = ToInt(report.Drained),
            Readings = (report.BodyBatteryValuesArray ?? [])
                .SelectMany(x => x is [{ } timestamp, { } level]
                    ? [new BodyBatteryReading { TimestampUtc = FromEpochMs(timestamp), Level = (int)level }]
                    : Array.Empty<BodyBatteryReading>())
                .DistinctBy(x => x.TimestampUtc)
                .ToList(),
        };
    }

    private static IEnumerable<Vo2MaxReading> ToVo2Max(GarminVo2MaxDay? day)
    {
        foreach (var (reading, fallbackSport) in new[] { (day?.Generic, "GENERIC"), (day?.Cycling, "CYCLING") })
        {
            if (reading?.CalendarDate is { } date && (reading.Vo2MaxPreciseValue ?? reading.Vo2MaxValue) is { } value)
                yield return new Vo2MaxReading { Date = date, Sport = reading.Sport ?? fallbackSport, Value = value };
        }
    }

    private static BodyCompositionReading? ToBodyComposition(GarminWeight? weight)
    {
        if (weight is null || (weight.TimestampGMT ?? weight.Date) is not { } timestamp)
            return null;

        var timestampUtc = FromEpochMs(timestamp);
        return new BodyCompositionReading
        {
            TimestampUtc = timestampUtc,
            Date = weight.CalendarDate ?? DateOnly.FromDateTime(timestampUtc),
            WeightKg = weight.Weight / 1000,
            Bmi = weight.Bmi,
            BodyFatPercent = weight.BodyFat,
            BodyWaterPercent = weight.BodyWater,
            BoneMassKg = weight.BoneMass / 1000,
            MuscleMassKg = weight.MuscleMass / 1000,
            VisceralFat = weight.VisceralFat,
            MetabolicAge = ToInt(weight.MetabolicAge),
        };
    }

    private static ActivityRecord? ToActivity(GarminActivity? activity)
    {
        if (activity?.BeginTimestamp is not { } begin)
            return null;

        return new ActivityRecord
        {
            SourceActivityId = activity.ActivityId.ToString(CultureInfo.InvariantCulture),
            Name = activity.ActivityName,
            Type = activity.ActivityType?.TypeKey,
            StartUtc = FromEpochMs(begin),
            DurationSeconds = activity.Duration,
            MovingSeconds = activity.MovingDuration,
            DistanceMeters = activity.Distance,
            Calories = activity.Calories,
            AverageHeartRate = activity.AverageHR,
            MaxHeartRate = activity.MaxHR,
            ElevationGain = activity.ElevationGain,
            ElevationLoss = activity.ElevationLoss,
            AverageSpeed = activity.AverageSpeed,
            MaxSpeed = activity.MaxSpeed,
            Steps = ToInt(activity.Steps),
            LocationName = activity.LocationName,
            HeartRateZone1Seconds = activity.HrTimeInZone1,
            HeartRateZone2Seconds = activity.HrTimeInZone2,
            HeartRateZone3Seconds = activity.HrTimeInZone3,
            HeartRateZone4Seconds = activity.HrTimeInZone4,
            HeartRateZone5Seconds = activity.HrTimeInZone5,
        };
    }

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
