namespace egeszsegkalauz.data.Models.Hevy;

public class BodyMeasurement
{
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public double? WeightKg { get; set; }
    public double? LeanMassKg { get; set; }
    public double? FatPercent { get; set; }
    public double? NeckCm { get; set; }
    public double? ShoulderCm { get; set; }
    public double? ChestCm { get; set; }
    public double? LeftBicepCm { get; set; }
    public double? RightBicepCm { get; set; }
    public double? LeftForearmCm { get; set; }
    public double? RightForearmCm { get; set; }
    public double? Abdomen { get; set; }
    public double? Waist { get; set; }
    public double? Hips { get; set; }
    public double? LeftThigh { get; set; }
    public double? RightThigh { get; set; }
    public double? LeftCalf { get; set; }
    public double? RightCalf { get; set; }
    public string? PictureUrl { get; set; }
}
