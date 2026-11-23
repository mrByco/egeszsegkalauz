using egeszsegkalauz.data;
using egeszsegkalauz.services.DTO;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.Garmin;

public class GarminDataService : IGarminDataService
{
    private readonly AppDbContext db;
    private readonly TimeProvider timeProvider;

    public GarminDataService(AppDbContext db, TimeProvider timeProvider)
    {
        this.db = db;
        this.timeProvider = timeProvider;
    }

    public async Task<List<GarminHeartRateDayDto>> ListHeartRateDaysAsync(string userNanoid, int days,
        CancellationToken cancellationToken)
    {
        var from = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddDays(1 - days);
        var userId = await db.Users.Where(x => x.Nanoid == userNanoid).Select(x => x.Id).SingleAsync(cancellationToken);
        return await db.GarminHeartRateDays
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.CalendarDate >= from)
            .OrderByDescending(x => x.CalendarDate)
            .Select(x => new GarminHeartRateDayDto
            {
                CalendarDate = x.CalendarDate,
                RestingHeartRate = x.RestingHeartRate,
                MinHeartRate = x.MinHeartRate,
                MaxHeartRate = x.MaxHeartRate,
                LastSevenDaysAvgRestingHeartRate = x.LastSevenDaysAvgRestingHeartRate,
                ValueCount = db.GarminHeartRateValues.Count(v =>
                    v.UserId == userId && v.CalendarDate == x.CalendarDate && v.HeartRate != null),
            })
            .ToListAsync(cancellationToken);
    }
}
