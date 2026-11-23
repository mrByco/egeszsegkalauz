namespace egeszsegkalauz.data.Models.Garmin;

public class BodyBatteryDay
{
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public double? Charged { get; set; }
    public double? Drained { get; set; }
    public string? StartTimestampGMT { get; set; }
    public string? EndTimestampGMT { get; set; }
    public string? StartTimestampLocal { get; set; }
    public string? EndTimestampLocal { get; set; }
}
