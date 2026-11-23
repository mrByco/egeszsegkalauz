using System.ComponentModel.DataAnnotations;

namespace egeszsegkalauz.services.DTO;

public class CompleteOAuthRequest
{
    [Required]
    [MaxLength(2000)]
    public required string Code { get; set; }

    [Required]
    [MaxLength(4000)]
    public required string State { get; set; }
}
