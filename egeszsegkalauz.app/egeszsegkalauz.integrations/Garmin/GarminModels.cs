using System.Text.Json.Serialization;

namespace egeszsegkalauz.integrations.Garmin;

// Garmin sends some integer-looking values as floats, so every number is read as double.

internal class GarminDailyHeartRate
{
    public DateOnly? CalendarDate { get; set; }
    public double? RestingHeartRate { get; set; }
    public double? MinHeartRate { get; set; }
    public double? MaxHeartRate { get; set; }

    // Each entry is [epoch milliseconds, bpm]; bpm is null while the watch was off the wrist.
    public List<double?[]>? HeartRateValues { get; set; }
}

internal class GarminUserSummary
{
    public DateOnly? CalendarDate { get; set; }
    public double? TotalSteps { get; set; }
    public double? DailyStepGoal { get; set; }
    public double? TotalDistanceMeters { get; set; }
    public double? TotalKilocalories { get; set; }
    public double? ActiveKilocalories { get; set; }
    public double? BmrKilocalories { get; set; }
    public double? FloorsAscended { get; set; }
    public double? FloorsDescended { get; set; }
    public double? ModerateIntensityMinutes { get; set; }
    public double? VigorousIntensityMinutes { get; set; }
    public double? HighlyActiveSeconds { get; set; }
    public double? ActiveSeconds { get; set; }
    public double? SedentarySeconds { get; set; }
    public double? SleepingSeconds { get; set; }
    public double? RestingHeartRate { get; set; }
    public double? MinHeartRate { get; set; }
    public double? MaxHeartRate { get; set; }
    public double? AverageStressLevel { get; set; }
    public double? MaxStressLevel { get; set; }
    public double? LowStressDuration { get; set; }
    public double? MediumStressDuration { get; set; }
    public double? HighStressDuration { get; set; }
    public double? RestStressDuration { get; set; }
    public double? BodyBatteryHighestValue { get; set; }
    public double? BodyBatteryLowestValue { get; set; }
    public double? BodyBatteryChargedValue { get; set; }
    public double? BodyBatteryDrainedValue { get; set; }
    public double? BodyBatteryAtWakeTime { get; set; }
    public double? AverageSpo2 { get; set; }
    public double? LowestSpo2 { get; set; }
    public double? AvgWakingRespirationValue { get; set; }
}

internal class GarminSleepSummary
{
    public DateOnly? CalendarDate { get; set; }
    public double? SleepTimeSeconds { get; set; }
    public double? NapTimeSeconds { get; set; }
    public double? SleepStartTimestampGMT { get; set; }
    public double? SleepEndTimestampGMT { get; set; }
    public double? DeepSleepSeconds { get; set; }
    public double? LightSleepSeconds { get; set; }
    public double? RemSleepSeconds { get; set; }
    public double? AwakeSleepSeconds { get; set; }
    public double? UnmeasurableSleepSeconds { get; set; }
    public double? AwakeCount { get; set; }
    public double? AvgSleepStress { get; set; }
    public double? AverageRespirationValue { get; set; }
    public double? LowestRespirationValue { get; set; }
    public double? HighestRespirationValue { get; set; }
    public GarminSleepScores? SleepScores { get; set; }
}

internal class GarminSleepScores
{
    public GarminSleepScore? Overall { get; set; }
}

internal class GarminSleepScore
{
    public double? Value { get; set; }
    public string? QualifierKey { get; set; }
}

internal class GarminBodyBatteryReport
{
    public DateOnly? Date { get; set; }
    public double? Charged { get; set; }
    public double? Drained { get; set; }

    // Each entry is [epoch milliseconds, level].
    public List<double?[]>? BodyBatteryValuesArray { get; set; }
}

internal class GarminVo2MaxDay
{
    public GarminVo2Max? Generic { get; set; }
    public GarminVo2Max? Cycling { get; set; }
}

internal class GarminVo2Max
{
    public DateOnly? CalendarDate { get; set; }
    public double? Vo2MaxPreciseValue { get; set; }
    public double? Vo2MaxValue { get; set; }
    public string? Sport { get; set; }
}

internal class GarminWeight
{
    public DateOnly? CalendarDate { get; set; }
    public double? TimestampGMT { get; set; }
    public double? Date { get; set; }

    // Weight, bone mass and muscle mass are in grams.
    public double? Weight { get; set; }
    public double? Bmi { get; set; }
    public double? BodyFat { get; set; }
    public double? BodyWater { get; set; }
    public double? BoneMass { get; set; }
    public double? MuscleMass { get; set; }
    public double? VisceralFat { get; set; }
    public double? MetabolicAge { get; set; }
}

internal class GarminActivity
{
    public long ActivityId { get; set; }
    public string? ActivityName { get; set; }
    public GarminActivityType? ActivityType { get; set; }
    public double? BeginTimestamp { get; set; }
    public double? Duration { get; set; }
    public double? MovingDuration { get; set; }
    public double? Distance { get; set; }
    public double? Calories { get; set; }
    public double? AverageHR { get; set; }
    public double? MaxHR { get; set; }
    public double? ElevationGain { get; set; }
    public double? ElevationLoss { get; set; }
    public double? AverageSpeed { get; set; }
    public double? MaxSpeed { get; set; }
    public double? Steps { get; set; }
    public string? LocationName { get; set; }

    [JsonPropertyName("hrTimeInZone_1")]
    public double? HrTimeInZone1 { get; set; }

    [JsonPropertyName("hrTimeInZone_2")]
    public double? HrTimeInZone2 { get; set; }

    [JsonPropertyName("hrTimeInZone_3")]
    public double? HrTimeInZone3 { get; set; }

    [JsonPropertyName("hrTimeInZone_4")]
    public double? HrTimeInZone4 { get; set; }

    [JsonPropertyName("hrTimeInZone_5")]
    public double? HrTimeInZone5 { get; set; }
}

internal class GarminActivityType
{
    public string? TypeKey { get; set; }
}
