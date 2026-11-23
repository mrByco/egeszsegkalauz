namespace egeszsegkalauz.integrations.Common;

public class BodyBatteryReading
{
    public required DateTime TimestampUtc { get; init; }
    public required int Level { get; init; }
}
