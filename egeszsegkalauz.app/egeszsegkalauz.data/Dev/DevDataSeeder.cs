using egeszsegkalauz.data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.data.Dev;

public static class DevDataSeeder
{
    public const string DevPassword = "Jelszo123!";
    public const string AdminEmail = "admin@egeszsegkalauz.local";
    public const string ViewerEmail = "teszt.elek@egeszsegkalauz.local";

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
            return;

        var admin = CreateUser(AdminEmail);
        admin.Roles.Add(await db.Roles.SingleAsync(x => x.IsSystem, cancellationToken));

        var viewer = new Role { Name = "Megtekintő", Permissions = [Permission.UsersView, Permission.RolesView] };
        var tester = CreateUser(ViewerEmail);
        tester.Roles.Add(viewer);

        db.Users.AddRange(admin, tester);

        await db.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"[dev-db] Seeded dev users (password: {DevPassword}).");
    }

    private static User CreateUser(string email)
    {
        var user = new User { Email = email, PasswordHash = "" };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, DevPassword);
        return user;
    }
}
