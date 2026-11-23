using egeszsegkalauz.data.Models;

namespace egeszsegkalauz.services.Services.Imports;

public class ImportPacer
{
    private readonly Dictionary<HealthDataSource, DateTime> nextAllowed = [];
    private readonly Lock gate = new();

    public TimeSpan WaitTime(HealthDataSource source, DateTime now)
    {
        lock (gate)
        {
            return nextAllowed.TryGetValue(source, out var allowed) && allowed > now ? allowed - now : TimeSpan.Zero;
        }
    }

    public void Hold(HealthDataSource source, DateTime until)
    {
        lock (gate)
        {
            if (!nextAllowed.TryGetValue(source, out var allowed) || allowed < until)
                nextAllowed[source] = until;
        }
    }
}
