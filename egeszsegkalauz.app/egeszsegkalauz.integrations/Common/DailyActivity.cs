namespace egeszsegkalauz.integrations.Common;

public class DailyActivity
{
    public required DateOnly Date { get; init; }
    public int? Steps { get; init; }
    public int? StepGoal { get; init; }
    public double? DistanceMeters { get; init; }
    public double? TotalKilocalories { get; init; }
    public double? ActiveKilocalories { get; init; }
    public double? BmrKilocalories { get; init; }
    public double? FloorsAscended { get; init; }
    public double? FloorsDescended { get; init; }
    public int? ModerateIntensityMinutes { get; init; }
    public int? VigorousIntensityMinutes { get; init; }
    public int? HighlyActiveSeconds { get; init; }
    public int? ActiveSeconds { get; init; }
    public int? SedentarySeconds { get; init; }
    public int? SleepingSeconds { get; init; }
    public int? RestingHeartRate { get; init; }
    public int? MinHeartRate { get; init; }
    public int? MaxHeartRate { get; init; }
    public int? AverageStressLevel { get; init; }
    public int? MaxStressLevel { get; init; }
    public int? LowStressSeconds { get; init; }
    public int? MediumStressSeconds { get; init; }
    public int? HighStressSeconds { get; init; }
    public int? RestStressSeconds { get; init; }
    public int? BodyBatteryHighest { get; init; }
    public int? BodyBatteryLowest { get; init; }
    public int? BodyBatteryCharged { get; init; }
    public int? BodyBatteryDrained { get; init; }
    public int? BodyBatteryAtWake { get; init; }
    public double? AverageSpo2 { get; init; }
    public double? LowestSpo2 { get; init; }
    public double? AverageWakingRespiration { get; init; }
}
