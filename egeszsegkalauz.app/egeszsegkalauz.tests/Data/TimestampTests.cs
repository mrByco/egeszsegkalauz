using egeszsegkalauz.data.Models;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.tests.Data;

public class TimestampTests
{
    [SetUp]
    public async Task ResetAsync()
    {
        await PostgresFixture.ResetAsync();
    }

    [Test]
    public async Task InsertStampsBothAndUpdateMovesOnlyUpdatedAt()
    {
        await using var db = PostgresFixture.CreateDbContext();
        var user = new User { Email = "a@example.com", PasswordHash = "hash" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        Assert.That(user.CreatedAt, Is.Not.EqualTo(default(DateTime)));
        Assert.That(user.UpdatedAt, Is.EqualTo(user.CreatedAt));
        var created = user.CreatedAt;

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        Assert.That(user.CreatedAt, Is.EqualTo(created));
        Assert.That(user.UpdatedAt, Is.GreaterThan(created));
    }

    [Test]
    public async Task NanoidIsGeneratedAndUnique()
    {
        await using var db = PostgresFixture.CreateDbContext();
        db.Users.AddRange(
            new User { Email = "a@example.com", PasswordHash = "hash" },
            new User { Email = "b@example.com", PasswordHash = "hash" });
        await db.SaveChangesAsync();

        var ids = await db.Users.Select(x => x.Nanoid).ToListAsync();
        Assert.That(ids, Is.Unique.And.All.Length.EqualTo(12));
    }
}
