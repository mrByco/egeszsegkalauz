namespace egeszsegkalauz.integrations.Common;

public interface IOAuthAdapter
{
    string BuildAuthorizationUrl(string state);
    Task<OAuthTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken);
    Task<OAuthTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
}
