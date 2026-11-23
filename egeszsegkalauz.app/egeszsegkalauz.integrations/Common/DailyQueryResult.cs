namespace egeszsegkalauz.integrations.Common;

public class DailyQueryResult
{
    public required RawRecord Raw { get; init; }
    public DailyHeartRate? HeartRate { get; init; }
    public DailyActivity? Activity { get; init; }
}
