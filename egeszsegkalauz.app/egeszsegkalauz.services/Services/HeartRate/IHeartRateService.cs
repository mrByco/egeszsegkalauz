using egeszsegkalauz.services.DTO;

namespace egeszsegkalauz.services.Services.HeartRate;

public interface IHeartRateService
{
    Task<List<DailyHeartRateDto>> ListDailyAsync(string userNanoid, int days, CancellationToken cancellationToken);
}
