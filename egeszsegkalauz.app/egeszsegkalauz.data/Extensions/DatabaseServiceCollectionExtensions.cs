using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace egeszsegkalauz.data.Extensions;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddAppDbContext(
        this IServiceCollection services, string connectionString, bool development)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql => npgsql.CommandTimeout(120));
            if (development)
                options.EnableSensitiveDataLogging();
        });
        return services;
    }
}
