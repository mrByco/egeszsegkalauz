using egeszsegkalauz.data.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace egeszsegkalauz.tests.Data;

public class UserEmailTests
{
    [SetUp]
    public async Task ResetAsync()
    {
        await PostgresFixture.ResetAsync();
    }

    [Test]
    public async Task EmailIsUniqueIgnoringCase()
    {
        await using (var db = PostgresFixture.CreateDbContext())
        {
            db.Users.Add(new User { Email = "Teszt@Example.com", PasswordHash = "hash" });
            await db.SaveChangesAsync();
        }

        await using (var db = PostgresFixture.CreateDbContext())
        {
            db.Users.Add(new User { Email = "teszt@example.com", PasswordHash = "hash" });
            await Assert.ThatAsync(() => db.SaveChangesAsync(), Throws.InstanceOf<DbUpdateException>()
                .With.InnerException.Matches<PostgresException>(e => e.SqlState == PostgresErrorCodes.UniqueViolation));
        }
    }

    [Test]
    public async Task LookupByEmailIgnoresCase()
    {
        await using (var db = PostgresFixture.CreateDbContext())
        {
            db.Users.Add(new User { Email = "Teszt@Example.com", PasswordHash = "hash" });
            await db.SaveChangesAsync();
        }

        await using (var db = PostgresFixture.CreateDbContext())
        {
            Assert.That(await db.Users.AnyAsync(x => x.Email == "teszt@example.com"), Is.True);
        }
    }
}
