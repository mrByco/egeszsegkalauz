using egeszsegkalauz.data;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Exceptions;
using egeszsegkalauz.services.Services.HealthData;
using egeszsegkalauz.services.Services.Integrations;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.Imports;

public class SummaryImportService : ISummaryImportService
{
    // Devices sync late (e.g. days away from the phone), so an incremental import re-reads the last two weeks.
    private const int OverlapDays = 14;

    private readonly AppDbContext db;
    private readonly HealthDataAdapters adapters;
    private readonly IApiKeyProtector apiKeyProtector;
    private readonly HealthDataStore store;
    private readonly TimeProvider timeProvider;

    public SummaryImportService(AppDbContext db, HealthDataAdapters adapters, IApiKeyProtector apiKeyProtector,
        HealthDataStore store, TimeProvider timeProvider)
    {
        this.db = db;
        this.adapters = adapters;
        this.apiKeyProtector = apiKeyProtector;
        this.store = store;
        this.timeProvider = timeProvider;
    }

    public async Task<SummaryImportResultDto> ImportAsync(string userNanoid, HealthDataSource source,
        CancellationToken cancellationToken)
    {
        var integration = await db.UserIntegrations
                              .Include(x => x.User)
                              .SingleOrDefaultAsync(x => x.User.Nanoid == userNanoid && x.Source == source, cancellationToken)
                          ?? throw new NotFoundException($"{source} is not connected.");
        var apiKey = apiKeyProtector.Unprotect(integration.ProtectedApiKey);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        integration.FirstDataDate ??= await adapters.For(source).FindFirstDataDateAsync(apiKey, today, cancellationToken);
        var result = new SummaryImportResultDto { To = today, Endpoints = [] };
        if (integration.FirstDataDate is not { } firstDate)
        {
            await db.SaveChangesAsync(cancellationToken);
            return result;
        }

        var from = integration.SummariesImportedThrough is { } through && through.AddDays(-OverlapDays) > firstDate
            ? through.AddDays(-OverlapDays)
            : firstDate;
        result.From = from;

        var counts = new Dictionary<string, int>();
        await foreach (var chunk in adapters.SummariesFor(source).ImportSummariesAsync(apiKey, from, today, cancellationToken))
        {
            await store.StoreChunkAsync(integration.UserId, source, chunk, cancellationToken);
            foreach (var record in chunk.Records)
                counts[record.Endpoint] = counts.GetValueOrDefault(record.Endpoint) + 1;
        }

        result.Endpoints = counts.OrderBy(x => x.Key)
            .Select(x => new EndpointRecordCountDto { Endpoint = x.Key, Records = x.Value })
            .ToList();

        integration.SummariesImportedAt = timeProvider.GetUtcNow().UtcDateTime;
        integration.SummariesImportedThrough = today;
        await db.SaveChangesAsync(cancellationToken);
        return result;
    }
}
