using egeszsegkalauz.services.DTO;

namespace egeszsegkalauz.services.Services.Garmin;

public interface IGarminDataService
{
    Task<List<GarminHeartRateDayDto>> ListHeartRateDaysAsync(string userNanoid, int days, CancellationToken cancellationToken);
}
