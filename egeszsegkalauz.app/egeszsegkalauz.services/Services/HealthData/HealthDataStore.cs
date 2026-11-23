using egeszsegkalauz.data;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.integrations.Common;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.HealthData;

// Our database is a buffer of what each source returned: raw records verbatim, plus each source's own tables.
public class HealthDataStore
{
    private readonly AppDbContext db;
    private readonly SourceStores sourceStores;
    private readonly TimeProvider timeProvider;

    public HealthDataStore(AppDbContext db, SourceStores sourceStores, TimeProvider timeProvider)
    {
        this.db = db;
        this.sourceStores = sourceStores;
        this.timeProvider = timeProvider;
    }

    // Rows are replaced by deleting first (ExecuteDelete runs immediately), so each store is one transaction:
    // a failed save must not leave the old rows deleted.
    public async Task StoreChunkAsync(int userId, HealthDataSource source, SyncChunk chunk, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await StoreRawAsync(userId, source, chunk.Records, cancellationToken);
        await DeleteRawAsync(userId, source, chunk.Deleted, cancellationToken);
        var sourceStore = sourceStores.For(source);
        await sourceStore.ApplyAsync(userId, chunk.Records, cancellationToken);
        await sourceStore.DeleteAsync(userId, chunk.Deleted, cancellationToken);
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<int> DeleteMissingAsync(int userId, HealthDataSource source, string endpoint,
        IReadOnlyCollection<string> keep, CancellationToken cancellationToken)
    {
        var missing = (await db.RawHealthRecords
                .Where(x => x.UserId == userId && x.Source == source && x.Endpoint == endpoint)
                .Select(x => x.Key)
                .ToListAsync(cancellationToken))
            .Where(key => !keep.Contains(key))
            .Select(key => new RawRecordKey { Endpoint = endpoint, Key = key })
            .ToList();
        if (missing.Count > 0)
            await StoreChunkAsync(userId, source, new SyncChunk { Deleted = missing }, cancellationToken);
        return missing.Count;
    }

    public async Task DeleteSourceDataAsync(int userId, HealthDataSource source, CancellationToken cancellationToken)
    {
        await db.RawHealthRecords.Where(x => x.UserId == userId && x.Source == source).ExecuteDeleteAsync(cancellationToken);
        await sourceStores.For(source).DeleteAllAsync(userId, cancellationToken);
        await db.ImportJobs.Where(x => x.UserId == userId && x.Source == source).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task StoreRawAsync(int userId, HealthDataSource source, IReadOnlyCollection<RawRecord> records,
        CancellationToken cancellationToken)
    {
        var fetchedAt = timeProvider.GetUtcNow().UtcDateTime;
        var unique = records.DistinctBy(x => (x.Endpoint, x.Key)).ToList();
        await DeleteRawAsync(userId, source, unique.Select(x => new RawRecordKey { Endpoint = x.Endpoint, Key = x.Key }).ToList(),
            cancellationToken);

        db.RawHealthRecords.AddRange(unique.Select(x => new RawHealthRecord
        {
            UserId = userId,
            Source = source,
            Endpoint = x.Endpoint,
            Key = x.Key,
            Date = x.Date,
            Payload = x.Json,
            FetchedAt = fetchedAt,
        }));
    }

    private async Task DeleteRawAsync(int userId, HealthDataSource source, IReadOnlyCollection<RawRecordKey> keys,
        CancellationToken cancellationToken)
    {
        foreach (var group in keys.GroupBy(x => x.Endpoint))
        {
            var groupKeys = group.Select(x => x.Key).ToList();
            await db.RawHealthRecords
                .Where(x => x.UserId == userId && x.Source == source && x.Endpoint == group.Key && groupKeys.Contains(x.Key))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);

        // Imports write hundreds of thousands of rows through one context; keep only the bookkeeping rows tracked.
        foreach (var entry in db.ChangeTracker.Entries()
                     .Where(x => x.Entity is not (User or UserIntegration or ImportJob or ImportQueueItem)).ToList())
            entry.State = EntityState.Detached;
    }
}
