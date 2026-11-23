using System.Runtime.CompilerServices;
using egeszsegkalauz.integrations.Common;
using egeszsegkalauz.integrations.Garmin;

namespace egeszsegkalauz.tests.Integrations;

public class FakeGarminAdapter : IGarminAdapterService
{
    public const string ValidKey = "valid-garmin-key";

    public DateOnly? FirstDataDate { get; set; }
    public List<(string Kind, DateOnly Date)> DailyQueries { get; } = [];
    public List<(DateOnly From, DateOnly To)> SummaryRanges { get; } = [];
    public Func<string, DateOnly, Exception?> DailyFailure { get; set; } = (_, _) => null;
    public HashSet<DateOnly> EmptyDates { get; } = [];
    public Func<string, DateOnly, string> RawJson { get; set; } = (_, _) => """{"ok":true}""";

    public IReadOnlyList<string> DailyQueryKinds { get; } = [GarminAdapterService.HeartRateQuery, GarminAdapterService.UserSummaryQuery];

    public DailyImportPolicy Policy { get; } = new()
    {
        MinimumInterval = TimeSpan.FromSeconds(1),
        MaxAttempts = 2,
        RetryDelay = attempt => TimeSpan.FromMinutes(attempt),
    };

    public Task<bool> IsApiKeyValidAsync(string apiKey, CancellationToken cancellationToken)
    {
        return Task.FromResult(apiKey == ValidKey);
    }

    public Task<DateOnly?> FindFirstDataDateAsync(string apiKey, DateOnly today, CancellationToken cancellationToken)
    {
        return Task.FromResult(FirstDataDate);
    }

    public async IAsyncEnumerable<SummaryChunk> ImportSummariesAsync(string apiKey, DateOnly from, DateOnly to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        SummaryRanges.Add((from, to));
        await Task.Yield();
        yield return new SummaryChunk
        {
            Endpoint = "sleep-summaries",
            Raw = [new RawRecord { Endpoint = "sleep-summaries", Key = $"{to:yyyy-MM-dd}", Date = to, Json = """{"score":80}""" }],
            Sleep = [new SleepSession { Date = to, SleepSeconds = 28000, Score = 80 }],
        };
        yield return new SummaryChunk
        {
            Endpoint = "activities",
            Raw = [new RawRecord { Endpoint = "activity", Key = "42", Date = to, Json = """{"activityId":42}""" }],
            Activities =
            [
                new ActivityRecord
                {
                    SourceActivityId = "42", Name = "Futás", StartUtc = to.ToDateTime(new TimeOnly(7, 0), DateTimeKind.Utc),
                },
            ],
        };
    }

    public Task<DailyQueryResult> RunDailyQueryAsync(string apiKey, string kind, DateOnly date,
        CancellationToken cancellationToken)
    {
        DailyQueries.Add((kind, date));
        if (DailyFailure(kind, date) is { } failure)
            throw failure;

        var raw = new RawRecord
        {
            Endpoint = kind, Key = $"{date:yyyy-MM-dd}", Date = date, Json = EmptyDates.Contains(date) ? "null" : RawJson(kind, date),
        };
        if (EmptyDates.Contains(date))
            return Task.FromResult(new DailyQueryResult { Raw = raw });

        var midnight = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return Task.FromResult(new DailyQueryResult
        {
            Raw = raw,
            HeartRate = kind == GarminAdapterService.HeartRateQuery
                ? new DailyHeartRate
                {
                    Date = date, RestingBpm = 50,
                    Readings =
                    [
                        new HeartRateReading { TimestampUtc = midnight.AddMinutes(2), Bpm = 55 },
                        new HeartRateReading { TimestampUtc = midnight.AddMinutes(4), Bpm = 57 },
                    ],
                }
                : null,
            Activity = kind == GarminAdapterService.UserSummaryQuery ? new DailyActivity { Date = date, Steps = 8000 } : null,
        });
    }
}
