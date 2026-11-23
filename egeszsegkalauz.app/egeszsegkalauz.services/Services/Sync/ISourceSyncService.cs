using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;

namespace egeszsegkalauz.services.Services.Sync;

public interface ISourceSyncService
{
    Task<SyncResultDto> SyncAsync(string userNanoid, HealthDataSource source, CancellationToken cancellationToken);
    Task<bool> SyncNextDueAsync(CancellationToken cancellationToken);
    Task<bool> HandleWebhookAsync(HealthDataSource source, string body, Func<string, string?> header,
        CancellationToken cancellationToken);
}
