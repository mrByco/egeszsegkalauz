using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;

namespace egeszsegkalauz.services.Services.Integrations;

public interface IIntegrationService
{
    Task<List<IntegrationDto>> ListAsync(string userNanoid, CancellationToken cancellationToken);
    Task<IntegrationDto> ConnectAsync(string userNanoid, HealthDataSource source, string apiKey, CancellationToken cancellationToken);
    Task DisconnectAsync(string userNanoid, HealthDataSource source, CancellationToken cancellationToken);
    Task SetApiKeyWithoutValidationAsync(string email, HealthDataSource source, string apiKey, CancellationToken cancellationToken);
}
