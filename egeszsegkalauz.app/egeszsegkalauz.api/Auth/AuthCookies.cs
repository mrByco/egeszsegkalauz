using egeszsegkalauz.services.Services.Auth;

namespace egeszsegkalauz.api.Auth;

public static class AuthCookies
{
    public const string AccessToken = "access_token";
    public const string RefreshToken = "refresh_token";

    public static void Append(HttpResponse response, AuthTokens tokens)
    {
        response.Cookies.Append(AccessToken, tokens.Access.Value, Options(tokens.Access.ExpiresAt));
        if (tokens.Refresh is not null)
            response.Cookies.Append(RefreshToken, tokens.Refresh.Value, Options(tokens.Refresh.ExpiresAt));
    }

    public static void Delete(HttpResponse response)
    {
        response.Cookies.Delete(AccessToken, Options());
        response.Cookies.Delete(RefreshToken, Options());
    }

    public static string? ReadRefreshToken(HttpRequest request)
    {
        return request.Cookies[RefreshToken];
    }

    private static CookieOptions Options(DateTime? expiresAt = null) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api",
        Expires = expiresAt,
    };
}
