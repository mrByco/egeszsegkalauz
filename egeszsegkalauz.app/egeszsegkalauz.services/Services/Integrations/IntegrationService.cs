using egeszsegkalauz.data;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Exceptions;
using egeszsegkalauz.services.Services.HealthData;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.Integrations;

public class IntegrationService : IIntegrationService
{
    private readonly AppDbContext db;
    private readonly HealthDataAdapters adapters;
    private readonly IApiKeyProtector apiKeyProtector;
    private readonly HealthDataStore store;

    public IntegrationService(AppDbContext db, HealthDataAdapters adapters, IApiKeyProtector apiKeyProtector,
        HealthDataStore store)
    {
        this.db = db;
        this.adapters = adapters;
        this.apiKeyProtector = apiKeyProtector;
        this.store = store;
    }

    public async Task<List<IntegrationDto>> ListAsync(string userNanoid, CancellationToken cancellationToken)
    {
        var connected = await db.UserIntegrations
            .AsNoTracking()
            .Where(x => x.User.Nanoid == userNanoid)
            .ToDictionaryAsync(x => x.Source, cancellationToken);

        return Enum.GetValues<HealthDataSource>()
            .Select(source => ToDto(source, connected.GetValueOrDefault(source)))
            .ToList();
    }

    public async Task<IntegrationDto> ConnectAsync(string userNanoid, HealthDataSource source, string apiKey,
        CancellationToken cancellationToken)
    {
        apiKey = apiKey.Trim();
        if (!await adapters.For(source).IsApiKeyValidAsync(apiKey, cancellationToken))
            throw new InvalidInputException($"{source} rejected the API key.");

        var user = await db.Users.SingleOrDefaultAsync(x => x.Nanoid == userNanoid, cancellationToken)
                   ?? throw new NotFoundException($"User {userNanoid} does not exist.");
        return ToDto(source, await StoreAsync(user, source, apiKey, cancellationToken));
    }

    public async Task DisconnectAsync(string userNanoid, HealthDataSource source, CancellationToken cancellationToken)
    {
        var integration = await db.UserIntegrations
            .SingleOrDefaultAsync(x => x.User.Nanoid == userNanoid && x.Source == source, cancellationToken);
        if (integration is null)
            return;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (HealthDataAdapters.Describe(source).DeletesDataOnDisconnect)
            await store.DeleteSourceDataAsync(integration.UserId, source, cancellationToken);
        db.UserIntegrations.Remove(integration);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SetApiKeyWithoutValidationAsync(string email, HealthDataSource source, string apiKey,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken)
                   ?? throw new NotFoundException($"User {email} does not exist.");
        await StoreAsync(user, source, apiKey.Trim(), cancellationToken);
    }

    private async Task<UserIntegration> StoreAsync(User user, HealthDataSource source, string apiKey,
        CancellationToken cancellationToken)
    {
        var integration = await db.UserIntegrations
            .SingleOrDefaultAsync(x => x.UserId == user.Id && x.Source == source, cancellationToken);
        if (integration is null)
        {
            integration = new UserIntegration { User = user, Source = source, ProtectedApiKey = "" };
            db.UserIntegrations.Add(integration);
        }

        integration.ProtectedApiKey = apiKeyProtector.Protect(apiKey);
        integration.ApiKeyHint = Hint(apiKey);
        await db.SaveChangesAsync(cancellationToken);
        return integration;
    }

    private static string Hint(string apiKey) => apiKey.Length <= 4 ? "…" : "…" + apiKey[^4..];

    private static IntegrationDto ToDto(HealthDataSource source, UserIntegration? integration)
    {
        var descriptor = HealthDataAdapters.Describe(source);
        return new IntegrationDto
        {
            Source = source,
            AuthKind = descriptor.AuthKind,
            Capabilities = descriptor.Capabilities,
            DeletesDataOnDisconnect = descriptor.DeletesDataOnDisconnect,
            Connected = integration is not null,
            ApiKeyHint = integration?.ApiKeyHint,
            AccountName = integration?.AccountName,
            FirstDataDate = integration?.FirstDataDate,
            SummariesImportedAt = integration?.SummariesImportedAt,
            LastSyncAt = integration?.LastSyncAt,
            LastSyncError = integration?.LastSyncError,
        };
    }
}
