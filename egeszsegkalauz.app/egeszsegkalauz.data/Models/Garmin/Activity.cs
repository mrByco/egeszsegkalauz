namespace egeszsegkalauz.data.Models.Garmin;

public class Activity
{
    public int UserId { get; set; }
    public long ActivityId { get; set; }
    public string? ActivityName { get; set; }
    public string? ActivityTypeKey { get; set; }
    public string? StartTimeLocal { get; set; }
    public string? StartTimeGMT { get; set; }
    public long? BeginTimestamp { get; set; }
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
    public double? HrTimeInZone1 { get; set; }
    public double? HrTimeInZone2 { get; set; }
    public double? HrTimeInZone3 { get; set; }
    public double? HrTimeInZone4 { get; set; }
    public double? HrTimeInZone5 { get; set; }
}
