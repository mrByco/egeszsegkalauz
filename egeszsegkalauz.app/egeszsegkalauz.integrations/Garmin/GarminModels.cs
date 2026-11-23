using System.Text.Json;
using System.Text.Json.Serialization;

namespace egeszsegkalauz.integrations.Garmin;

// Garmin's payloads as Garmin names them. Numbers are read as double because Garmin sends some integer
// fields as floats; timestamps stay as Garmin sends them (epoch milliseconds or "yyyy-MM-ddTHH:mm:ss.f").

public static class GarminJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static T? Read<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
}

public class GarminHeartRateDay
{
    public DateOnly? CalendarDate { get; set; }
    public string? StartTimestampGMT { get; set; }
    public string? EndTimestampGMT { get; set; }
    public string? StartTimestampLocal { get; set; }
    public string? EndTimestampLocal { get; set; }
    public double? MaxHeartRate { get; set; }
    public double? MinHeartRate { get; set; }
    public double? RestingHeartRate { get; set; }
    public double? LastSevenDaysAvgRestingHeartRate { get; set; }

    // Each entry is [epoch milliseconds, bpm]; bpm is null while the watch was off the wrist.
    public List<double?[]>? HeartRateValues { get; set; }
}

public class GarminUserSummary
{
    public DateOnly? CalendarDate { get; set; }
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

public class GarminSleepSummary
{
    public double? Id { get; set; }
    public DateOnly? CalendarDate { get; set; }
    public double? SleepTimeSeconds { get; set; }
    public double? NapTimeSeconds { get; set; }
    public bool? SleepWindowConfirmed { get; set; }
    public double? SleepStartTimestampGMT { get; set; }
    public double? SleepEndTimestampGMT { get; set; }
    public double? SleepStartTimestampLocal { get; set; }
    public double? SleepEndTimestampLocal { get; set; }
    public double? UnmeasurableSleepSeconds { get; set; }
    public double? DeepSleepSeconds { get; set; }
    public double? LightSleepSeconds { get; set; }
    public double? RemSleepSeconds { get; set; }
    public double? AwakeSleepSeconds { get; set; }
    public bool? DeviceRemCapable { get; set; }
    public double? AverageRespirationValue { get; set; }
    public double? LowestRespirationValue { get; set; }
    public double? HighestRespirationValue { get; set; }
    public double? AwakeCount { get; set; }
    public double? AvgSleepStress { get; set; }
    public string? SleepScoreFeedback { get; set; }
    public string? SleepScoreInsight { get; set; }
    public GarminSleepScores? SleepScores { get; set; }
}

public class GarminSleepScores
{
    public GarminSleepScore? Overall { get; set; }
}

public class GarminSleepScore
{
    public double? Value { get; set; }
    public string? QualifierKey { get; set; }
}

public class GarminBodyBatteryReport
{
    public DateOnly? Date { get; set; }
    public double? Charged { get; set; }
    public double? Drained { get; set; }
    public string? StartTimestampGMT { get; set; }
    public string? EndTimestampGMT { get; set; }
    public string? StartTimestampLocal { get; set; }
    public string? EndTimestampLocal { get; set; }

    // Each entry is [epoch milliseconds, level].
    public List<double?[]>? BodyBatteryValuesArray { get; set; }
}

public class GarminVo2MaxDay
{
    public GarminVo2Max? Generic { get; set; }
    public GarminVo2Max? Cycling { get; set; }
}

public class GarminVo2Max
{
    public DateOnly? CalendarDate { get; set; }
    public double? Vo2MaxPreciseValue { get; set; }
    public double? Vo2MaxValue { get; set; }
    public string? Sport { get; set; }
    public string? SubSport { get; set; }
}

public class GarminWeight
{
    public double? Date { get; set; }
    public DateOnly? CalendarDate { get; set; }
    public double? TimestampGMT { get; set; }

    // Weight, bone mass and muscle mass are in grams.
    public double? Weight { get; set; }
    public double? Bmi { get; set; }
    public double? BodyFat { get; set; }
    public double? BodyWater { get; set; }
    public double? BoneMass { get; set; }
    public double? MuscleMass { get; set; }
    public double? PhysiqueRating { get; set; }
    public double? VisceralFat { get; set; }
    public double? MetabolicAge { get; set; }
    public double? WeightDelta { get; set; }
}

public class GarminActivity
{
    public long ActivityId { get; set; }
    public string? ActivityName { get; set; }
    public GarminActivityType? ActivityType { get; set; }
    public string? StartTimeLocal { get; set; }
    public string? StartTimeGMT { get; set; }
    public double? BeginTimestamp { get; set; }
    public double? Duration { get; set; }
    public double? ElapsedDuration { get; set; }
    public double? MovingDuration { get; set; }
    public double? Distance { get; set; }
    public double? Calories { get; set; }
    public double? BmrCalories { get; set; }
    public double? AverageHR { get; set; }
    public double? MaxHR { get; set; }
    public double? ElevationGain { get; set; }
    public double? ElevationLoss { get; set; }
    public double? AverageSpeed { get; set; }
    public double? MaxSpeed { get; set; }
    public double? Steps { get; set; }
    public double? LapCount { get; set; }
    public double? StartLatitude { get; set; }
    public double? StartLongitude { get; set; }
    public string? LocationName { get; set; }
    public string? Manufacturer { get; set; }

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

public class GarminActivityType
{
    public string? TypeKey { get; set; }
}
