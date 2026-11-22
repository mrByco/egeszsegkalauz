using egeszsegkalauz.services.DTO;

namespace egeszsegkalauz.services.Services.Users;

public interface IUserService
{
    Task<List<UserDto>> ListAsync(CancellationToken cancellationToken);
    Task<UserDto> SetRolesAsync(string userNanoid, SetUserRolesRequest request, CancellationToken cancellationToken);
    Task<bool> GrantAdminIfNoneAsync(string email, CancellationToken cancellationToken);
}
