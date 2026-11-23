using egeszsegkalauz.data;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.integrations.Common;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Exceptions;
using egeszsegkalauz.services.Services.HealthData;
using egeszsegkalauz.services.Services.Integrations;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.Imports;

public class DailyImportService : IDailyImportService
{
    public static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(5);

    // Devices sync late (e.g. days away from the phone): recent days are always queried again, and days that
    // came back empty are asked again for a month. Older empty days are before the device or off-wrist.
    private const int AlwaysRefreshedDays = 14;
    private const int EmptyDayRetryDays = 30;
    private const string EmptyPayload = "null";

    private readonly AppDbContext db;
    private readonly HealthDataAdapters adapters;
    private readonly IApiKeyProtector apiKeyProtector;
    private readonly HealthDataStore store;
    private readonly ImportPacer pacer;
    private readonly TimeProvider timeProvider;

    public DailyImportService(AppDbContext db, HealthDataAdapters adapters, IApiKeyProtector apiKeyProtector,
        HealthDataStore store, ImportPacer pacer, TimeProvider timeProvider)
    {
        this.db = db;
        this.adapters = adapters;
        this.apiKeyProtector = apiKeyProtector;
        this.store = store;
        this.pacer = pacer;
        this.timeProvider = timeProvider;
    }

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<ImportJobDto> StartAsync(string userNanoid, HealthDataSource source, CancellationToken cancellationToken)
    {
        var integration = await FindIntegrationAsync(userNanoid, source, cancellationToken);
        var running = await db.ImportJobs.SingleOrDefaultAsync(
            x => x.UserId == integration.UserId && x.Source == source && x.Status == ImportJobStatus.Running, cancellationToken);
        if (running is not null)
            return ToDto(running);

        var today = DateOnly.FromDateTime(Now);
        integration.FirstDataDate ??= await adapters.For(source)
            .FindFirstDataDateAsync(apiKeyProtector.Unprotect(integration.ProtectedApiKey), today, cancellationToken);

        var job = new ImportJob { User = integration.User, Source = source, Status = ImportJobStatus.Running };
        if (integration.FirstDataDate is { } firstDate)
            job.Items = await PlanQueriesAsync(job, integration.UserId, firstDate, today, cancellationToken);
        job.TotalQueries = job.Items.Count;
        if (job.TotalQueries == 0)
        {
            job.Status = ImportJobStatus.Completed;
            job.FinishedAt = Now;
        }

        db.ImportJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(job);
    }

    public async Task<DailyImportStatusDto> GetStatusAsync(string userNanoid, HealthDataSource source,
        CancellationToken cancellationToken)
    {
        var job = await db.ImportJobs
            .AsNoTracking()
            .Where(x => x.User.Nanoid == userNanoid && x.Source == source)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return new DailyImportStatusDto { Job = job is null ? null : ToDto(job) };
    }

