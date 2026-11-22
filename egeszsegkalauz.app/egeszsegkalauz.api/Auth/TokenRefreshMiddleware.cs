using System.Security.Claims;
using egeszsegkalauz.services.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace egeszsegkalauz.api.Auth;

public class TokenRefreshMiddleware
{
    private static readonly PathString[] PathsThatManageTheirOwnTokens = ["/api/auth/login", "/api/auth/logout"];

    private readonly RequestDelegate next;

    public TokenRefreshMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService, IOptionsMonitor<JwtBearerOptions> jwtOptions)
    {
        var refreshToken = AuthCookies.ReadRefreshToken(context.Request);
        if (refreshToken is not null && context.User.Identity?.IsAuthenticated != true && ShouldRefresh(context.Request.Path))
        {
            var tokens = await authService.RefreshAsync(refreshToken, context.RequestAborted);
            if (tokens is null)
            {
                AuthCookies.Delete(context.Response);
            }
            else
            {
                AuthCookies.Append(context.Response, tokens);
                context.User = await ValidateAsync(tokens.Access.Value, jwtOptions);
            }
        }

        await next(context);
    }

    private static bool ShouldRefresh(PathString path) =>
        path.StartsWithSegments("/api") && !PathsThatManageTheirOwnTokens.Any(p => path.StartsWithSegments(p));

    private static async Task<ClaimsPrincipal> ValidateAsync(string accessToken, IOptionsMonitor<JwtBearerOptions> jwtOptions)
    {
        var parameters = jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(accessToken, parameters);
        return new ClaimsPrincipal(result.ClaimsIdentity);
    }
}
