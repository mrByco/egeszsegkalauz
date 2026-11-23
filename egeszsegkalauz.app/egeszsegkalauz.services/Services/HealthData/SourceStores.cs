using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.Services.Integrations.Garmin;
using egeszsegkalauz.services.Services.Integrations.Hevy;

namespace egeszsegkalauz.services.Services.HealthData;

public class SourceStores
{
    private readonly GarminStore garmin;
    private readonly HevyStore hevy;

    public SourceStores(GarminStore garmin, HevyStore hevy)
    {
        this.garmin = garmin;
        this.hevy = hevy;
    }

    public ISourceStore For(HealthDataSource source)
    {
        return source switch
        {
            HealthDataSource.Garmin => garmin,
            HealthDataSource.Hevy => hevy,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };
    }
}
