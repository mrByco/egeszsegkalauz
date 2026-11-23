using egeszsegkalauz.integrations.Garmin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace egeszsegkalauz.integrations;

public static class IntegrationsServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services)
    {
        services.AddOptions<GarminOptions>().BindConfiguration(GarminOptions.SectionName);
        services.AddSingleton<GarminTokenCache>();
        services.AddHttpClient<IGarminAdapterService, GarminAdapterService>((provider, client) =>
            {
                client.BaseAddress = provider.GetRequiredService<IOptions<GarminOptions>>().Value.BaseUrl;
            })
            .AddStandardResilienceHandler();
        return services;
    }
}
