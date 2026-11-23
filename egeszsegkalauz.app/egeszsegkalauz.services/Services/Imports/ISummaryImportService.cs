using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;

namespace egeszsegkalauz.services.Services.Imports;

public interface ISummaryImportService
{
    Task<SummaryImportResultDto> ImportAsync(string userNanoid, HealthDataSource source, CancellationToken cancellationToken);
}
