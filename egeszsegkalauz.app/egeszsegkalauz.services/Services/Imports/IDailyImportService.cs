using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;

namespace egeszsegkalauz.services.Services.Imports;

public interface IDailyImportService
{
    Task<ImportJobDto> StartAsync(string userNanoid, HealthDataSource source, CancellationToken cancellationToken);
    Task<DailyImportStatusDto> GetStatusAsync(string userNanoid, HealthDataSource source, CancellationToken cancellationToken);
    Task CancelAsync(string userNanoid, HealthDataSource source, CancellationToken cancellationToken);
    Task<TimeSpan> ProcessNextAsync(CancellationToken cancellationToken);
}
