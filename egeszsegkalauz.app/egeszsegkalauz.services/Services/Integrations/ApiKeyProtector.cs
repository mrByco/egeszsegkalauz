using Microsoft.AspNetCore.DataProtection;

namespace egeszsegkalauz.services.Services.Integrations;

public class ApiKeyProtector : IApiKeyProtector
{
    private readonly IDataProtector protector;

    public ApiKeyProtector(IDataProtectionProvider dataProtectionProvider)
    {
        protector = dataProtectionProvider.CreateProtector("egeszsegkalauz.integrations.api-key");
    }

    public string Protect(string apiKey)
    {
        return protector.Protect(apiKey);
    }

    public string Unprotect(string protectedApiKey)
    {
        return protector.Unprotect(protectedApiKey);
    }
}
