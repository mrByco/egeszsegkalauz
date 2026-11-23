namespace egeszsegkalauz.services.DTO;

public class SummaryImportResultDto
{
    public DateOnly? From { get; set; }
    public required DateOnly To { get; set; }
    public required int RawRecords { get; set; }
    public required int SleepNights { get; set; }
    public required int BodyBatteryDays { get; set; }
    public required int Vo2MaxReadings { get; set; }
    public required int BodyCompositionReadings { get; set; }
    public required int Activities { get; set; }
}
