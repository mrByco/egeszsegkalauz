namespace egeszsegkalauz.integrations.Common;

public class Vo2MaxReading
{
    public required DateOnly Date { get; init; }
    public required string Sport { get; init; }
    public required double Value { get; init; }
}
