using egeszsegkalauz.data;
using egeszsegkalauz.data.Dev;
using egeszsegkalauz.services.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace egeszsegkalauz.tests.Services;

public class AuthServiceTests
{
    private const string Email = "teszt.elek@egeszsegkalauz.local";

    private static readonly AuthOptions AuthOptions = new() { SigningKey = new string('k', 32) };

    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

    [SetUp]
    public async Task SeedAsync()
    {
        await PostgresFixture.ResetAsync();
        await using var db = PostgresFixture.CreateDbContext();
        await DevDataSeeder.SeedAsync(db);
    }

    [Test]
    public async Task LoginIssuesTokensAndRecordsTheLogin()
    {
        var result = await LoginAsync();

        Assert.That(result.User.Email, Is.EqualTo(Email));
        Assert.That(result.Tokens.Refresh, Is.Not.Null);

        await using var db = PostgresFixture.CreateDbContext();
        var user = await db.Users.SingleAsync(x => x.Email == Email);
        Assert.That(user.LastLoginAt, Is.EqualTo(time.GetUtcNow().UtcDateTime).Within(TimeSpan.FromMilliseconds(1)));
    }

    [Test]
    public async Task LoginIgnoresEmailCase()
    {
        Assert.That(await TryLoginAsync(Email.ToUpperInvariant(), DevDataSeeder.DevPassword), Is.Not.Null);
    }

    [TestCase(Email, "wrong-password")]
    [TestCase("nobody@egeszsegkalauz.local", DevDataSeeder.DevPassword)]
    public async Task LoginRejectsBadCredentials(string email, string password)
    {
        Assert.That(await TryLoginAsync(email, password), Is.Null);
    }

    [Test]
    public async Task AccessTokenIdentifiesTheUserByNanoid()
    {
        var result = await LoginAsync();

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(result.Tokens.Access.Value,
            new TokenValidationParameters
            {
                ValidIssuer = AuthOptions.Issuer,
                ValidAudience = AuthOptions.Audience,
                IssuerSigningKey = AuthOptions.CreateSigningKey(),
                LifetimeValidator = (_, expires, _, _) => expires > time.GetUtcNow().UtcDateTime,
            });

        Assert.That(validation.IsValid, Is.True, validation.Exception?.Message);
        Assert.That(validation.Claims[JwtRegisteredClaimNames.Sub], Is.EqualTo(result.User.Nanoid));
    }

    [Test]
    public async Task RefreshRotatesTheRefreshToken()
    {
        var login = RefreshTokenOf(await LoginAsync());

        var rotated = RefreshTokenOf(await RefreshAsync(login));

        Assert.That(rotated, Is.Not.EqualTo(login));
        Assert.That(await RefreshAsync(rotated), Is.Not.Null);
    }

    [Test]
    public async Task ARotatedTokenWithinTheGracePeriodGetsAnAccessTokenOnly()
    {
        var login = RefreshTokenOf(await LoginAsync());
        await RefreshAsync(login);

        time.Advance(AuthOptions.RotationGracePeriod / 2);
        var concurrent = await RefreshAsync(login);

        Assert.That(concurrent, Is.Not.Null);
        Assert.That(concurrent?.Refresh, Is.Null);
    }

    [Test]
    public async Task ReusingARotatedTokenEndsEverySessionOfTheUser()
    {
        var login = RefreshTokenOf(await LoginAsync());
        var rotated = RefreshTokenOf(await RefreshAsync(login));

        time.Advance(AuthOptions.RotationGracePeriod + TimeSpan.FromSeconds(1));

        Assert.That(await RefreshAsync(login), Is.Null);
        Assert.That(await RefreshAsync(rotated), Is.Null);
    }

    [Test]
    public async Task ExpiredRefreshTokensAreRejected()
    {
        var login = RefreshTokenOf(await LoginAsync());

        time.Advance(AuthOptions.RefreshTokenLifetime);

        Assert.That(await RefreshAsync(login), Is.Null);
    }

    [Test]
    public async Task LogoutRevokesTheRefreshToken()
    {
        var login = RefreshTokenOf(await LoginAsync());

        await using (var db = PostgresFixture.CreateDbContext())
            await CreateService(db).LogoutAsync(login, CancellationToken.None);

        Assert.That(await RefreshAsync(login), Is.Null);
    }

    [Test]
    public async Task UnknownRefreshTokensAreRejected()
    {
        Assert.That(await RefreshAsync("not-a-token"), Is.Null);
    }

    private async Task<LoginResult> LoginAsync() =>
        await TryLoginAsync(Email, DevDataSeeder.DevPassword) ?? throw new AssertionException("Login failed.");

    private async Task<LoginResult?> TryLoginAsync(string email, string password)
    {
        await using var db = PostgresFixture.CreateDbContext();
        return await CreateService(db).LoginAsync(email, password, CancellationToken.None);
    }

    private async Task<AuthTokens?> RefreshAsync(string refreshToken)
    {
        await using var db = PostgresFixture.CreateDbContext();
        return await CreateService(db).RefreshAsync(refreshToken, CancellationToken.None);
    }

    private static string RefreshTokenOf(LoginResult login) => RefreshTokenOf(login.Tokens);

    private static string RefreshTokenOf(AuthTokens? tokens) =>
        tokens?.Refresh?.Value ?? throw new AssertionException("No refresh token was issued.");

    private AuthService CreateService(AppDbContext db) => new(db, Options.Create(AuthOptions), time);
}
