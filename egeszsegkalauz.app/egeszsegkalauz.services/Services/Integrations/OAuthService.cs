using System.Security.Cryptography;
using egeszsegkalauz.data;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Exceptions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.Integrations;

public class OAuthService : IOAuthService
{
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    private readonly AppDbContext db;
    private readonly HealthDataAdapters adapters;
    private readonly IApiKeyProtector protector;
    private readonly IIntegrationService integrationService;
    private readonly ITimeLimitedDataProtector stateProtector;

    public OAuthService(AppDbContext db, HealthDataAdapters adapters, IApiKeyProtector protector,
        IIntegrationService integrationService, IDataProtectionProvider dataProtectionProvider)
    {
        this.db = db;
        this.adapters = adapters;
        this.protector = protector;
        this.integrationService = integrationService;
        stateProtector = dataProtectionProvider.CreateProtector("egeszsegkalauz.integrations.oauth-state").ToTimeLimitedDataProtector();
    }

    public OAuthStartDto Start(string userNanoid, HealthDataSource source)
    {
        var state = stateProtector.Protect(StatePayload(userNanoid, source), StateLifetime);
        return new OAuthStartDto { AuthorizationUrl = adapters.OAuthFor(source).BuildAuthorizationUrl(state) };
    }

    public async Task<IntegrationDto> CompleteAsync(string userNanoid, HealthDataSource source, CompleteOAuthRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsStateValid(request.State, userNanoid, source))
            throw new InvalidInputException("The authorization request expired or belongs to someone else.");

        var tokens = await adapters.OAuthFor(source).ExchangeCodeAsync(request.Code, cancellationToken);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Nanoid == userNanoid, cancellationToken)
                   ?? throw new NotFoundException($"User {userNanoid} does not exist.");
        if (tokens.ExternalUserId is { } externalUserId
            && await db.UserIntegrations.AnyAsync(
                x => x.Source == source && x.ExternalUserId == externalUserId && x.UserId != user.Id, cancellationToken))
            throw new ConflictException($"This {source} account is already connected to another user.");

        var integration = await db.UserIntegrations
                              .SingleOrDefaultAsync(x => x.UserId == user.Id && x.Source == source, cancellationToken)
                          ?? db.UserIntegrations.Add(new UserIntegration { User = user, Source = source, ProtectedApiKey = "" }).Entity;
        integration.ProtectedApiKey = protector.Protect(tokens.AccessToken);
        integration.ProtectedRefreshToken = protector.Protect(tokens.RefreshToken);
        integration.AccessTokenExpiresAt = tokens.ExpiresAtUtc;
        integration.ExternalUserId = tokens.ExternalUserId;
        integration.AccountName = tokens.AccountName;
        integration.ApiKeyHint = null;
        await db.SaveChangesAsync(cancellationToken);

        return (await integrationService.ListAsync(userNanoid, cancellationToken)).Single(x => x.Source == source);
    }

    private bool IsStateValid(string state, string userNanoid, HealthDataSource source)
    {
        try
        {
            return stateProtector.Unprotect(state) == StatePayload(userNanoid, source);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static string StatePayload(string userNanoid, HealthDataSource source) => $"{userNanoid}|{source}";
}
