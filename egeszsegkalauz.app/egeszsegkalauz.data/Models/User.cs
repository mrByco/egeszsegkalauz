namespace egeszsegkalauz.data.Models;

public class User : Entity
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<Role> Roles { get; set; } = [];
}
