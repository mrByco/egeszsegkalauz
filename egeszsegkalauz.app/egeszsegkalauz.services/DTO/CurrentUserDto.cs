using egeszsegkalauz.data.Models;

namespace egeszsegkalauz.services.DTO;

public class CurrentUserDto
{
    public required string Nanoid { get; set; }
    public required string Email { get; set; }
    public required List<Permission> Permissions { get; set; }
}
