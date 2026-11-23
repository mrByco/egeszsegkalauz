namespace egeszsegkalauz.integrations.Common;

public class HeartRateReading
{
    public required DateTime TimestampUtc { get; init; }
    public required int Bpm { get; init; }
}
