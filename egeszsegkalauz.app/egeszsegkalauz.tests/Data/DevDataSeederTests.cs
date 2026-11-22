using egeszsegkalauz.data.Dev;
using egeszsegkalauz.data.Models;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.tests.Data;

public class DevDataSeederTests
{
    [SetUp]
    public Task ResetAsync() => PostgresFixture.ResetAsync();

    [Test]
    public async Task SeedsAnEmptyDatabase()
    {
        await using var db = PostgresFixture.CreateDbContext();

        await DevDataSeeder.SeedAsync(db);

        Assert.That(await db.Users.CountAsync(), Is.GreaterThan(0));
    }

    [Test]
    public async Task LeavesExistingDataAlone()
    {
        await using (var db = PostgresFixture.CreateDbContext())
        {
            db.Users.Add(new User { Email = "from-dump@example.com", DisplayName = "Dump" });
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
