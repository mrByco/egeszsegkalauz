using egeszsegkalauz.data.Models;

namespace egeszsegkalauz.services.Services.Permissions;

public interface IPermissionService
{
    Task<IReadOnlySet<Permission>> GetPermissionsAsync(string userNanoid, CancellationToken cancellationToken);
}
