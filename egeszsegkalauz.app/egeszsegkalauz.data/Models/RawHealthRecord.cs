namespace egeszsegkalauz.data.Models;

public class RawHealthRecord
{
    public int UserId { get; set; }
    public HealthDataSource Source { get; set; }
    public required string Endpoint { get; set; }
    public required string Key { get; set; }
    public DateOnly? Date { get; set; }
    public required string Payload { get; set; }
    public DateTime FetchedAt { get; set; }
}
