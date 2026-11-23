namespace egeszsegkalauz.data.Models;

// High-volume time series: keyed by (user, source, timestamp) instead of deriving from Entity.
public class HeartRateSample
{
    public int UserId { get; set; }
    public HealthDataSource Source { get; set; }
    public DateOnly Date { get; set; }
    public DateTime Timestamp { get; set; }
    public short Bpm { get; set; }
}
