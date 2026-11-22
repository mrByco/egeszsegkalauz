namespace egeszsegkalauz.data.Models;

public class User : Entity
{
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
}
