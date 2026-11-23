namespace egeszsegkalauz.data.Models.Garmin;

public class BodyBatteryValue
{
    public int UserId { get; set; }

    // Epoch milliseconds, as Garmin sends it.
    public long Timestamp { get; set; }
    public DateOnly Date { get; set; }
    public double? BodyBatteryLevel { get; set; }
}
