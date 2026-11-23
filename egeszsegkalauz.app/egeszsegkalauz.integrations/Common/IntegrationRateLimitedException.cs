namespace egeszsegkalauz.integrations.Common;

public class IntegrationRateLimitedException : IntegrationUnavailableException
{
    public IntegrationRateLimitedException(string message, TimeSpan pause) : base(message)
    {
        Pause = pause;
    }

    public TimeSpan Pause { get; }
}
