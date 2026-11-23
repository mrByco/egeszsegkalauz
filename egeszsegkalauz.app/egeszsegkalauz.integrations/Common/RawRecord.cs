namespace egeszsegkalauz.integrations.Common;

public class RawRecord
{
    public required string Endpoint { get; init; }
    public required string Key { get; init; }
    public DateOnly? Date { get; init; }
    public required string Json { get; init; }
}
