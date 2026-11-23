namespace egeszsegkalauz.data.Models;

public class ExerciseActivity : Entity
{
    public int UserId { get; set; }
    public required User User { get; set; }
    public HealthDataSource Source { get; set; }
    public required string SourceActivityId { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public DateTime StartUtc { get; set; }
    public double? DurationSeconds { get; set; }
    public double? MovingSeconds { get; set; }
    public double? DistanceMeters { get; set; }
    public double? Calories { get; set; }
    public double? AverageHeartRate { get; set; }
    public double? MaxHeartRate { get; set; }
    public double? ElevationGain { get; set; }
    public double? ElevationLoss { get; set; }
    public double? AverageSpeed { get; set; }
    public double? MaxSpeed { get; set; }
    public int? Steps { get; set; }
    public string? LocationName { get; set; }
    public double? HeartRateZone1Seconds { get; set; }
    public double? HeartRateZone2Seconds { get; set; }
    public double? HeartRateZone3Seconds { get; set; }
    public double? HeartRateZone4Seconds { get; set; }
    public double? HeartRateZone5Seconds { get; set; }
}
