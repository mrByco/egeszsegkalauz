namespace egeszsegkalauz.data.Models.Garmin;

public class UserSummary
{
    public int UserId { get; set; }
    public DateOnly CalendarDate { get; set; }
    public double? TotalKilocalories { get; set; }
    public double? ActiveKilocalories { get; set; }
    public double? BmrKilocalories { get; set; }
    public double? TotalSteps { get; set; }
    public double? DailyStepGoal { get; set; }
    public double? TotalDistanceMeters { get; set; }
    public double? WellnessDistanceMeters { get; set; }
    public string? WellnessStartTimeGmt { get; set; }
    public string? WellnessEndTimeGmt { get; set; }
    public double? HighlyActiveSeconds { get; set; }
    public double? ActiveSeconds { get; set; }
    public double? SedentarySeconds { get; set; }
    public double? SleepingSeconds { get; set; }
    public double? ModerateIntensityMinutes { get; set; }
    public double? VigorousIntensityMinutes { get; set; }
    public double? IntensityMinutesGoal { get; set; }
    public double? FloorsAscended { get; set; }
    public double? FloorsDescended { get; set; }
    public double? MinHeartRate { get; set; }
    public double? MaxHeartRate { get; set; }
    public double? RestingHeartRate { get; set; }
    public double? LastSevenDaysAvgRestingHeartRate { get; set; }
    public double? MinAvgHeartRate { get; set; }
    public double? MaxAvgHeartRate { get; set; }
    public double? AverageStressLevel { get; set; }
    public double? MaxStressLevel { get; set; }
    public double? StressDuration { get; set; }
    public double? RestStressDuration { get; set; }
    public double? ActivityStressDuration { get; set; }
    public double? UncategorizedStressDuration { get; set; }
    public double? TotalStressDuration { get; set; }
    public double? LowStressDuration { get; set; }
    public double? MediumStressDuration { get; set; }
    public double? HighStressDuration { get; set; }
    public string? StressQualifier { get; set; }
    public double? MeasurableAwakeDuration { get; set; }
    public double? MeasurableAsleepDuration { get; set; }
    public double? BodyBatteryChargedValue { get; set; }
    public double? BodyBatteryDrainedValue { get; set; }
    public double? BodyBatteryHighestValue { get; set; }
    public double? BodyBatteryLowestValue { get; set; }
    public double? BodyBatteryMostRecentValue { get; set; }
    public double? BodyBatteryDuringSleep { get; set; }
    public double? BodyBatteryAtWakeTime { get; set; }
    public double? AverageSpo2 { get; set; }
    public double? LowestSpo2 { get; set; }
    public double? LatestSpo2 { get; set; }
    public double? AvgWakingRespirationValue { get; set; }
    public double? HighestRespirationValue { get; set; }
    public double? LowestRespirationValue { get; set; }
    public double? LatestRespirationValue { get; set; }
}
