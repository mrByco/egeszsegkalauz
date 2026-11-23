namespace egeszsegkalauz.data.Models.Garmin;

public class Vo2Max
{
    public int UserId { get; set; }
    public DateOnly CalendarDate { get; set; }

    // Which object of Garmin's day it came from: "generic" or "cycling".
    public required string Kind { get; set; }
    public string? Sport { get; set; }
    public string? SubSport { get; set; }
    public double? Vo2MaxPreciseValue { get; set; }
    public double? Vo2MaxValue { get; set; }
}
