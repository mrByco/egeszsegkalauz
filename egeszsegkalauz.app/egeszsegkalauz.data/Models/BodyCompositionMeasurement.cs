namespace egeszsegkalauz.data.Models;

public class BodyCompositionMeasurement : Entity
{
    public int UserId { get; set; }
    public required User User { get; set; }
    public HealthDataSource Source { get; set; }
    public DateTime Timestamp { get; set; }
    public DateOnly Date { get; set; }
    public double? WeightKg { get; set; }
    public double? Bmi { get; set; }
    public double? BodyFatPercent { get; set; }
    public double? BodyWaterPercent { get; set; }
    public double? BoneMassKg { get; set; }
    public double? MuscleMassKg { get; set; }
    public double? VisceralFat { get; set; }
    public int? MetabolicAge { get; set; }
}
