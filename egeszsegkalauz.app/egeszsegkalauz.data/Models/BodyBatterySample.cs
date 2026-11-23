namespace egeszsegkalauz.data.Models;

public class BodyBatterySample
{
    public int UserId { get; set; }
    public HealthDataSource Source { get; set; }
    public DateOnly Date { get; set; }
    public DateTime Timestamp { get; set; }
    public short Level { get; set; }
}
