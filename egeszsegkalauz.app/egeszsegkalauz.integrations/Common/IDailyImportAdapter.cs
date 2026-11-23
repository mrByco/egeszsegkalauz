namespace egeszsegkalauz.integrations.Common;

public interface IDailyImportAdapter
{
    IReadOnlyList<string> DailyQueryKinds { get; }
    DailyImportPolicy Policy { get; }

    Task<DailyQueryResult> RunDailyQueryAsync(string apiKey, string kind, DateOnly date,
        CancellationToken cancellationToken);
}
