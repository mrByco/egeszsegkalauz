namespace egeszsegkalauz.integrations.Common;

public class OAuthTokens
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
    public string? ExternalUserId { get; init; }
    public string? AccountName { get; init; }
}
