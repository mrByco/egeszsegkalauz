namespace egeszsegkalauz.services.DTO;

public class GarminHeartRateDayDto
{
    public required DateOnly CalendarDate { get; set; }
    public double? RestingHeartRate { get; set; }
    public double? MinHeartRate { get; set; }
    public double? MaxHeartRate { get; set; }
    public double? LastSevenDaysAvgRestingHeartRate { get; set; }
    public required int ValueCount { get; set; }
}
