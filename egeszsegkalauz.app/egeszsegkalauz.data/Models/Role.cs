namespace egeszsegkalauz.data.Models;

public class Role : Entity
{
    public const string AdminName = "Admin";

    public required string Name { get; set; }
    public bool IsSystem { get; set; }
    public List<Permission> Permissions { get; set; } = [];
    public List<User> Users { get; set; } = [];
}
