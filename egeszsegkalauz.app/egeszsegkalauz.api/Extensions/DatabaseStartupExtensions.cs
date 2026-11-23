using egeszsegkalauz.data;
using egeszsegkalauz.data.Dev;
using egeszsegkalauz.data.Extensions;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.Services.Integrations;
using egeszsegkalauz.services.Services.Users;
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
        {
            await DevDataSeeder.SeedAsync(db);
            await SeedDevGarminKeyAsync(app, scope.ServiceProvider);
        }

        var adminEmail = app.Configuration["Bootstrap:AdminEmail"];
        if (!string.IsNullOrWhiteSpace(adminEmail)
            && await scope.ServiceProvider.GetRequiredService<IUserService>().GrantAdminIfNoneAsync(adminEmail, CancellationToken.None))
        {
            app.Logger.LogInformation("Granted the Admin role to {Email}, as no user had it.", adminEmail);
        }
    }

    private static async Task SeedDevGarminKeyAsync(WebApplication app, IServiceProvider services)
    {
        var apiKey = app.Configuration["DevData:GarminApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return;

        var integrationService = services.GetRequiredService<IIntegrationService>();
        foreach (var email in new[] { DevDataSeeder.AdminEmail, DevDataSeeder.ViewerEmail })
            await integrationService.SetApiKeyWithoutValidationAsync(email, HealthDataSource.Garmin, apiKey, CancellationToken.None);
        app.Logger.LogInformation("Connected the dev users to Garmin with DevData:GarminApiKey.");
    }
}
