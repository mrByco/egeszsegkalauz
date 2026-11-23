namespace egeszsegkalauz.services.DTO;

public class SummaryImportResultDto
{
    public DateOnly? From { get; set; }
    public required DateOnly To { get; set; }
    public required List<EndpointRecordCountDto> Endpoints { get; set; }
}
