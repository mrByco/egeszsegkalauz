using egeszsegkalauz.data.Models;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.data.Dev;

public static class DevDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
            return;

        db.Users.AddRange(
            new User { Email = "admin@egeszsegkalauz.local", DisplayName = "Admin" },
            new User { Email = "teszt.elek@egeszsegkalauz.local", DisplayName = "Teszt Elek" });

        await db.SaveChangesAsync(cancellationToken);
        Console.WriteLine("[dev-db] Seeded dev data.");
    }
}
