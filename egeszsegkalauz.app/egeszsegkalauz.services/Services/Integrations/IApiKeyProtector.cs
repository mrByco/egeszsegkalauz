namespace egeszsegkalauz.services.Services.Integrations;

public interface IApiKeyProtector
{
    string Protect(string apiKey);
    string Unprotect(string protectedApiKey);
}
