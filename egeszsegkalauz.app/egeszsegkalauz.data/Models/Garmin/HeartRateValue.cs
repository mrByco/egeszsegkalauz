namespace egeszsegkalauz.data.Models.Garmin;

public class HeartRateValue
{
    public int UserId { get; set; }

    // Epoch milliseconds, as Garmin sends it.
    public long Timestamp { get; set; }
    public DateOnly CalendarDate { get; set; }
    public double? HeartRate { get; set; }
}
