namespace egeszsegkalauz.data.Models;

public class ImportJob : Entity
{
    public int UserId { get; set; }
    public required User User { get; set; }
    public HealthDataSource Source { get; set; }
    public ImportJobStatus Status { get; set; }
    public int TotalQueries { get; set; }
    public int CompletedQueries { get; set; }
    public int FailedQueries { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? LastError { get; set; }
    public List<ImportQueueItem> Items { get; set; } = [];
}
