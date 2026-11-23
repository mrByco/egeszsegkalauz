namespace egeszsegkalauz.integrations.Common;

public class IntegrationKeyRejectedException : IntegrationUnavailableException
{
    public IntegrationKeyRejectedException(string message) : base(message)
    {
    }
}