    public async Task CancelAsync(string userNanoid, HealthDataSource source, CancellationToken cancellationToken)
    {
        var now = Now;
        await db.ImportJobs
            .Where(x => x.User.Nanoid == userNanoid && x.Source == source && x.Status == ImportJobStatus.Running)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, ImportJobStatus.Cancelled)
                .SetProperty(x => x.FinishedAt, now)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);
    }

    public async Task<TimeSpan> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var now = Now;
        var item = await db.ImportQueueItems
            .Include(x => x.Job)
            .ThenInclude(x => x.User)
            .Where(x => x.Status == ImportQueueItemStatus.Pending && x.NotBefore <= now
                        && x.Job.Status == ImportJobStatus.Running)
            .OrderBy(x => x.JobId)
            .ThenByDescending(x => x.Date)
            .ThenBy(x => x.Kind)
            .FirstOrDefaultAsync(cancellationToken);
        if (item is null)
            return await FinishDrainedJobsAsync(now, cancellationToken);

        var job = item.Job;

        var wait = pacer.WaitTime(job.Source, now);
        if (wait > TimeSpan.Zero)
            return wait;

        var integration = await db.UserIntegrations
            .SingleOrDefaultAsync(x => x.UserId == job.UserId && x.Source == job.Source, cancellationToken);
        if (integration is null)
        {
            Fail(job, $"{job.Source} is no longer connected.");
            await db.SaveChangesAsync(cancellationToken);
            return TimeSpan.Zero;
        }

        var adapter = adapters.DailyFor(job.Source);
        var policy = adapter.Policy;
        pacer.Hold(job.Source, now + policy.MinimumInterval);
        DailyQueryResult result;
        try
        {
            result = await adapter.RunDailyQueryAsync(apiKeyProtector.Unprotect(integration.ProtectedApiKey),
                item.Kind, item.Date, cancellationToken);
        }
        catch (IntegrationKeyRejectedException e)
        {
            Fail(job, e.Message);
            await db.SaveChangesAsync(cancellationToken);
            return TimeSpan.Zero;
        }
        catch (IntegrationRateLimitedException e)
        {
            item.NotBefore = now + e.Pause;
            job.LastError = e.Message;
            pacer.Hold(job.Source, now + e.Pause);
            await db.SaveChangesAsync(cancellationToken);
            return TimeSpan.Zero;
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested)
        {
            RegisterFailedAttempt(item, job, e.Message, policy, now);
            await db.SaveChangesAsync(cancellationToken);
            return TimeSpan.Zero;
        }

        item.Status = ImportQueueItemStatus.Done;
        item.Attempts++;
        job.CompletedQueries++;
        try
        {
            await store.StoreDailyResultAsync(job.User, job.Source, result, cancellationToken);
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested)
        {
            db.ChangeTracker.Clear();
            var failedItem = await db.ImportQueueItems.Include(x => x.Job).SingleAsync(x => x.Id == item.Id, cancellationToken);
            RegisterFailedAttempt(failedItem, failedItem.Job, e.Message, policy, now);
            await db.SaveChangesAsync(cancellationToken);
        }

        return TimeSpan.Zero;
    }

    private static void RegisterFailedAttempt(ImportQueueItem item, ImportJob job, string message, DailyImportPolicy policy,
        DateTime now)
    {
        item.Attempts++;
        item.LastError = message;
        job.LastError = message;
        if (item.Attempts >= policy.MaxAttempts)
        {
            item.Status = ImportQueueItemStatus.Failed;
            job.FailedQueries++;
        }
        else
        {
            item.NotBefore = now + policy.RetryDelay(item.Attempts);
        }
    }

    private async Task<List<ImportQueueItem>> PlanQueriesAsync(ImportJob job, int userId, DateOnly firstDate,
        DateOnly today, CancellationToken cancellationToken)
    {
        var source = job.Source;
        var refreshFrom = today.AddDays(1 - AlwaysRefreshedDays);
        var retryEmptyFrom = today.AddDays(1 - EmptyDayRetryDays);
        var now = Now;
        var items = new List<ImportQueueItem>();
        foreach (var kind in adapters.DailyFor(source).DailyQueryKinds)
        {
            var fetched = (await db.RawHealthRecords
                    .Where(x => x.UserId == userId && x.Source == source && x.Endpoint == kind && x.Date >= firstDate)
                    .Where(x => x.Date < retryEmptyFrom || x.Payload != EmptyPayload)
                    .Select(x => x.Date)
                    .ToListAsync(cancellationToken))
                .OfType<DateOnly>()
                .ToHashSet();

            for (var date = firstDate; date <= today; date = date.AddDays(1))
            {
                if (date >= refreshFrom || !fetched.Contains(date))
                    items.Add(new ImportQueueItem { Job = job, Kind = kind, Date = date, NotBefore = now });
            }
        }

        return items;
    }

    private async Task<TimeSpan> FinishDrainedJobsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var drained = await db.ImportJobs
            .Where(x => x.Status == ImportJobStatus.Running && !x.Items.Any(i => i.Status == ImportQueueItemStatus.Pending))
            .ToListAsync(cancellationToken);
        foreach (var job in drained)
        {
            job.Status = ImportJobStatus.Completed;
            job.FinishedAt = now;
        }

        if (drained.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        var nextDue = await db.ImportQueueItems
            .Where(x => x.Status == ImportQueueItemStatus.Pending && x.Job.Status == ImportJobStatus.Running)
            .MinAsync(x => (DateTime?)x.NotBefore, cancellationToken);
        return nextDue is { } due && due - now < IdleDelay ? due - now : IdleDelay;
    }

    private async Task<UserIntegration> FindIntegrationAsync(string userNanoid, HealthDataSource source,
        CancellationToken cancellationToken)
    {
        return await db.UserIntegrations
                   .Include(x => x.User)
                   .SingleOrDefaultAsync(x => x.User.Nanoid == userNanoid && x.Source == source, cancellationToken)
               ?? throw new NotFoundException($"{source} is not connected.");
    }

    private void Fail(ImportJob job, string message)
    {
        job.Status = ImportJobStatus.Failed;
        job.LastError = message;
        job.FinishedAt = Now;
    }

    private static ImportJobDto ToDto(ImportJob job)
    {
        return new ImportJobDto
        {
            Nanoid = job.Nanoid,
            Source = job.Source,
            Status = job.Status,
            TotalQueries = job.TotalQueries,
            CompletedQueries = job.CompletedQueries,
            FailedQueries = job.FailedQueries,
            CreatedAt = job.CreatedAt,
            FinishedAt = job.FinishedAt,
            LastError = job.LastError,
        };
    }
}
