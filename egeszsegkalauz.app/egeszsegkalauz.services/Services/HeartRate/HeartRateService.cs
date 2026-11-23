using egeszsegkalauz.data;
using egeszsegkalauz.services.DTO;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.HeartRate;

public class HeartRateService : IHeartRateService
{
    private readonly AppDbContext db;
    private readonly TimeProvider timeProvider;

    public HeartRateService(AppDbContext db, TimeProvider timeProvider)
    {
        this.db = db;
        this.timeProvider = timeProvider;
    }

    public async Task<List<DailyHeartRateDto>> ListDailyAsync(string userNanoid, int days, CancellationToken cancellationToken)
    {
        var from = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddDays(1 - days);
        return await db.DailyHeartRateSummaries
            .AsNoTracking()
            .Where(x => x.User.Nanoid == userNanoid && x.Date >= from)
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.Source)
            .Select(x => new DailyHeartRateDto
            {
                Date = x.Date,
                Source = x.Source,
                RestingBpm = x.RestingBpm,
                MinBpm = x.MinBpm,
                MaxBpm = x.MaxBpm,
                SampleCount = x.SampleCount,
            })
            .ToListAsync(cancellationToken);
    }
}
