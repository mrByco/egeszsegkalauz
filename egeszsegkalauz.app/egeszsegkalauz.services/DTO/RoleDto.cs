using egeszsegkalauz.data.Models;

namespace egeszsegkalauz.services.DTO;

public class RoleDto
{
    public required string Nanoid { get; set; }
    public required string Name { get; set; }
    public required bool IsSystem { get; set; }
    public required List<Permission> Permissions { get; set; }
    public required int UserCount { get; set; }
}
