namespace egeszsegkalauz.integrations.Common;

public class IntegrationUnavailableException : Exception
{
    public IntegrationUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
