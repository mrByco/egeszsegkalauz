namespace egeszsegkalauz.data.Models;

public class RefreshToken : Entity
{
    public int UserId { get; set; }
    public required User User { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RotatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
