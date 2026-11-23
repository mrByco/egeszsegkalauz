using egeszsegkalauz.data;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Exceptions;
using egeszsegkalauz.services.Services.HealthData;
using egeszsegkalauz.services.Services.Integrations;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.Sync;

public class SourceSyncService : ISourceSyncService
{
    public static readonly TimeSpan SyncInterval = TimeSpan.FromHours(1);

    // Changes are read from a little before the previous sync, so one saved during that sync is not missed.
    private static readonly TimeSpan CursorOverlap = TimeSpan.FromMinutes(10);

    private static readonly HealthDataSource[] SyncedSources = Enum.GetValues<HealthDataSource>()
        .Where(x => HealthDataAdapters.Describe(x).Capabilities.Contains(IntegrationCapability.Sync))
        .ToArray();

    private readonly AppDbContext db;
    private readonly HealthDataAdapters adapters;
    private readonly AccessTokenProvider accessTokens;
    private readonly HealthDataStore store;
    private readonly TimeProvider timeProvider;

    public SourceSyncService(AppDbContext db, HealthDataAdapters adapters, AccessTokenProvider accessTokens,
        HealthDataStore store, TimeProvider timeProvider)
    {
        this.db = db;
        this.adapters = adapters;
        this.accessTokens = accessTokens;
        this.store = store;
        this.timeProvider = timeProvider;
    }

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<SyncResultDto> SyncAsync(string userNanoid, HealthDataSource source, CancellationToken cancellationToken)
    {
        var integration = await db.UserIntegrations
                              .SingleOrDefaultAsync(x => x.User.Nanoid == userNanoid && x.Source == source, cancellationToken)
                          ?? throw new NotFoundException($"{source} is not connected.");
        return await SyncIntegrationAsync(integration, cancellationToken);
    }

    public async Task<bool> SyncNextDueAsync(CancellationToken cancellationToken)
    {
        var now = Now;
        var candidates = await db.UserIntegrations
            .Where(x => SyncedSources.Contains(x.Source) && x.LastSyncAt != null && x.LastSyncAttemptAt < now - SyncInterval)
            .OrderBy(x => x.LastSyncAttemptAt)
            .ToListAsync(cancellationToken);

        // Spread users over the hour instead of everyone syncing at the same minute.
        var due = candidates.FirstOrDefault(x => x.LastSyncAttemptAt + SyncInterval + Jitter(x) <= now);
        if (due is null)
            return false;

        try
        {
            await SyncIntegrationAsync(due, cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // SyncIntegrationAsync recorded the error on the integration; the next attempt is an hour later.
        }

        return true;
    }

    public async Task<bool> HandleWebhookAsync(HealthDataSource source, string body, Func<string, string?> header,
        CancellationToken cancellationToken)
    {
        var webhooks = adapters.WebhookFor(source);
        if (!webhooks.IsSignatureValid(body, header(webhooks.SignatureHeader)))
            return false;

        if (webhooks.Parse(body) is not { } notification)
            return true;

        var integration = await db.UserIntegrations
            .FirstOrDefaultAsync(x => x.Source == source && x.ExternalUserId == notification.ExternalUserId, cancellationToken);
        if (integration is null)
            return true;

        var accessToken = await accessTokens.GetAccessTokenAsync(integration, cancellationToken);
        var chunk = await webhooks.FetchAsync(accessToken, notification, cancellationToken);
        await store.StoreChunkAsync(integration.UserId, source, chunk, cancellationToken);
        return true;
    }

    private async Task<SyncResultDto> SyncIntegrationAsync(UserIntegration integration, CancellationToken cancellationToken)
    {
        var startedAt = Now;
        integration.LastSyncAttemptAt = startedAt;
        var since = integration.LastSyncAt - CursorOverlap;
        var adapter = adapters.SyncFor(integration.Source);

        try
        {
            var accessToken = await accessTokens.GetAccessTokenAsync(integration, cancellationToken);
            var seen = new Dictionary<string, HashSet<string>>();
            var deleted = 0;
            await foreach (var chunk in adapter.SyncAsync(accessToken, since, cancellationToken))
            {
                await store.StoreChunkAsync(integration.UserId, integration.Source, chunk, cancellationToken);
                deleted += chunk.Deleted.Count;
                foreach (var record in chunk.Records)
                {
                    if (!seen.TryGetValue(record.Endpoint, out var keys))
                        seen[record.Endpoint] = keys = [];
                    keys.Add(record.Key);
                }
            }

            foreach (var endpoint in adapter.CompletelyListedEndpoints(fullSync: since is null))
                deleted += await store.DeleteMissingAsync(integration.UserId, integration.Source, endpoint,
                    seen.GetValueOrDefault(endpoint) ?? [], cancellationToken);

            integration.LastSyncAt = startedAt;
            integration.LastSyncError = null;
            await db.SaveChangesAsync(cancellationToken);
            return new SyncResultDto
            {
                FullSync = since is null,
                Deleted = deleted,
                Endpoints = seen.OrderBy(x => x.Key)
                    .Select(x => new EndpointRecordCountDto { Endpoint = x.Key, Records = x.Value.Count })
                    .ToList(),
            };
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested)
        {
            integration.LastSyncError = e.Message;
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private static TimeSpan Jitter(UserIntegration integration) => TimeSpan.FromMinutes(integration.Id * 7 % 15);
}
