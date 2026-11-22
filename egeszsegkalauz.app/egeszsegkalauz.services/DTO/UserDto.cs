namespace egeszsegkalauz.services.DTO;

public class UserDto
{
    public required string Nanoid { get; set; }
    public required string Email { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
