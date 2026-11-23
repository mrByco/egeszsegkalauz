namespace egeszsegkalauz.data.Models.Garmin;

public class SleepSummary
{
    public int UserId { get; set; }
    public DateOnly CalendarDate { get; set; }
    public long? SleepId { get; set; }
    public double? SleepTimeSeconds { get; set; }
    public double? NapTimeSeconds { get; set; }
    public bool? SleepWindowConfirmed { get; set; }
    public long? SleepStartTimestampGMT { get; set; }
    public long? SleepEndTimestampGMT { get; set; }
    public long? SleepStartTimestampLocal { get; set; }
    public long? SleepEndTimestampLocal { get; set; }
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
    public double? SleepScoresOverallValue { get; set; }
    public string? SleepScoresOverallQualifierKey { get; set; }
}
