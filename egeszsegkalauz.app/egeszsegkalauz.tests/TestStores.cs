using egeszsegkalauz.data;
using egeszsegkalauz.services.Services.HealthData;
using egeszsegkalauz.services.Services.Integrations.Garmin;
using egeszsegkalauz.services.Services.Integrations.Hevy;

namespace egeszsegkalauz.tests;

public static class TestStores
{
    public static HealthDataStore Create(AppDbContext db, TimeProvider time)
    {
        return new HealthDataStore(db, new SourceStores(new GarminStore(db), new HevyStore(db)), time);
    }
}
