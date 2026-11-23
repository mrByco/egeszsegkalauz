using egeszsegkalauz.data.Models;

namespace egeszsegkalauz.services.DTO;

public class DailyHeartRateDto
{
    public required DateOnly Date { get; set; }
    public required HealthDataSource Source { get; set; }
    public int? RestingBpm { get; set; }
    public int? MinBpm { get; set; }
    public int? MaxBpm { get; set; }
    public required int SampleCount { get; set; }
}
