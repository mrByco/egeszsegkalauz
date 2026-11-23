namespace egeszsegkalauz.data.Models.Hevy;

public class ExerciseTemplate
{
    public int UserId { get; set; }
    public required string Id { get; set; }
    public string? Title { get; set; }
    public string? Type { get; set; }
    public string? PrimaryMuscleGroup { get; set; }
    public List<string> SecondaryMuscleGroups { get; set; } = [];
    public string? Equipment { get; set; }
    public bool? IsCustom { get; set; }
}
