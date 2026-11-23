using egeszsegkalauz.data;
using egeszsegkalauz.data.Models;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.Integrations;

public class AccessTokenProvider
{
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(2);

    private readonly AppDbContext db;
    private readonly HealthDataAdapters adapters;
    private readonly IApiKeyProtector protector;
    private readonly TimeProvider timeProvider;

    public AccessTokenProvider(AppDbContext db, HealthDataAdapters adapters, IApiKeyProtector protector, TimeProvider timeProvider)
    {
        this.db = db;
        this.adapters = adapters;
        this.protector = protector;
        this.timeProvider = timeProvider;
    }

    public async Task<string> GetAccessTokenAsync(UserIntegration integration, CancellationToken cancellationToken)
    {
        if (IsFresh(integration))
            return protector.Unprotect(integration.ProtectedApiKey);

        // Each refresh token works once: lock the row so a parallel sync or webhook waits for this refresh
        // and then uses its result instead of burning the already rotated token.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var current = await db.UserIntegrations
            .FromSql($"""SELECT * FROM "UserIntegrations" WHERE "Id" = {integration.Id} FOR UPDATE""")
            .AsNoTracking()
            .SingleAsync(cancellationToken);
        if (IsFresh(current))
        {
            integration.ProtectedApiKey = current.ProtectedApiKey;
            integration.ProtectedRefreshToken = current.ProtectedRefreshToken;
            integration.AccessTokenExpiresAt = current.AccessTokenExpiresAt;
            await transaction.CommitAsync(cancellationToken);
            return protector.Unprotect(current.ProtectedApiKey);
        }

        if (current.ProtectedRefreshToken is null)
            throw new InvalidOperationException($"{integration.Source} integration {integration.Id} has no refresh token.");

        var tokens = await adapters.OAuthFor(integration.Source)
            .RefreshAsync(protector.Unprotect(current.ProtectedRefreshToken), cancellationToken);
        integration.ProtectedApiKey = protector.Protect(tokens.AccessToken);
        integration.ProtectedRefreshToken = protector.Protect(tokens.RefreshToken);
        integration.AccessTokenExpiresAt = tokens.ExpiresAtUtc;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return tokens.AccessToken;
    }

    private bool IsFresh(UserIntegration integration) =>
        integration.AccessTokenExpiresAt is { } expiresAt && expiresAt - RefreshMargin > timeProvider.GetUtcNow().UtcDateTime;
}
