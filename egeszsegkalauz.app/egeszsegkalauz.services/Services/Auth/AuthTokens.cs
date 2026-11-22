namespace egeszsegkalauz.services.Services.Auth;

public class AuthTokens
{
    public required IssuedToken Access { get; set; }
    public IssuedToken? Refresh { get; set; }
}
