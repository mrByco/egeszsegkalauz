using egeszsegkalauz.data.Models;

namespace egeszsegkalauz.services.DTO;

public class IntegrationDto
{
    public required HealthDataSource Source { get; set; }
    public required IntegrationAuthKind AuthKind { get; set; }
    public required List<IntegrationCapability> Capabilities { get; set; }
    public required bool DeletesDataOnDisconnect { get; set; }
    public required bool Connected { get; set; }
    public string? ApiKeyHint { get; set; }
    public string? AccountName { get; set; }
    public DateOnly? FirstDataDate { get; set; }
    public DateTime? SummariesImportedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncError { get; set; }
}
