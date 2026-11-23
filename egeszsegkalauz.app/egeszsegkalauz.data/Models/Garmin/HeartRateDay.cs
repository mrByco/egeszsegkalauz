namespace egeszsegkalauz.data.Models.Garmin;

public class HeartRateDay
{
    public int UserId { get; set; }
    public DateOnly CalendarDate { get; set; }
    public string? StartTimestampGMT { get; set; }
    public string? EndTimestampGMT { get; set; }
    public string? StartTimestampLocal { get; set; }
    public string? EndTimestampLocal { get; set; }
    public double? MaxHeartRate { get; set; }
    public double? MinHeartRate { get; set; }
    public double? RestingHeartRate { get; set; }
    public double? LastSevenDaysAvgRestingHeartRate { get; set; }
}
