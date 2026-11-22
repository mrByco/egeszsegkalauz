using egeszsegkalauz.services.Services.Users;
using Microsoft.Extensions.DependencyInjection;

namespace egeszsegkalauz.services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
