using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using egeszsegkalauz.data;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;
using Npgsql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace egeszsegkalauz.services.Services.Auth;

public class AuthService : IAuthService
{
    private static readonly PasswordHasher<User> Hasher = new();
    private static readonly User UnknownUser = new() { Email = "", PasswordHash = "" };
    private static readonly string UnknownUserHash = Hasher.HashPassword(UnknownUser, Guid.NewGuid().ToString());

    private readonly AppDbContext db;
    private readonly AuthOptions authOptions;
    private readonly TimeProvider timeProvider;

    public AuthService(AppDbContext db, IOptions<AuthOptions> authOptions, TimeProvider timeProvider)
    {
        this.db = db;
        this.authOptions = authOptions.Value;
        this.timeProvider = timeProvider;
    }

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<LoginResult?> RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        email = email.Trim();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return null;

        var user = new User { Email = email, PasswordHash = "" };
        user.PasswordHash = Hasher.HashPassword(user, password);
        db.Users.Add(user);

        try
        {
            return await StartSessionAsync(user, cancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return null;
        }
    }

    public async Task<LoginResult?> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null)
        {
            // Hash anyway, so an unknown email is as slow as a wrong password and can't be told apart.
            Hasher.VerifyHashedPassword(UnknownUser, UnknownUserHash, password);
            return null;
        }

        var verification = Hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
            return null;
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = Hasher.HashPassword(user, password);

        return await StartSessionAsync(user, cancellationToken);
    }

    public async Task<AuthTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var stored = await db.RefreshTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == Hash(refreshToken), cancellationToken);
        if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt <= Now)
            return null;

        if (stored.RotatedAt is null)
        {
            if (await TryClaimRotationAsync(stored, cancellationToken))
            {
                var next = AddRefreshToken(stored.User);
                await db.SaveChangesAsync(cancellationToken);
                return new AuthTokens { Access = CreateAccessToken(stored.User), Refresh = next };
            }

            await db.Entry(stored).ReloadAsync(cancellationToken);
        }

        // Parallel requests from one browser all present the same cookie and race to rotate it. The losers
        // get an access token only, so the browser keeps the refresh token the winner set.
        if (Now - stored.RotatedAt <= authOptions.RotationGracePeriod)
            return new AuthTokens { Access = CreateAccessToken(stored.User) };

        // A rotated token showing up again means two parties hold it: end every session of that user.
        await RevokeAllAsync(stored.UserId, cancellationToken);
        return null;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var now = Now;
        await db.RefreshTokens
            .Where(x => x.TokenHash == Hash(refreshToken) && x.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now).SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(string userNanoid, CancellationToken cancellationToken)
    {
        return await db.Users
            .Where(x => x.Nanoid == userNanoid)
            .Select(x => new CurrentUserDto { Nanoid = x.Nanoid, Email = x.Email })
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<LoginResult> StartSessionAsync(User user, CancellationToken cancellationToken)
    {
        user.LastLoginAt = Now;
        var refreshToken = AddRefreshToken(user);
        await db.SaveChangesAsync(cancellationToken);

        return new LoginResult
        {
            Tokens = new AuthTokens { Access = CreateAccessToken(user), Refresh = refreshToken },
            User = ToCurrentUserDto(user),
        };
    }

    private async Task<bool> TryClaimRotationAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        var now = Now;
        var claimed = await db.RefreshTokens
            .Where(x => x.Id == token.Id && x.RotatedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RotatedAt, now).SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
        return claimed == 1;
    }

    private async Task RevokeAllAsync(int userId, CancellationToken cancellationToken)
    {
        var now = Now;
        await db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now).SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
    }

    private IssuedToken CreateAccessToken(User user)
    {
        var now = Now;
        var expiresAt = now + authOptions.AccessTokenLifetime;
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = authOptions.Issuer,
            Audience = authOptions.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Nanoid,
                [JwtRegisteredClaimNames.Email] = user.Email,
            },
            SigningCredentials = new SigningCredentials(authOptions.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        });
        return new IssuedToken { Value = token, ExpiresAt = expiresAt };
    }

    private IssuedToken AddRefreshToken(User user)
    {
        var value = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = Now + authOptions.RefreshTokenLifetime;
        db.RefreshTokens.Add(new RefreshToken { User = user, TokenHash = Hash(value), ExpiresAt = expiresAt });
        return new IssuedToken { Value = value, ExpiresAt = expiresAt };
    }

    private static string Hash(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    private static CurrentUserDto ToCurrentUserDto(User user) => new() { Nanoid = user.Nanoid, Email = user.Email };
}
