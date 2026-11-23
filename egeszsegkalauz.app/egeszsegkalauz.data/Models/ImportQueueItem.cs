namespace egeszsegkalauz.data.Models;

public class ImportQueueItem
{
    public long Id { get; set; }
    public int JobId { get; set; }
    public required ImportJob Job { get; set; }
    public required string Kind { get; set; }
    public DateOnly Date { get; set; }
    public ImportQueueItemStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTime NotBefore { get; set; }
    public string? LastError { get; set; }
}
