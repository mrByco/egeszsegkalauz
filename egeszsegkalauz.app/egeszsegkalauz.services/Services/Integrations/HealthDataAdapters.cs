using egeszsegkalauz.data.Models;
using egeszsegkalauz.integrations.Common;
using egeszsegkalauz.integrations.Garmin;

namespace egeszsegkalauz.services.Services.Integrations;

public class HealthDataAdapters
{
    private readonly IGarminAdapterService garmin;

    public HealthDataAdapters(IGarminAdapterService garmin)
    {
        this.garmin = garmin;
    }

    public IHealthDataAdapter For(HealthDataSource source)
    {
        return source switch
        {
            HealthDataSource.Garmin => garmin,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };
    }

    public ISummaryImportAdapter SummariesFor(HealthDataSource source)
    {
        return source switch
        {
            HealthDataSource.Garmin => garmin,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };
    }

    public IDailyImportAdapter DailyFor(HealthDataSource source)
    {
        return source switch
        {
            HealthDataSource.Garmin => garmin,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };
    }
}
