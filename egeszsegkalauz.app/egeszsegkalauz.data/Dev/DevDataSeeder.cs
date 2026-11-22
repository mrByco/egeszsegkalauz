using egeszsegkalauz.data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.data.Dev;

public static class DevDataSeeder
{
    public const string DevPassword = "Jelszo123!";

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
            return;

        db.Users.AddRange(
            CreateUser("admin@egeszsegkalauz.local"),
            CreateUser("teszt.elek@egeszsegkalauz.local"));

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
