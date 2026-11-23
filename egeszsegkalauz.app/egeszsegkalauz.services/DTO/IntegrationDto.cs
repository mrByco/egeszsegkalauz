using egeszsegkalauz.data.Models;

namespace egeszsegkalauz.services.DTO;

public class IntegrationDto
{
    public required HealthDataSource Source { get; set; }
    public required bool Connected { get; set; }
    public string? ApiKeyHint { get; set; }
    public DateOnly? FirstDataDate { get; set; }
    public DateTime? SummariesImportedAt { get; set; }
}
