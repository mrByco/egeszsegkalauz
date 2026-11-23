namespace egeszsegkalauz.services.DTO;

public class SyncResultDto
{
    public required bool FullSync { get; set; }
    public required List<EndpointRecordCountDto> Endpoints { get; set; }
    public required int Deleted { get; set; }
}
