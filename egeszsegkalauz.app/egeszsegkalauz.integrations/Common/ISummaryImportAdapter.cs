namespace egeszsegkalauz.integrations.Common;

public interface ISummaryImportAdapter
{
    IAsyncEnumerable<SummaryChunk> ImportSummariesAsync(string apiKey, DateOnly from, DateOnly to,
        CancellationToken cancellationToken);
}
