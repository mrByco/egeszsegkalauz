namespace egeszsegkalauz.data.Models.Hevy;

public class WorkoutSet
{
    public int UserId { get; set; }
    public required string WorkoutId { get; set; }
    public int ExerciseIndex { get; set; }
    public int Index { get; set; }
    public string? Type { get; set; }
    public double? WeightKg { get; set; }
    public double? Reps { get; set; }
    public double? DistanceMeters { get; set; }
    public double? DurationSeconds { get; set; }
    public double? Rpe { get; set; }
    public double? CustomMetric { get; set; }
}
