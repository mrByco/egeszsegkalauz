namespace egeszsegkalauz.data.Models;

public class DailyHeartRateSummary : Entity
{
    public int UserId { get; set; }
    public required User User { get; set; }
    public HealthDataSource Source { get; set; }
    public DateOnly Date { get; set; }
    public int? RestingBpm { get; set; }
    public int? MinBpm { get; set; }
    public int? MaxBpm { get; set; }
    public int SampleCount { get; set; }
}
