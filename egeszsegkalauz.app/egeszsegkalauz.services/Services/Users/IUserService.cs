using egeszsegkalauz.services.DTO;

namespace egeszsegkalauz.services.Services.Users;

public interface IUserService
{
    Task<List<UserDto>> ListAsync(CancellationToken cancellationToken);
}
