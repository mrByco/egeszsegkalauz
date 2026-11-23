using System.ComponentModel.DataAnnotations;

namespace egeszsegkalauz.services.DTO;

public class ConnectIntegrationRequest
{
    [Required]
    [MaxLength(500)]
    public required string ApiKey { get; set; }
}
