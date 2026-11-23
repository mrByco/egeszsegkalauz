using egeszsegkalauz.data;
using egeszsegkalauz.integrations;
using egeszsegkalauz.services.Services.Auth;
using egeszsegkalauz.services.Services.HealthData;
using egeszsegkalauz.services.Services.Imports;
using egeszsegkalauz.services.Services.Garmin;
using egeszsegkalauz.services.Services.Integrations;
using egeszsegkalauz.services.Services.Permissions;
using egeszsegkalauz.services.Services.Roles;
using egeszsegkalauz.services.Services.Users;
using egeszsegkalauz.services.Services.Integrations.Garmin;
using egeszsegkalauz.services.Services.Integrations.Hevy;
using egeszsegkalauz.services.Services.Sync;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace egeszsegkalauz.services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();

        services.AddDataProtection().SetApplicationName("egeszsegkalauz").PersistKeysToDbContext<AppDbContext>();
        services.AddIntegrations();
        services.AddSingleton<IApiKeyProtector, ApiKeyProtector>();
        services.AddScoped<HealthDataAdapters>();
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddScoped<IGarminDataService, GarminDataService>();
        services.AddScoped<GarminStore>();
        services.AddScoped<HevyStore>();
        services.AddScoped<SourceStores>();
        services.AddScoped<HealthDataStore>();
        services.AddSingleton<ImportPacer>();
        services.AddScoped<ISummaryImportService, SummaryImportService>();
        services.AddScoped<IDailyImportService, DailyImportService>();
        services.AddScoped<AccessTokenProvider>();
        services.AddScoped<IOAuthService, OAuthService>();
        services.AddScoped<ISourceSyncService, SourceSyncService>();
        return services;
    }
}
