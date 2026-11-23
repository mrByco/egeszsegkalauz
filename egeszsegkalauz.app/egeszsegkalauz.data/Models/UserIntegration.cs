namespace egeszsegkalauz.data.Models;

public class UserIntegration : Entity
{
    public int UserId { get; set; }
    public required User User { get; set; }
    public HealthDataSource Source { get; set; }
    public required string ProtectedApiKey { get; set; }
    public required string ApiKeyHint { get; set; }
    public DateOnly? FirstDataDate { get; set; }
    public DateTime? SummariesImportedAt { get; set; }
    public DateOnly? SummariesImportedThrough { get; set; }
}
