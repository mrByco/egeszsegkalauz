namespace egeszsegkalauz.integrations.Common;

public class DailyImportPolicy
{
    public required TimeSpan MinimumInterval { get; init; }
    public required int MaxAttempts { get; init; }
    public required Func<int, TimeSpan> RetryDelay { get; init; }
}
