namespace egeszsegkalauz.integrations.Common;

public interface ICursorSyncAdapter
{
    // Without since, yields everything; with it, only what changed or was deleted since then.
    IAsyncEnumerable<SyncChunk> SyncAsync(string accessToken, DateTime? since, CancellationToken cancellationToken);

    // Endpoints a sync lists completely, so stored records of theirs that the sync did not return are gone.
    IReadOnlyList<string> CompletelyListedEndpoints(bool fullSync);
}
