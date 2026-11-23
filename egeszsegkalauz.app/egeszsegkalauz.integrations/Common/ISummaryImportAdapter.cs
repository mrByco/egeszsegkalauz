namespace egeszsegkalauz.integrations.Common;

public interface ISummaryImportAdapter
{
    IAsyncEnumerable<SyncChunk> ImportSummariesAsync(string apiKey, DateOnly from, DateOnly to,
        CancellationToken cancellationToken);
}
