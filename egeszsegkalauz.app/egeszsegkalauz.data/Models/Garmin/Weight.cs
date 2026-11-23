namespace egeszsegkalauz.data.Models.Garmin;

public class Weight
{
    public int UserId { get; set; }

    // Epoch milliseconds, as Garmin sends it.
    public long TimestampGMT { get; set; }
    public long? Date { get; set; }
    public DateOnly? CalendarDate { get; set; }

    // Grams, as Garmin sends them.
    public double? WeightGrams { get; set; }
    public double? Bmi { get; set; }
    public double? BodyFat { get; set; }
    public double? BodyWater { get; set; }
    public double? BoneMass { get; set; }
    public double? MuscleMass { get; set; }
    public double? PhysiqueRating { get; set; }
    public double? VisceralFat { get; set; }
    public double? MetabolicAge { get; set; }
    public double? WeightDelta { get; set; }
}
