namespace egeszsegkalauz.integrations.Common;

public interface IHealthDataAdapter
{
    Task<bool> IsApiKeyValidAsync(string apiKey, CancellationToken cancellationToken);
    Task<DateOnly?> FindFirstDataDateAsync(string apiKey, DateOnly today, CancellationToken cancellationToken);
}
