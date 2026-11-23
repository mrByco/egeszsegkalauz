using egeszsegkalauz.data.Dev;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.integrations.Common;
using egeszsegkalauz.integrations.Garmin;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Services.HealthData;
using egeszsegkalauz.services.Services.Imports;
using egeszsegkalauz.services.Services.Integrations;
using egeszsegkalauz.tests.Integrations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace egeszsegkalauz.tests.Services;

public class DailyImportServiceTests
{
    private static readonly DateOnly Today = new(2026, 11, 22);

    private readonly ApiKeyProtector protector = new(new EphemeralDataProtectionProvider());
    private FakeTimeProvider time = new();
    private FakeGarminAdapter garmin = new();
    private ImportPacer pacer = new();
    private string adminNanoid = "";

    [SetUp]
    public async Task SeedAsync()
    {
        time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 22, 10, 0, 0, TimeSpan.Zero));
        garmin = new FakeGarminAdapter { FirstDataDate = Today.AddDays(-2) };
        pacer = new ImportPacer();
        await PostgresFixture.ResetAsync();
        await using var db = PostgresFixture.CreateDbContext();
        await DevDataSeeder.SeedAsync(db);
        adminNanoid = await db.Users.Where(x => x.Email == DevDataSeeder.AdminEmail).Select(x => x.Nanoid).SingleAsync();
    }

    [Test]
    public async Task StartQueuesOneQueryPerKindAndDay()
    {
        await ConnectAsync();

        var job = await RunAsync(service => service.StartAsync(adminNanoid, HealthDataSource.Garmin, CancellationToken.None));

        Assert.That((job.Status, job.TotalQueries), Is.EqualTo((ImportJobStatus.Running, 6)));
        var again = await RunAsync(service => service.StartAsync(adminNanoid, HealthDataSource.Garmin, CancellationToken.None));
        Assert.That(again.Nanoid, Is.EqualTo(job.Nanoid));
    }

    [Test]
    public async Task TheQueueIsDrainedNewestDayFirstAtTheSourcesPace()
    {
        await ConnectAsync();
        await StartAsync();

        await ProcessAsync();
        Assert.That(await ProcessAsync(), Is.EqualTo(TimeSpan.FromSeconds(1)), "the adapter allows one query per second");
        await DrainAsync();

        Assert.That(garmin.DailyQueries.First(), Is.EqualTo((GarminAdapterService.HeartRateQuery, Today)));
        var status = await StatusAsync();
        Assert.That((status.Status, status.CompletedQueries, status.FailedQueries), Is.EqualTo((ImportJobStatus.Completed, 6, 0)));

        await using var db = PostgresFixture.CreateDbContext();
        Assert.That(await db.DailyHeartRateSummaries.CountAsync(), Is.EqualTo(3));
        Assert.That(await db.HeartRateSamples.CountAsync(), Is.EqualTo(6));
        Assert.That(await db.DailyActivitySummaries.Select(x => x.Steps).ToListAsync(), Is.All.EqualTo(8000));
        Assert.That(await db.RawHealthRecords.CountAsync(), Is.EqualTo(6));
    }

    [Test]
    public async Task ARestartQueuesTheLastTwoWeeksAndRecentEmptyDaysAgain()
    {
        garmin.FirstDataDate = Today.AddDays(-40);
        garmin.EmptyDates.UnionWith([Today.AddDays(-20), Today.AddDays(-35)]);
        await ConnectAsync();
        await StartAsync();
        await DrainAsync();

        var job = await StartAsync();

        Assert.That(job.TotalQueries, Is.EqualTo((14 + 1) * 2), "the last 14 days, plus the empty day within 30 days");
    }

    [Test]
    public async Task AFailedStoreKeepsTheOldDataAndRetriesTheQuery()
    {
        await ConnectAsync();
        await StartAsync();
        await DrainAsync();
        await StartAsync();
        garmin.RawJson = (kind, date) => date == Today ? "{not json" : """{"ok":true}""";

        await ProcessAsync();

        await using var db = PostgresFixture.CreateDbContext();
        var raw = await db.RawHealthRecords.SingleAsync(x => x.Endpoint == GarminAdapterService.HeartRateQuery && x.Date == Today);
        Assert.That(raw.Payload, Does.Contain("ok"));
        Assert.That(await db.HeartRateSamples.CountAsync(x => x.Date == Today), Is.EqualTo(2));
        var item = await db.ImportQueueItems
            .Where(x => x.Job.Status == ImportJobStatus.Running && x.Date == Today && x.Kind == GarminAdapterService.HeartRateQuery)
            .SingleAsync();
        Assert.That((item.Status, item.Attempts), Is.EqualTo((ImportQueueItemStatus.Pending, 1)));
    }

    [Test]
    public async Task UnexpectedErrorsCountAsAttemptsSoTheQueueMovesOn()
    {
        await ConnectAsync();
        await StartAsync();
        garmin.DailyFailure = (_, date) => date == Today ? new InvalidOperationException("timeout") : null;

        await DrainAsync();

        var status = await StatusAsync();
        Assert.That((status.Status, status.CompletedQueries, status.FailedQueries), Is.EqualTo((ImportJobStatus.Completed, 4, 2)));
    }

    [Test]
    public async Task RateLimitingPostponesTheQueryAndPausesTheSource()
    {
        await ConnectAsync();
        await StartAsync();
        var limited = true;
        garmin.DailyFailure = (_, _) => limited ? new IntegrationRateLimitedException("slow down", TimeSpan.FromMinutes(2)) : null;

        await ProcessAsync();
        Assert.That(await ProcessAsync(), Is.EqualTo(TimeSpan.FromMinutes(2)));

        limited = false;
        time.Advance(TimeSpan.FromMinutes(2));
        await DrainAsync();
        Assert.That((await StatusAsync()).CompletedQueries, Is.EqualTo(6));
    }

    [Test]
    public async Task AQueryFailsAfterTheAdaptersMaxAttemptsButTheJobContinues()
    {
        await ConnectAsync();
        await StartAsync();
        garmin.DailyFailure = (kind, date) =>
            kind == GarminAdapterService.UserSummaryQuery && date == Today ? new IntegrationUnavailableException("boom") : null;

        await DrainAsync();

        var status = await StatusAsync();
        Assert.That((status.Status, status.CompletedQueries, status.FailedQueries), Is.EqualTo((ImportJobStatus.Completed, 5, 1)));
        Assert.That(status.LastError, Is.EqualTo("boom"));
    }

    [Test]
    public async Task ARejectedKeyFailsTheJob()
    {
        await ConnectAsync();
        await StartAsync();
        garmin.DailyFailure = (_, _) => new IntegrationKeyRejectedException("Garmin rejected the API key.");

        await ProcessAsync();

        Assert.That((await StatusAsync()).Status, Is.EqualTo(ImportJobStatus.Failed));
    }

    [Test]
    public async Task CancelStopsTheJob()
    {
        await ConnectAsync();
        await StartAsync();

        await RunAsync(async service =>
        {
            await service.CancelAsync(adminNanoid, HealthDataSource.Garmin, CancellationToken.None);
            return true;
        });
        await ProcessAsync();

        Assert.That((await StatusAsync()).Status, Is.EqualTo(ImportJobStatus.Cancelled));
        Assert.That(garmin.DailyQueries, Is.Empty);
    }

    private async Task DrainAsync()
    {
        for (var step = 0; step < 1000; step++)
        {
            var delay = await ProcessAsync();
            if ((await StatusAsync()).Status != ImportJobStatus.Running)
                return;
            time.Advance(delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1));
        }

        throw new AssertionException("The import did not finish.");
    }

    private async Task<TimeSpan> ProcessAsync()
    {
        return await RunAsync(service => service.ProcessNextAsync(CancellationToken.None));
    }

    private async Task<ImportJobDto> StartAsync()
    {
        return await RunAsync(service => service.StartAsync(adminNanoid, HealthDataSource.Garmin, CancellationToken.None));
    }

    private async Task<ImportJobDto> StatusAsync()
    {
        var status = await RunAsync(service => service.GetStatusAsync(adminNanoid, HealthDataSource.Garmin, CancellationToken.None));
        return status.Job ?? throw new AssertionException("No import job.");
    }

    private async Task ConnectAsync()
    {
        await using var db = PostgresFixture.CreateDbContext();
        await new IntegrationService(db, new HealthDataAdapters(garmin), protector)
            .ConnectAsync(adminNanoid, HealthDataSource.Garmin, FakeGarminAdapter.ValidKey, CancellationToken.None);
    }

    private async Task<T> RunAsync<T>(Func<DailyImportService, Task<T>> action)
    {
        await using var db = PostgresFixture.CreateDbContext();
        var service = new DailyImportService(db, new HealthDataAdapters(garmin), protector, new HealthDataStore(db, time),
            pacer, time);
        return await action(service);
    }
}
