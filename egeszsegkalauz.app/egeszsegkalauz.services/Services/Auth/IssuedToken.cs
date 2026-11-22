namespace egeszsegkalauz.services.Services.Auth;

public class IssuedToken
{
    public required string Value { get; set; }
    public required DateTime ExpiresAt { get; set; }
}
