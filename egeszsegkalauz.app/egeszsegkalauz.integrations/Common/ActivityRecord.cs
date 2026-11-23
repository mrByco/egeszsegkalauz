namespace egeszsegkalauz.integrations.Common;

public class ActivityRecord
{
    public required string SourceActivityId { get; init; }
    public string? Name { get; init; }
    public string? Type { get; init; }
    public required DateTime StartUtc { get; init; }
    public double? DurationSeconds { get; init; }
    public double? MovingSeconds { get; init; }
    public double? DistanceMeters { get; init; }
    public double? Calories { get; init; }
    public double? AverageHeartRate { get; init; }
    public double? MaxHeartRate { get; init; }
    public double? ElevationGain { get; init; }
    public double? ElevationLoss { get; init; }
    public double? AverageSpeed { get; init; }
    public double? MaxSpeed { get; init; }
    public int? Steps { get; init; }
    public string? LocationName { get; init; }
    public double? HeartRateZone1Seconds { get; init; }
    public double? HeartRateZone2Seconds { get; init; }
    public double? HeartRateZone3Seconds { get; init; }
    public double? HeartRateZone4Seconds { get; init; }
    public double? HeartRateZone5Seconds { get; init; }
}
