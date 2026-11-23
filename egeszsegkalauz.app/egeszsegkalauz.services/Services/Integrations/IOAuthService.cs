using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;

namespace egeszsegkalauz.services.Services.Integrations;

public interface IOAuthService
{
    OAuthStartDto Start(string userNanoid, HealthDataSource source);
    Task<IntegrationDto> CompleteAsync(string userNanoid, HealthDataSource source, CompleteOAuthRequest request,
        CancellationToken cancellationToken);
}
