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
    public Func<string, DateOnly, string?> RawJson { get; set; } = (_, _) => null;

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

    public async IAsyncEnumerable<SyncChunk> ImportSummariesAsync(string apiKey, DateOnly from, DateOnly to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        SummaryRanges.Add((from, to));
        await Task.Yield();
        yield return new SyncChunk
        {
            Records =
            [
                new RawRecord
                {
                    Endpoint = GarminAdapterService.SleepSummariesEndpoint, Key = $"{to:yyyy-MM-dd}", Date = to,
                    Json = $"{{\"calendarDate\":\"{to:yyyy-MM-dd}\",\"sleepTimeSeconds\":28000,\"sleepScores\":{{\"overall\":{{\"value\":80}}}}}}",
                },
            ],
        };
        yield return new SyncChunk
        {
            Records = [new RawRecord { Endpoint = GarminAdapterService.ActivityEndpoint, Key = "42", Date = to, Json = GarminSamples.Activity }],
        };
    }

    public Task<SyncChunk> RunDailyQueryAsync(string apiKey, string kind, DateOnly date, CancellationToken cancellationToken)
    {
        DailyQueries.Add((kind, date));
        if (DailyFailure(kind, date) is { } failure)
            throw failure;

        var json = EmptyDates.Contains(date)
            ? "null"
            : RawJson(kind, date) ?? (kind == GarminAdapterService.HeartRateQuery ? GarminSamples.HeartRateDay(date) : GarminSamples.UserSummary(date));
        return Task.FromResult(new SyncChunk
        {
            Records = [new RawRecord { Endpoint = kind, Key = $"{date:yyyy-MM-dd}", Date = date, Json = json }],
        });
    }
}
