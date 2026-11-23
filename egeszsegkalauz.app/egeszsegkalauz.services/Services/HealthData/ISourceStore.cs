using egeszsegkalauz.integrations.Common;

namespace egeszsegkalauz.services.Services.HealthData;

// Turns a source's raw records into that source's own tables, in the source's own shape.
public interface ISourceStore
{
    Task ApplyAsync(int userId, IReadOnlyList<RawRecord> records, CancellationToken cancellationToken);
    Task DeleteAsync(int userId, IReadOnlyList<RawRecordKey> keys, CancellationToken cancellationToken);
    Task DeleteAllAsync(int userId, CancellationToken cancellationToken);
}
