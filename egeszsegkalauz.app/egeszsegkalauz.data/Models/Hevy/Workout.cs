namespace egeszsegkalauz.data.Models.Hevy;

public class Workout
{
    public int UserId { get; set; }
    public required string Id { get; set; }
    public string? Title { get; set; }
    public string? RoutineId { get; set; }
    public string? Description { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<WorkoutExercise> Exercises { get; set; } = [];
}
