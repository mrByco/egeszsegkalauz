using egeszsegkalauz.data.Dev;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.Exceptions;
using egeszsegkalauz.services.Services.HealthData;
using egeszsegkalauz.services.Services.Imports;
using egeszsegkalauz.services.Services.Integrations;
using egeszsegkalauz.tests.Integrations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace egeszsegkalauz.tests.Services;

public class SummaryImportServiceTests
{
    private static readonly DateOnly Today = new(2026, 11, 22);

    private readonly ApiKeyProtector protector = new(new EphemeralDataProtectionProvider());
    private FakeTimeProvider time = new();
    private FakeGarminAdapter garmin = new();
    private string adminNanoid = "";

    [SetUp]
    public async Task SeedAsync()
    {
        time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 22, 10, 0, 0, TimeSpan.Zero));
        garmin = new FakeGarminAdapter { FirstDataDate = new DateOnly(2022, 1, 1) };
        await PostgresFixture.ResetAsync();
        await using var db = PostgresFixture.CreateDbContext();
        await DevDataSeeder.SeedAsync(db);
        adminNanoid = await db.Users.Where(x => x.Email == DevDataSeeder.AdminEmail).Select(x => x.Nanoid).SingleAsync();
    }

    [Test]
    public async Task ImportFailsWhenTheSourceIsNotConnected()
    {
        await Assert.ThatAsync(ImportAsync, Throws.InstanceOf<NotFoundException>());
    }

    [Test]
    public async Task FirstImportCoversTheWholeHistoryAndStoresRawAndTypedData()
    {
        await ConnectAsync();

        var result = await ImportAsync();

        Assert.That(garmin.SummaryRanges.Single(), Is.EqualTo((new DateOnly(2022, 1, 1), Today)));
        Assert.That(result.From, Is.EqualTo(new DateOnly(2022, 1, 1)));
        Assert.That(result.Endpoints.Select(x => (x.Endpoint, x.Records)), Is.EqualTo(new[] { ("activity", 1), ("sleep-summaries", 1) }));

        await using var db = PostgresFixture.CreateDbContext();
        Assert.That((await db.GarminSleepSummaries.SingleAsync()).SleepScoresOverallValue, Is.EqualTo(80));
        Assert.That((await db.GarminActivities.SingleAsync()).ActivityName, Is.EqualTo("Futás"));
        Assert.That(await db.RawHealthRecords.Select(x => x.Endpoint).ToListAsync(), Is.EquivalentTo(new[] { "sleep-summaries", "activity" }));
        var integration = await db.UserIntegrations.SingleAsync();
        Assert.That((integration.FirstDataDate, integration.SummariesImportedThrough),
            Is.EqualTo(((DateOnly?)new DateOnly(2022, 1, 1), (DateOnly?)Today)));
    }

    [Test]
    public async Task LaterImportsReReadOnlyTheLastTwoWeeks()
    {
        await ConnectAsync();
        await ImportAsync();

        time.Advance(TimeSpan.FromDays(3));
        await ImportAsync();

        Assert.That(garmin.SummaryRanges[1], Is.EqualTo((Today.AddDays(-14), Today.AddDays(3))));
        await using (var db = PostgresFixture.CreateDbContext())
        {
            Assert.That(await db.GarminActivities.CountAsync(), Is.EqualTo(1));
            Assert.That(await db.GarminSleepSummaries.CountAsync(), Is.EqualTo(2));
        }
    }

    [Test]
    public async Task NothingIsImportedWhenTheSourceHasNoData()
    {
        garmin.FirstDataDate = null;
        await ConnectAsync();

        var result = await ImportAsync();

        Assert.That(result.From, Is.Null);
        Assert.That(garmin.SummaryRanges, Is.Empty);
    }

    private async Task ConnectAsync()
    {
        await using var db = PostgresFixture.CreateDbContext();
        await new IntegrationService(db, new HealthDataAdapters(garmin, new FakeHevyAdapter()), protector, TestStores.Create(db, time))
            .ConnectAsync(adminNanoid, HealthDataSource.Garmin, FakeGarminAdapter.ValidKey, CancellationToken.None);
    }

    private async Task<egeszsegkalauz.services.DTO.SummaryImportResultDto> ImportAsync()
    {
        await using var db = PostgresFixture.CreateDbContext();
        var service = new SummaryImportService(db, new HealthDataAdapters(garmin, new FakeHevyAdapter()), protector, TestStores.Create(db, time), time);
        return await service.ImportAsync(adminNanoid, HealthDataSource.Garmin, CancellationToken.None);
    }
}
