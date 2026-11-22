using egeszsegkalauz.data.Dev;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Exceptions;
using egeszsegkalauz.services.Services.Roles;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.tests.Services;

public class RoleServiceTests
{
    [SetUp]
    public async Task SeedAsync()
    {
        await PostgresFixture.ResetAsync();
        await using var db = PostgresFixture.CreateDbContext();
        await DevDataSeeder.SeedAsync(db);
    }

    [Test]
    public async Task ListsTheAdminRoleFirstWithEveryPermission()
    {
        var roles = await RunAsync(service => service.ListAsync(CancellationToken.None));

        Assert.That(roles[0].Name, Is.EqualTo(Role.AdminName));
        Assert.That(roles[0].IsSystem, Is.True);
        Assert.That(roles[0].Permissions, Is.EquivalentTo(Enum.GetValues<Permission>()));
        Assert.That(roles[0].UserCount, Is.EqualTo(1));
    }

    [Test]
    public async Task CreateStoresTrimmedNameAndDistinctPermissions()
    {
        var created = await RunAsync(service => service.CreateAsync(Request("  Szerkesztő ",
            Permission.RolesEdit, Permission.RolesView, Permission.RolesEdit), CancellationToken.None));

        var loaded = await RunAsync(service => service.GetAsync(created.Nanoid, CancellationToken.None));
        Assert.That(loaded.Name, Is.EqualTo("Szerkesztő"));
        Assert.That(loaded.Permissions, Is.EqualTo(new[] { Permission.RolesView, Permission.RolesEdit }));
    }

    [Test]
    public async Task RoleNamesAreUniqueIgnoringCase()
    {
        await Assert.ThatAsync(() => RunAsync(service => service.CreateAsync(Request("MEGTEKINTŐ"), CancellationToken.None)),
            Throws.InstanceOf<ConflictException>());
    }

    [Test]
    public async Task UpdateReplacesNameAndPermissions()
    {
        var viewer = await FindAsync("Megtekintő");

        var updated = await RunAsync(service =>
            service.UpdateAsync(viewer.Nanoid, Request("Olvasó", Permission.UsersView), CancellationToken.None));

        Assert.That(updated.Name, Is.EqualTo("Olvasó"));
        Assert.That(updated.Permissions, Is.EqualTo(new[] { Permission.UsersView }));
        Assert.That(updated.UserCount, Is.EqualTo(1));
    }

    [Test]
    public async Task TheAdminRoleCannotBeChangedOrDeleted()
    {
        var admin = await FindAsync(Role.AdminName);

        await Assert.ThatAsync(() => RunAsync(service => service.UpdateAsync(admin.Nanoid, Request("Főnök"), CancellationToken.None)),
            Throws.InstanceOf<ConflictException>());
        await Assert.ThatAsync(() => RunAsync(async service =>
        {
            await service.DeleteAsync(admin.Nanoid, CancellationToken.None);
            return true;
        }), Throws.InstanceOf<ConflictException>());
    }

    [Test]
    public async Task DeleteRemovesTheRoleFromItsUsers()
    {
        var viewer = await FindAsync("Megtekintő");

        await RunAsync(async service =>
        {
            await service.DeleteAsync(viewer.Nanoid, CancellationToken.None);
            return true;
        });

        await using var db = PostgresFixture.CreateDbContext();
        Assert.That(await db.Roles.AnyAsync(x => x.Nanoid == viewer.Nanoid), Is.False);
        Assert.That(await db.Users.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task UnknownRolesAreNotFound()
    {
        await Assert.ThatAsync(() => RunAsync(service => service.GetAsync("nincs-ilyen", CancellationToken.None)),
            Throws.InstanceOf<NotFoundException>());
    }

    private static SaveRoleRequest Request(string name, params Permission[] permissions)
    {
        return new SaveRoleRequest { Name = name, Permissions = permissions.ToList() };
    }

    private static async Task<RoleDto> FindAsync(string name)
    {
        var roles = await RunAsync(service => service.ListAsync(CancellationToken.None));
        return roles.Single(x => x.Name == name);
    }

    private static async Task<T> RunAsync<T>(Func<RoleService, Task<T>> action)
    {
        await using var db = PostgresFixture.CreateDbContext();
        return await action(new RoleService(db));
    }
}
