using egeszsegkalauz.data;
using egeszsegkalauz.data.Dev;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Exceptions;
using egeszsegkalauz.services.Services.HealthData;
using egeszsegkalauz.services.Services.Integrations;
using egeszsegkalauz.tests.Integrations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.tests.Services;

public class IntegrationServiceTests
{
    private readonly ApiKeyProtector protector = new(new EphemeralDataProtectionProvider());

    [SetUp]
    public async Task SeedAsync()
    {
        await PostgresFixture.ResetAsync();
        await using var db = PostgresFixture.CreateDbContext();
        await DevDataSeeder.SeedAsync(db);
    }

    [Test]
    public async Task ListsEverySourceAsDisconnectedAtFirst()
    {
        var integrations = await RunAsync(async (service, nanoid) => await service.ListAsync(nanoid, CancellationToken.None));

        Assert.That(integrations.Select(x => x.Source), Is.EqualTo(Enum.GetValues<HealthDataSource>()));
        Assert.That(integrations.Single(x => x.Source == HealthDataSource.Hevy).AuthKind, Is.EqualTo(IntegrationAuthKind.OAuth));
        Assert.That(integrations.All(x => !x.Connected), Is.True);
    }

    [Test]
    public async Task ConnectStoresTheKeyEncryptedWithAHint()
    {
        var connected = await RunAsync(async (service, nanoid) =>
            await service.ConnectAsync(nanoid, HealthDataSource.Garmin, $"  {FakeGarminAdapter.ValidKey} ", CancellationToken.None));

        Assert.That(connected.Connected, Is.True);
        Assert.That(connected.ApiKeyHint, Is.EqualTo("…" + FakeGarminAdapter.ValidKey[^4..]));

        await using var db = PostgresFixture.CreateDbContext();
        var stored = await db.UserIntegrations.SingleAsync();
        Assert.That(stored.ProtectedApiKey, Does.Not.Contain(FakeGarminAdapter.ValidKey));
        Assert.That(protector.Unprotect(stored.ProtectedApiKey), Is.EqualTo(FakeGarminAdapter.ValidKey));
    }

    [Test]
    public async Task ConnectRejectsAKeyTheSourceRefuses()
    {
        await Assert.ThatAsync(() => RunAsync(async (service, nanoid) =>
                await service.ConnectAsync(nanoid, HealthDataSource.Garmin, "wrong", CancellationToken.None)),
            Throws.InstanceOf<InvalidInputException>());
    }

    [Test]
    public async Task ReconnectingReplacesTheKeyAndDisconnectRemovesIt()
    {
        await RunAsync(async (service, nanoid) =>
            await service.ConnectAsync(nanoid, HealthDataSource.Garmin, FakeGarminAdapter.ValidKey, CancellationToken.None));
        await RunAsync(async (service, _) =>
        {
            await service.SetApiKeyWithoutValidationAsync(DevDataSeeder.AdminEmail, HealthDataSource.Garmin, "other-key-1234",
                CancellationToken.None);
            return true;
        });

        await using (var db = PostgresFixture.CreateDbContext())
            Assert.That((await db.UserIntegrations.SingleAsync()).ApiKeyHint, Is.EqualTo("…1234"));

        var after = await RunAsync(async (service, nanoid) =>
        {
            await service.DisconnectAsync(nanoid, HealthDataSource.Garmin, CancellationToken.None);
            return await service.ListAsync(nanoid, CancellationToken.None);
        });
        Assert.That(after.Single(x => x.Source == HealthDataSource.Garmin).Connected, Is.False);
    }

    private async Task<T> RunAsync<T>(Func<IntegrationService, string, Task<T>> action)
    {
        await using var db = PostgresFixture.CreateDbContext();
        var nanoid = await AdminNanoidAsync(db);
        var service = new IntegrationService(db, new HealthDataAdapters(new FakeGarminAdapter(), new FakeHevyAdapter()), protector, TestStores.Create(db, TimeProvider.System));
        return await action(service, nanoid);
    }

    private static async Task<string> AdminNanoidAsync(AppDbContext db)
    {
        return await db.Users.Where(x => x.Email == DevDataSeeder.AdminEmail).Select(x => x.Nanoid).SingleAsync();
    }
}
