using egeszsegkalauz.data.Dev;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Exceptions;
using egeszsegkalauz.services.Services.Permissions;
using egeszsegkalauz.services.Services.Users;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.tests.Services;

public class UserRoleTests
{
    private const string AdminEmail = "admin@egeszsegkalauz.local";
    private const string TesterEmail = "teszt.elek@egeszsegkalauz.local";

    [SetUp]
    public async Task SeedAsync()
    {
        await PostgresFixture.ResetAsync();
        await using var db = PostgresFixture.CreateDbContext();
        await DevDataSeeder.SeedAsync(db);
    }

    [Test]
    public async Task PermissionsAreTheUnionOfTheUsersRoles()
    {
        var editor = await AddRoleAsync("Szerkesztő", Permission.RolesView, Permission.RolesEdit);
        var tester = await FindUserAsync(TesterEmail);
        await SetRolesAsync(tester.Nanoid, tester.Roles.Select(x => x.Nanoid).Append(editor).ToArray());

        Assert.That(await PermissionsOfAsync(TesterEmail),
            Is.EquivalentTo(new[] { Permission.UsersView, Permission.RolesView, Permission.RolesEdit }));
    }

    [Test]
    public async Task AdminsHaveEveryPermission()
    {
        Assert.That(await PermissionsOfAsync(AdminEmail), Is.EquivalentTo(Enum.GetValues<Permission>()));
    }

    [Test]
    public async Task UsersWithoutRolesHaveNoPermissions()
    {
        var tester = await FindUserAsync(TesterEmail);

        var updated = await SetRolesAsync(tester.Nanoid);

        Assert.That(updated.Roles, Is.Empty);
        Assert.That(await PermissionsOfAsync(TesterEmail), Is.Empty);
    }

    [Test]
    public async Task AssigningAnUnknownRoleFails()
    {
        var tester = await FindUserAsync(TesterEmail);

        await Assert.ThatAsync(() => SetRolesAsync(tester.Nanoid, "nincs-ilyen"), Throws.InstanceOf<NotFoundException>());
    }

    [Test]
    public async Task TheLastAdminKeepsTheAdminRole()
    {
        var admin = await FindUserAsync(AdminEmail);

        await Assert.ThatAsync(() => SetRolesAsync(admin.Nanoid), Throws.InstanceOf<ConflictException>());
        Assert.That((await FindUserAsync(AdminEmail)).Roles.Select(x => x.Name), Does.Contain(Role.AdminName));
    }

    [Test]
    public async Task AnAdminCanStepDownOnceAnotherAdminExists()
    {
        var admin = await FindUserAsync(AdminEmail);
        var tester = await FindUserAsync(TesterEmail);
        await SetRolesAsync(tester.Nanoid, admin.Roles.Single().Nanoid);

        var updated = await SetRolesAsync(admin.Nanoid);

        Assert.That(updated.Roles, Is.Empty);
    }

    [Test]
    public async Task BootstrapGrantsAdminOnlyWhileNobodyHasIt()
    {
        Assert.That(await GrantAdminIfNoneAsync(TesterEmail), Is.False);

        var admin = await FindUserAsync(AdminEmail);
        var tester = await FindUserAsync(TesterEmail);
        await SetRolesAsync(tester.Nanoid, admin.Roles.Single().Nanoid);
        await using (var db = PostgresFixture.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"UserRoles\"");

        Assert.That(await GrantAdminIfNoneAsync("nobody@egeszsegkalauz.local"), Is.False);
        Assert.That(await GrantAdminIfNoneAsync(TesterEmail), Is.True);
        Assert.That(await PermissionsOfAsync(TesterEmail), Is.EquivalentTo(Enum.GetValues<Permission>()));
    }

    private static async Task<string> AddRoleAsync(string name, params Permission[] permissions)
    {
        await using var db = PostgresFixture.CreateDbContext();
        var role = new Role { Name = name, Permissions = permissions.ToList() };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role.Nanoid;
    }

    private static async Task<UserDto> FindUserAsync(string email)
    {
        await using var db = PostgresFixture.CreateDbContext();
        return (await new UserService(db).ListAsync(CancellationToken.None)).Single(x => x.Email == email);
    }

    private static async Task<UserDto> SetRolesAsync(string userNanoid, params string[] roleNanoids)
    {
        await using var db = PostgresFixture.CreateDbContext();
        return await new UserService(db).SetRolesAsync(userNanoid,
            new SetUserRolesRequest { RoleNanoids = roleNanoids.ToList() }, CancellationToken.None);
    }

    private static async Task<bool> GrantAdminIfNoneAsync(string email)
    {
        await using var db = PostgresFixture.CreateDbContext();
        return await new UserService(db).GrantAdminIfNoneAsync(email, CancellationToken.None);
    }

    private static async Task<IReadOnlySet<Permission>> PermissionsOfAsync(string email)
    {
        var user = await FindUserAsync(email);
        await using var db = PostgresFixture.CreateDbContext();
        return await new PermissionService(db).GetPermissionsAsync(user.Nanoid, CancellationToken.None);
    }
}
