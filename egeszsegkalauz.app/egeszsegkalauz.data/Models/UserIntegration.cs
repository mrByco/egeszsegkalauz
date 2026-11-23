namespace egeszsegkalauz.data.Models;

public class UserIntegration : Entity
{
    public int UserId { get; set; }
    public required User User { get; set; }
    public HealthDataSource Source { get; set; }
    // The API key, or for OAuth sources the current access token.
    public required string ProtectedApiKey { get; set; }
    public string? ApiKeyHint { get; set; }
    public string? ProtectedRefreshToken { get; set; }
    public DateTime? AccessTokenExpiresAt { get; set; }
    public string? ExternalUserId { get; set; }
    public string? AccountName { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public DateTime? LastSyncAttemptAt { get; set; }
    public string? LastSyncError { get; set; }
    public DateOnly? FirstDataDate { get; set; }
    public DateTime? SummariesImportedAt { get; set; }
    public DateOnly? SummariesImportedThrough { get; set; }
}
