using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace egeszsegkalauz.api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserNanoid(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("The principal has no subject claim; is the endpoint [Authorize]d?");
    }
}
