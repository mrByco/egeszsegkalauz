namespace egeszsegkalauz.integrations.Common;

public interface IWebhookAdapter
{
    string SignatureHeader { get; }
    bool IsSignatureValid(string body, string? signature);
    WebhookNotification? Parse(string body);
    Task<SyncChunk> FetchAsync(string accessToken, WebhookNotification notification, CancellationToken cancellationToken);
}
