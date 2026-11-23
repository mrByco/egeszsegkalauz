namespace egeszsegkalauz.integrations.Common;

public class BodyBatteryDay
{
    public required DateOnly Date { get; init; }
    public int? Charged { get; init; }
    public int? Drained { get; init; }
    public required List<BodyBatteryReading> Readings { get; init; }
}
