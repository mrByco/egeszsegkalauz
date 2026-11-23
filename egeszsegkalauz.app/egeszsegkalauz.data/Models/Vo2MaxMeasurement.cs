namespace egeszsegkalauz.data.Models;

public class Vo2MaxMeasurement : Entity
{
    public int UserId { get; set; }
    public required User User { get; set; }
    public HealthDataSource Source { get; set; }
    public DateOnly Date { get; set; }
    public required string Sport { get; set; }
    public double Value { get; set; }
}
