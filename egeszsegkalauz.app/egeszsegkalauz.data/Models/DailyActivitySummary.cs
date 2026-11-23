namespace egeszsegkalauz.data.Models;

public class DailyActivitySummary : Entity
{
    public int UserId { get; set; }
    public required User User { get; set; }
    public HealthDataSource Source { get; set; }
    public DateOnly Date { get; set; }
    public int? Steps { get; set; }
    public int? StepGoal { get; set; }
    public double? DistanceMeters { get; set; }
    public double? TotalKilocalories { get; set; }
    public double? ActiveKilocalories { get; set; }
    public double? BmrKilocalories { get; set; }
    public double? FloorsAscended { get; set; }
    public double? FloorsDescended { get; set; }
    public int? ModerateIntensityMinutes { get; set; }
    public int? VigorousIntensityMinutes { get; set; }
    public int? HighlyActiveSeconds { get; set; }
    public int? ActiveSeconds { get; set; }
    public int? SedentarySeconds { get; set; }
    public int? SleepingSeconds { get; set; }
    public int? RestingHeartRate { get; set; }
    public int? MinHeartRate { get; set; }
    public int? MaxHeartRate { get; set; }
    public int? AverageStressLevel { get; set; }
    public int? MaxStressLevel { get; set; }
    public int? LowStressSeconds { get; set; }
    public int? MediumStressSeconds { get; set; }
    public int? HighStressSeconds { get; set; }
    public int? RestStressSeconds { get; set; }
    public int? BodyBatteryHighest { get; set; }
    public int? BodyBatteryLowest { get; set; }
    public int? BodyBatteryCharged { get; set; }
    public int? BodyBatteryDrained { get; set; }
    public int? BodyBatteryAtWake { get; set; }
    public double? AverageSpo2 { get; set; }
    public double? LowestSpo2 { get; set; }
    public double? AverageWakingRespiration { get; set; }
}
