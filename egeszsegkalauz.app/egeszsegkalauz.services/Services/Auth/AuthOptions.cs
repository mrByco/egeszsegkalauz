using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace egeszsegkalauz.services.Services.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "egeszsegkalauz";
    public string Audience { get; set; } = "egeszsegkalauz";

    [MinLength(32, ErrorMessage = "Auth:SigningKey must be at least 32 characters (HS256 needs a 256-bit key).")]
    public string SigningKey { get; set; } = "";

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
    public TimeSpan RotationGracePeriod { get; set; } = TimeSpan.FromSeconds(30);

    public SymmetricSecurityKey CreateSigningKey()
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
    }
}
