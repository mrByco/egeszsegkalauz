using egeszsegkalauz.data;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.Integrations;

public class IntegrationService : IIntegrationService
{
    private readonly AppDbContext db;
    private readonly HealthDataAdapters adapters;
    private readonly IApiKeyProtector apiKeyProtector;

    public IntegrationService(AppDbContext db, HealthDataAdapters adapters, IApiKeyProtector apiKeyProtector)
    {
        this.db = db;
        this.adapters = adapters;
        this.apiKeyProtector = apiKeyProtector;
    }

    public async Task<List<IntegrationDto>> ListAsync(string userNanoid, CancellationToken cancellationToken)
    {
        var connected = await db.UserIntegrations
            .AsNoTracking()
            .Where(x => x.User.Nanoid == userNanoid)
            .ToDictionaryAsync(x => x.Source, cancellationToken);

        return Enum.GetValues<HealthDataSource>()
            .Select(source => connected.TryGetValue(source, out var integration)
                ? ToDto(integration)
                : new IntegrationDto { Source = source, Connected = false })
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
        return ToDto(await StoreAsync(user, source, apiKey, cancellationToken));
    }

    public async Task DisconnectAsync(string userNanoid, HealthDataSource source, CancellationToken cancellationToken)
    {
        await db.UserIntegrations
            .Where(x => x.User.Nanoid == userNanoid && x.Source == source)
            .ExecuteDeleteAsync(cancellationToken);
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
            integration = new UserIntegration { User = user, Source = source, ProtectedApiKey = "", ApiKeyHint = "" };
            db.UserIntegrations.Add(integration);
        }

        integration.ProtectedApiKey = apiKeyProtector.Protect(apiKey);
        integration.ApiKeyHint = Hint(apiKey);
        await db.SaveChangesAsync(cancellationToken);
        return integration;
    }

    private static string Hint(string apiKey) => apiKey.Length <= 4 ? "…" : "…" + apiKey[^4..];

    private static IntegrationDto ToDto(UserIntegration integration)
    {
        return new IntegrationDto
        {
            Source = integration.Source,
            Connected = true,
            ApiKeyHint = integration.ApiKeyHint,
            FirstDataDate = integration.FirstDataDate,
            SummariesImportedAt = integration.SummariesImportedAt,
        };
    }
}
