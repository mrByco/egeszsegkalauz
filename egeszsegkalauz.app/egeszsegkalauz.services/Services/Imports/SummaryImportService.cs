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
        var result = new SummaryImportResultDto
        {
            To = today, RawRecords = 0, SleepNights = 0, BodyBatteryDays = 0, Vo2MaxReadings = 0,
            BodyCompositionReadings = 0, Activities = 0,
        };
        if (integration.FirstDataDate is not { } firstDate)
        {
            await db.SaveChangesAsync(cancellationToken);
            return result;
        }

        var from = integration.SummariesImportedThrough is { } through && through.AddDays(-OverlapDays) > firstDate
            ? through.AddDays(-OverlapDays)
            : firstDate;
        result.From = from;

        await foreach (var chunk in adapters.SummariesFor(source).ImportSummariesAsync(apiKey, from, today, cancellationToken))
        {
            await store.StoreSummaryChunkAsync(integration.User, source, chunk, cancellationToken);
            result.RawRecords += chunk.Raw.Count;
            result.SleepNights += chunk.Sleep.Count;
            result.BodyBatteryDays += chunk.BodyBattery.Count;
            result.Vo2MaxReadings += chunk.Vo2Max.Count;
            result.BodyCompositionReadings += chunk.BodyComposition.Count;
            result.Activities += chunk.Activities.Count;
        }

        integration.SummariesImportedAt = timeProvider.GetUtcNow().UtcDateTime;
        integration.SummariesImportedThrough = today;
        await db.SaveChangesAsync(cancellationToken);
        return result;
    }
}
