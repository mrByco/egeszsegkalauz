using egeszsegkalauz.services.DTO;

namespace egeszsegkalauz.services.Services.Roles;

public interface IRoleService
{
    Task<List<RoleDto>> ListAsync(CancellationToken cancellationToken);
    Task<RoleDto> GetAsync(string nanoid, CancellationToken cancellationToken);
    Task<RoleDto> CreateAsync(SaveRoleRequest request, CancellationToken cancellationToken);
    Task<RoleDto> UpdateAsync(string nanoid, SaveRoleRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string nanoid, CancellationToken cancellationToken);
}
