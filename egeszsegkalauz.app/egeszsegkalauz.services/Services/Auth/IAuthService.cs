using egeszsegkalauz.services.DTO;

namespace egeszsegkalauz.services.Services.Auth;

public interface IAuthService
{
    Task<LoginResult?> LoginAsync(string email, string password, CancellationToken cancellationToken);
    Task<AuthTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    Task<CurrentUserDto?> GetCurrentUserAsync(string userNanoid, CancellationToken cancellationToken);
}
