using egeszsegkalauz.data.Dev;
using egeszsegkalauz.data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.tests.Data;

public class DevDataSeederTests
{
    [SetUp]
    public async Task ResetAsync()
    {
        await PostgresFixture.ResetAsync();
    }

    [Test]
    public async Task SeedsAnEmptyDatabase()
    {
        await using var db = PostgresFixture.CreateDbContext();

        await DevDataSeeder.SeedAsync(db);

        Assert.That(await db.Users.CountAsync(), Is.GreaterThan(0));
    }

    [Test]
    public async Task SeededUsersCanBeVerifiedWithTheDevPassword()
    {
        await using var db = PostgresFixture.CreateDbContext();
        await DevDataSeeder.SeedAsync(db);

        var hasher = new PasswordHasher<User>();
        foreach (var user in await db.Users.ToListAsync())
        {
            Assert.That(hasher.VerifyHashedPassword(user, user.PasswordHash, DevDataSeeder.DevPassword),
                Is.EqualTo(PasswordVerificationResult.Success), user.Email);
        }
    }

    [Test]
    public async Task LeavesExistingDataAlone()
    {
        await using (var db = PostgresFixture.CreateDbContext())
        {
            db.Users.Add(new User { Email = "from-dump@example.com", PasswordHash = "hash" });
            await db.SaveChangesAsync();
        }

        await using (var db = PostgresFixture.CreateDbContext())
        {
            await DevDataSeeder.SeedAsync(db);
            Assert.That(await db.Users.Select(x => x.Email).ToListAsync(),
                Is.EqualTo(new[] { "from-dump@example.com" }));
        }
    }
}
