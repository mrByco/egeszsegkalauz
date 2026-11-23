namespace egeszsegkalauz.integrations.Common;

public class DailyHeartRate
{
    public required DateOnly Date { get; init; }
    public int? RestingBpm { get; init; }
    public int? MinBpm { get; init; }
    public int? MaxBpm { get; init; }
    public required List<HeartRateReading> Readings { get; init; }
}
