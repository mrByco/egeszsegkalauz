namespace egeszsegkalauz.integrations.Common;

public class SummaryChunk
{
    public required string Endpoint { get; init; }
    public required List<RawRecord> Raw { get; init; }
    public List<SleepSession> Sleep { get; init; } = [];
    public List<BodyBatteryDay> BodyBattery { get; init; } = [];
    public List<Vo2MaxReading> Vo2Max { get; init; } = [];
    public List<BodyCompositionReading> BodyComposition { get; init; } = [];
    public List<ActivityRecord> Activities { get; init; } = [];
}
