namespace egeszsegkalauz.data.Models.Hevy;

public class WorkoutExercise
{
    public int UserId { get; set; }
    public required string WorkoutId { get; set; }
    public int Index { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public string? ExerciseTemplateId { get; set; }
    public double? SupersetId { get; set; }
    public List<WorkoutSet> Sets { get; set; } = [];
}
