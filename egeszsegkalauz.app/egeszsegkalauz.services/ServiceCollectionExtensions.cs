using egeszsegkalauz.services.Services.Auth;
using egeszsegkalauz.services.Services.Permissions;
using egeszsegkalauz.services.Services.Roles;
using egeszsegkalauz.services.Services.Users;
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
        return services;
    }
}
