namespace egeszsegkalauz.data.Models;

public class BodyBatteryDailySummary : Entity
{
    public int UserId { get; set; }
    public required User User { get; set; }
    public HealthDataSource Source { get; set; }
    public DateOnly Date { get; set; }
    public int? Charged { get; set; }
    public int? Drained { get; set; }
    public int SampleCount { get; set; }
}
