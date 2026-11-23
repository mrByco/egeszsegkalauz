using egeszsegkalauz.data.Models;
using egeszsegkalauz.integrations.Common;
using egeszsegkalauz.integrations.Garmin;
using egeszsegkalauz.integrations.Hevy;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Exceptions;

namespace egeszsegkalauz.services.Services.Integrations;

public class HealthDataAdapters
{
    private readonly IGarminAdapterService garmin;
    private readonly IHevyAdapterService hevy;

    public HealthDataAdapters(IGarminAdapterService garmin, IHevyAdapterService hevy)
    {
        this.garmin = garmin;
        this.hevy = hevy;
    }

    public static IntegrationDescriptor Describe(HealthDataSource source)
    {
        return source switch
        {
            HealthDataSource.Garmin => new IntegrationDescriptor
            {
                AuthKind = IntegrationAuthKind.ApiKey,
                Capabilities = [IntegrationCapability.SummaryImport, IntegrationCapability.DailyImport],
                DeletesDataOnDisconnect = false,
            },
            // Hevy's developer terms require deleting synced data when the user disconnects.
            HealthDataSource.Hevy => new IntegrationDescriptor
            {
                AuthKind = IntegrationAuthKind.OAuth,
                Capabilities = [IntegrationCapability.Sync],
                DeletesDataOnDisconnect = true,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };
    }

    public IHealthDataAdapter For(HealthDataSource source)
    {
        return source switch
        {
            HealthDataSource.Garmin => garmin,
            _ => throw Unsupported(source, "API keys"),
        };
    }

    public ISummaryImportAdapter SummariesFor(HealthDataSource source)
    {
        return source switch
        {
            HealthDataSource.Garmin => garmin,
            _ => throw Unsupported(source, "summary imports"),
        };
    }

    public IDailyImportAdapter DailyFor(HealthDataSource source)
    {
        return source switch
        {
            HealthDataSource.Garmin => garmin,
            _ => throw Unsupported(source, "daily imports"),
        };
    }

    public IOAuthAdapter OAuthFor(HealthDataSource source)
    {
        return source switch
        {
            HealthDataSource.Hevy => hevy,
            _ => throw Unsupported(source, "OAuth"),
        };
    }

    public ICursorSyncAdapter SyncFor(HealthDataSource source)
    {
        return source switch
        {
            HealthDataSource.Hevy => hevy,
            _ => throw Unsupported(source, "sync"),
        };
    }

    public IWebhookAdapter WebhookFor(HealthDataSource source)
    {
        return source switch
        {
            HealthDataSource.Hevy => hevy,
            _ => throw Unsupported(source, "webhooks"),
        };
    }

    private static InvalidInputException Unsupported(HealthDataSource source, string feature) =>
        new($"{source} does not support {feature}.");
}
