using egeszsegkalauz.services.DTO;

namespace egeszsegkalauz.services.Services.Auth;

public class LoginResult
{
    public required AuthTokens Tokens { get; set; }
    public required CurrentUserDto User { get; set; }
}
