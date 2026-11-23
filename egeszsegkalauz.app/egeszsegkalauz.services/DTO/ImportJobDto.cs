using egeszsegkalauz.data.Models;

namespace egeszsegkalauz.services.DTO;

public class ImportJobDto
{
    public required string Nanoid { get; set; }
    public required HealthDataSource Source { get; set; }
    public required ImportJobStatus Status { get; set; }
    public required int TotalQueries { get; set; }
    public required int CompletedQueries { get; set; }
    public required int FailedQueries { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? LastError { get; set; }
}
