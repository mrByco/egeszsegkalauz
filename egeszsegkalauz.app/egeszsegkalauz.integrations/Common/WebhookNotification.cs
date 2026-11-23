namespace egeszsegkalauz.integrations.Common;

public class WebhookNotification
{
    public required string Type { get; init; }
    public required string ExternalUserId { get; init; }
    public string? ItemId { get; init; }
}
