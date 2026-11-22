using egeszsegkalauz.data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace egeszsegkalauz.tests;

[SetUpFixture]
public class PostgresFixture
{
    private static PostgreSqlContainer? container;

    public static string ConnectionString =>
        (container ?? throw new InvalidOperationException("The Postgres container has not started.")).GetConnectionString();

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await container.StartAsync();

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task StopAsync()
    {
        if (container is not null)
            await container.DisposeAsync();
    }

    public static AppDbContext CreateDbContext()
    {
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);
    }

    public static async Task ResetAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE \"Users\" RESTART IDENTITY CASCADE");
    }
}
