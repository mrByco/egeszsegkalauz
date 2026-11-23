using egeszsegkalauz.data.Dev;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.integrations.Common;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Exceptions;
using egeszsegkalauz.services.Services.HealthData;
using egeszsegkalauz.services.Services.Integrations;
using egeszsegkalauz.services.Services.Sync;
using egeszsegkalauz.tests.Integrations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace egeszsegkalauz.tests.Services;

public class SourceSyncServiceTests
{
    private readonly EphemeralDataProtectionProvider dataProtection = new();
    private readonly ApiKeyProtector protector;
    private FakeTimeProvider time = new();
    private FakeHevyAdapter hevy = new();
    private string adminNanoid = "";
    private string viewerNanoid = "";

    public SourceSyncServiceTests()
    {
        protector = new ApiKeyProtector(dataProtection);
    }

    [SetUp]
    public async Task SeedAsync()
    {
        time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 22, 10, 0, 0, TimeSpan.Zero));
        hevy = new FakeHevyAdapter();
        await PostgresFixture.ResetAsync();
        await using var db = PostgresFixture.CreateDbContext();
        await DevDataSeeder.SeedAsync(db);
        adminNanoid = await db.Users.Where(x => x.Email == DevDataSeeder.AdminEmail).Select(x => x.Nanoid).SingleAsync();
        viewerNanoid = await db.Users.Where(x => x.Email == DevDataSeeder.ViewerEmail).Select(x => x.Nanoid).SingleAsync();
    }

    [Test]
    public async Task ConnectingStoresEncryptedTokensForTheUserWhoStartedIt()
    {
        var connected = await ConnectAsync(adminNanoid);

        Assert.That((connected.Connected, connected.AccountName), Is.EqualTo((true, (string?)"teszt_elek")));
        await using var db = PostgresFixture.CreateDbContext();
        var stored = await db.UserIntegrations.SingleAsync();
        Assert.That(stored.ProtectedApiKey, Does.Not.Contain("access-1"));
        Assert.That(protector.Unprotect(stored.ProtectedApiKey), Is.EqualTo("access-1"));
        Assert.That(stored.ExternalUserId, Is.EqualTo("hevy-user-1"));
    }

    [Test]
    public async Task AStateIssuedToAnotherUserIsRejected()
    {
        var start = await RunOAuthAsync(service => Task.FromResult(service.Start(adminNanoid, HealthDataSource.Hevy)));
        var state = Uri.UnescapeDataString(start.AuthorizationUrl.Split("state=")[1]);

        await Assert.ThatAsync(() => RunOAuthAsync(service => service.CompleteAsync(viewerNanoid, HealthDataSource.Hevy,
                new CompleteOAuthRequest { Code = FakeHevyAdapter.ValidCode, State = state }, CancellationToken.None)),
            Throws.InstanceOf<InvalidInputException>());
    }

    [Test]
    public async Task OneHevyAccountCannotBeConnectedToTwoUsers()
    {
        await ConnectAsync(adminNanoid);

        await Assert.ThatAsync(() => ConnectAsync(viewerNanoid), Throws.InstanceOf<ConflictException>());
    }

    [Test]
    public async Task TheFirstSyncIsFullAndBuffersEverythingHevyReturned()
    {
        await ConnectAsync(adminNanoid);

        var result = await SyncAsync();

        Assert.That(result.FullSync, Is.True);
        Assert.That(result.Endpoints.Select(x => (x.Endpoint, x.Records)),
            Is.EqualTo(new[] { ("body-measurement", 1), ("exercise-template", 1), ("workout", 2) }));
        Assert.That(hevy.SyncSince.Single(), Is.Null);
        await using var db = PostgresFixture.CreateDbContext();
        Assert.That(await db.HevyWorkouts.CountAsync(), Is.EqualTo(2));
        Assert.That(await db.HevyWorkoutSets.CountAsync(), Is.EqualTo(4));
        Assert.That(await db.HevyExerciseTemplates.CountAsync(), Is.EqualTo(1));
        Assert.That(await db.HevyBodyMeasurements.CountAsync(), Is.EqualTo(1));
        Assert.That(await db.RawHealthRecords.CountAsync(x => x.Endpoint == "workout"), Is.EqualTo(2));
    }

    [Test]
    public async Task AFullSyncRemovesWorkoutsHevyNoLongerHas()
    {
        await ConnectAsync(adminNanoid);
        await SyncAsync();
        await using (var db = PostgresFixture.CreateDbContext())
            await db.UserIntegrations.ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSyncAt, (DateTime?)null));
        hevy.WorkoutIds = ["w1"];

        var result = await SyncAsync();

        Assert.That(result.Deleted, Is.EqualTo(1));
        await using (var db = PostgresFixture.CreateDbContext())
            Assert.That(await db.HevyWorkouts.Select(x => x.Id).ToListAsync(), Is.EqualTo(new[] { "w1" }));
    }

    [Test]
    public async Task LaterSyncsReadEventsAndApplyEditsAndDeletions()
    {
        await ConnectAsync(adminNanoid);
        await SyncAsync();

        time.Advance(TimeSpan.FromHours(2));
        hevy.WorkoutIds = ["w1"];
        hevy.DeletedWorkoutIds = ["w2"];
        var result = await SyncAsync();

        Assert.That(hevy.SyncSince[1], Is.EqualTo(new DateTime(2026, 11, 22, 9, 50, 0, DateTimeKind.Utc)));
        Assert.That((result.FullSync, result.Deleted), Is.EqualTo((false, 1)));
        await using (var db = PostgresFixture.CreateDbContext())
        {
            var remaining = await db.HevyWorkouts.Include(x => x.Exercises).SingleAsync();
            Assert.That(remaining.Id, Is.EqualTo("w1"));
            Assert.That(remaining.Exercises, Has.Count.EqualTo(1), "a re-synced workout replaces its exercises");
            Assert.That(await db.RawHealthRecords.AnyAsync(x => x.Endpoint == "workout" && x.Key == "w2"), Is.False);
        }
    }

    [Test]
    public async Task AnExpiredAccessTokenIsRefreshedOnceAndTheNewTokensAreKept()
    {
        await ConnectAsync(adminNanoid);
        time.Advance(TimeSpan.FromHours(2));
        hevy.Now = time.GetUtcNow().UtcDateTime;

        await SyncAsync();
        await SyncAsync();

        Assert.That(hevy.RefreshTokensUsed, Is.EqualTo(new[] { "refresh-1" }));
        Assert.That(hevy.AccessTokensUsed, Is.EqualTo(new[] { "access-2", "access-2" }));
        await using var db = PostgresFixture.CreateDbContext();
        Assert.That(protector.Unprotect((await db.UserIntegrations.SingleAsync()).ProtectedRefreshToken ?? ""), Is.EqualTo("refresh-2"));
    }

    [Test]
    public async Task WebhooksNeedAValidSignatureAndStoreTheWorkout()
    {
        await ConnectAsync(adminNanoid);

        Assert.That(await WebhookAsync("w7", "sha256=forged"), Is.False);
        Assert.That(await WebhookAsync("w7", FakeHevyAdapter.ValidSignature), Is.True);

        await using var db = PostgresFixture.CreateDbContext();
        Assert.That(await db.HevyWorkouts.Select(x => x.Id).ToListAsync(), Is.EqualTo(new[] { "w7" }));
    }

    [Test]
    public async Task ScheduledSyncsOnlyPickUsersWhoseLastSyncIsOverAnHourOld()
    {
        await ConnectAsync(adminNanoid);
        await SyncAsync();

        Assert.That(await RunSyncAsync(service => service.SyncNextDueAsync(CancellationToken.None)), Is.False);
        time.Advance(TimeSpan.FromHours(1.5));
        Assert.That(await RunSyncAsync(service => service.SyncNextDueAsync(CancellationToken.None)), Is.True);
        Assert.That(hevy.SyncSince, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task DisconnectingHevyDeletesItsDataButKeepsOtherSources()
    {
        await ConnectAsync(adminNanoid);
        await SyncAsync();
        await using (var db = PostgresFixture.CreateDbContext())
        {
            var adminId = await db.Users.Where(x => x.Nanoid == adminNanoid).Select(x => x.Id).SingleAsync();
            db.GarminWeights.Add(new egeszsegkalauz.data.Models.Garmin.Weight { UserId = adminId, TimestampGMT = 1, WeightGrams = 70000 });
            await db.SaveChangesAsync();
        }

        await using (var db = PostgresFixture.CreateDbContext())
        {
            await new IntegrationService(db, Adapters(), protector, TestStores.Create(db, time))
                .DisconnectAsync(adminNanoid, HealthDataSource.Hevy, CancellationToken.None);
        }

        await using (var db = PostgresFixture.CreateDbContext())
        {
            Assert.That(await db.HevyWorkouts.AnyAsync(), Is.False);
            Assert.That(await db.HevyWorkoutSets.AnyAsync(), Is.False);
            Assert.That(await db.HevyExerciseTemplates.AnyAsync(), Is.False);
            Assert.That(await db.HevyBodyMeasurements.AnyAsync(), Is.False);
            Assert.That(await db.RawHealthRecords.AnyAsync(x => x.Source == HealthDataSource.Hevy), Is.False);
            Assert.That(await db.UserIntegrations.AnyAsync(), Is.False);
            Assert.That(await db.GarminWeights.CountAsync(), Is.EqualTo(1));
        }
    }

    private HealthDataAdapters Adapters() => new(new FakeGarminAdapter(), hevy);

    private async Task<IntegrationDto> ConnectAsync(string userNanoid)
    {
        var start = await RunOAuthAsync(service => Task.FromResult(service.Start(userNanoid, HealthDataSource.Hevy)));
        var state = Uri.UnescapeDataString(start.AuthorizationUrl.Split("state=")[1]);
        return await RunOAuthAsync(service => service.CompleteAsync(userNanoid, HealthDataSource.Hevy,
            new CompleteOAuthRequest { Code = FakeHevyAdapter.ValidCode, State = state }, CancellationToken.None));
    }

    private async Task<SyncResultDto> SyncAsync()
    {
        return await RunSyncAsync(service => service.SyncAsync(adminNanoid, HealthDataSource.Hevy, CancellationToken.None));
    }

    private async Task<bool> WebhookAsync(string workoutId, string signature)
    {
        return await RunSyncAsync(service => service.HandleWebhookAsync(HealthDataSource.Hevy, workoutId,
            name => name == "Hevy-Signature" ? signature : null, CancellationToken.None));
    }

    private async Task<T> RunOAuthAsync<T>(Func<OAuthService, Task<T>> action)
    {
        await using var db = PostgresFixture.CreateDbContext();
        var integrations = new IntegrationService(db, Adapters(), protector, TestStores.Create(db, time));
        return await action(new OAuthService(db, Adapters(), protector, integrations, dataProtection));
    }

    private async Task<T> RunSyncAsync<T>(Func<SourceSyncService, Task<T>> action)
    {
        await using var db = PostgresFixture.CreateDbContext();
        var adapters = Adapters();
        var service = new SourceSyncService(db, adapters, new AccessTokenProvider(db, adapters, protector, time),
            TestStores.Create(db, time), time);
        return await action(service);
    }
}
