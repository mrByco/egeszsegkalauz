using egeszsegkalauz.data;
using egeszsegkalauz.data.Dev;
using egeszsegkalauz.data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.api.Extensions;

public static class DatabaseStartupExtensions
{
    public static async Task<DevDatabase?> AddAppDatabaseAsync(this WebApplicationBuilder builder)
    {
        DevDatabase? devDatabase = null;
        var connectionString = builder.Configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (!builder.Environment.IsDevelopment())
                throw new InvalidOperationException(
                    "ConnectionStrings:Default is not configured. Only Development may start without it.");

            var options = builder.Configuration.GetSection(DevDatabaseOptions.SectionName).Get<DevDatabaseOptions>()
                          ?? new DevDatabaseOptions();
            devDatabase = await DevDatabase.StartAsync(options, builder.Environment.ContentRootPath);
            connectionString = devDatabase.ConnectionString;
        }

        builder.Services.AddAppDbContext(connectionString, builder.Environment.IsDevelopment());
        return devDatabase;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app, bool seedDevData)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        if (seedDevData)
            await DevDataSeeder.SeedAsync(db);
    }
}
