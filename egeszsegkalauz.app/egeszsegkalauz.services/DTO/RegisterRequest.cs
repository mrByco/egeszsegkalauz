using System.ComponentModel.DataAnnotations;

namespace egeszsegkalauz.services.DTO;

public class RegisterRequest
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    [EmailAddress]
    [MaxLength(254)]
    public required string Email { get; set; }

    [MinLength(MinPasswordLength)]
    [MaxLength(MaxPasswordLength)]
    public required string Password { get; set; }
}
