using egeszsegkalauz.integrations.Garmin;
using egeszsegkalauz.integrations.Hevy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
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

        services.AddOptions<HevyOptions>().BindConfiguration(HevyOptions.SectionName);
        services.AddHttpClient<IHevyAdapterService, HevyAdapterService>((provider, client) =>
            {
                client.BaseAddress = provider.GetRequiredService<IOptions<HevyOptions>>().Value.ApiBaseUrl;
            })
            // Each refresh token works once, so a retried token request could burn it.
            .AddStandardResilienceHandler(resilience => resilience.Retry.DisableForUnsafeHttpMethods());
        return services;
    }
}
