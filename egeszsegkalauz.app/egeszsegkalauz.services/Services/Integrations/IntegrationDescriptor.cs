using egeszsegkalauz.services.DTO;

namespace egeszsegkalauz.services.Services.Integrations;

public class IntegrationDescriptor
{
    public required IntegrationAuthKind AuthKind { get; init; }
    public required List<IntegrationCapability> Capabilities { get; init; }
    public required bool DeletesDataOnDisconnect { get; init; }
}
