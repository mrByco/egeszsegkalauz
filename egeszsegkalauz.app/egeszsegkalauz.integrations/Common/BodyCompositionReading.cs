namespace egeszsegkalauz.integrations.Common;

public class BodyCompositionReading
{
    public required DateTime TimestampUtc { get; init; }
    public required DateOnly Date { get; init; }
    public double? WeightKg { get; init; }
    public double? Bmi { get; init; }
    public double? BodyFatPercent { get; init; }
    public double? BodyWaterPercent { get; init; }
    public double? BoneMassKg { get; init; }
    public double? MuscleMassKg { get; init; }
    public double? VisceralFat { get; init; }
    public int? MetabolicAge { get; init; }
}
