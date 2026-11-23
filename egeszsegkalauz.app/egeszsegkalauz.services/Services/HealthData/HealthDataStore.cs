using egeszsegkalauz.data;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.integrations.Common;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.HealthData;

public class HealthDataStore
{
    private readonly AppDbContext db;
    private readonly TimeProvider timeProvider;

    public HealthDataStore(AppDbContext db, TimeProvider timeProvider)
    {
        this.db = db;
        this.timeProvider = timeProvider;
    }

    // Rows are replaced by deleting first (ExecuteDelete runs immediately), so each store is one transaction:
    // a failed save must not leave the old rows deleted.
    public async Task StoreDailyResultAsync(User user, HealthDataSource source, DailyQueryResult result,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await StoreRawAsync(user.Id, source, [result.Raw], cancellationToken);
        if (result.HeartRate is not null)
            await StoreHeartRateAsync(user, source, result.HeartRate, cancellationToken);
        if (result.Activity is not null)
            await StoreDailyActivityAsync(user, source, result.Activity, cancellationToken);
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task StoreSummaryChunkAsync(User user, HealthDataSource source, SummaryChunk chunk,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await StoreRawAsync(user.Id, source, chunk.Raw, cancellationToken);
        await StoreSleepAsync(user, source, chunk.Sleep, cancellationToken);
        await StoreBodyBatteryAsync(user, source, chunk.BodyBattery, cancellationToken);
        await StoreVo2MaxAsync(user, source, chunk.Vo2Max, cancellationToken);
        await StoreBodyCompositionAsync(user, source, chunk.BodyComposition, cancellationToken);
        await StoreActivitiesAsync(user, source, chunk.Activities, cancellationToken);
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task StoreRawAsync(int userId, HealthDataSource source, IReadOnlyCollection<RawRecord> records,
        CancellationToken cancellationToken)
    {
        var fetchedAt = timeProvider.GetUtcNow().UtcDateTime;
        var unique = records.DistinctBy(x => (x.Endpoint, x.Key)).ToList();
        foreach (var group in unique.GroupBy(x => x.Endpoint))
        {
            var keys = group.Select(x => x.Key).ToList();
            await db.RawHealthRecords
                .Where(x => x.UserId == userId && x.Source == source && x.Endpoint == group.Key && keys.Contains(x.Key))
                .ExecuteDeleteAsync(cancellationToken);
        }

        db.RawHealthRecords.AddRange(unique.Select(x => new RawHealthRecord
        {
            UserId = userId,
            Source = source,
            Endpoint = x.Endpoint,
            Key = x.Key,
            Date = x.Date,
            Payload = x.Json,
            FetchedAt = fetchedAt,
        }));
    }

    private async Task StoreHeartRateAsync(User user, HealthDataSource source, DailyHeartRate day,
        CancellationToken cancellationToken)
    {
        var timestamps = day.Readings.Select(x => x.TimestampUtc).ToList();
        await db.HeartRateSamples
            .Where(x => x.UserId == user.Id && x.Source == source && (x.Date == day.Date || timestamps.Contains(x.Timestamp)))
            .ExecuteDeleteAsync(cancellationToken);

        var samples = day.Readings
            .DistinctBy(x => x.TimestampUtc)
            .Select(x => new HeartRateSample
            {
                UserId = user.Id,
                Source = source,
                Date = day.Date,
                Timestamp = x.TimestampUtc,
                Bpm = (short)x.Bpm,
            })
            .ToList();
        db.HeartRateSamples.AddRange(samples);

        var summary = await db.DailyHeartRateSummaries
                          .SingleOrDefaultAsync(x => x.UserId == user.Id && x.Source == source && x.Date == day.Date,
                              cancellationToken)
                      ?? db.DailyHeartRateSummaries.Add(new DailyHeartRateSummary { User = user, Source = source, Date = day.Date }).Entity;
        summary.RestingBpm = day.RestingBpm;
        summary.MinBpm = day.MinBpm;
        summary.MaxBpm = day.MaxBpm;
        summary.SampleCount = samples.Count;
    }

    private async Task StoreDailyActivityAsync(User user, HealthDataSource source, DailyActivity day,
        CancellationToken cancellationToken)
    {
        var summary = await db.DailyActivitySummaries
                          .SingleOrDefaultAsync(x => x.UserId == user.Id && x.Source == source && x.Date == day.Date,
                              cancellationToken)
                      ?? db.DailyActivitySummaries.Add(new DailyActivitySummary { User = user, Source = source, Date = day.Date }).Entity;
        summary.Steps = day.Steps;
        summary.StepGoal = day.StepGoal;
        summary.DistanceMeters = day.DistanceMeters;
        summary.TotalKilocalories = day.TotalKilocalories;
        summary.ActiveKilocalories = day.ActiveKilocalories;
        summary.BmrKilocalories = day.BmrKilocalories;
        summary.FloorsAscended = day.FloorsAscended;
        summary.FloorsDescended = day.FloorsDescended;
        summary.ModerateIntensityMinutes = day.ModerateIntensityMinutes;
        summary.VigorousIntensityMinutes = day.VigorousIntensityMinutes;
        summary.HighlyActiveSeconds = day.HighlyActiveSeconds;
        summary.ActiveSeconds = day.ActiveSeconds;
        summary.SedentarySeconds = day.SedentarySeconds;
        summary.SleepingSeconds = day.SleepingSeconds;
        summary.RestingHeartRate = day.RestingHeartRate;
        summary.MinHeartRate = day.MinHeartRate;
        summary.MaxHeartRate = day.MaxHeartRate;
        summary.AverageStressLevel = day.AverageStressLevel;
        summary.MaxStressLevel = day.MaxStressLevel;
        summary.LowStressSeconds = day.LowStressSeconds;
        summary.MediumStressSeconds = day.MediumStressSeconds;
        summary.HighStressSeconds = day.HighStressSeconds;
        summary.RestStressSeconds = day.RestStressSeconds;
        summary.BodyBatteryHighest = day.BodyBatteryHighest;
        summary.BodyBatteryLowest = day.BodyBatteryLowest;
        summary.BodyBatteryCharged = day.BodyBatteryCharged;
        summary.BodyBatteryDrained = day.BodyBatteryDrained;
        summary.BodyBatteryAtWake = day.BodyBatteryAtWake;
        summary.AverageSpo2 = day.AverageSpo2;
        summary.LowestSpo2 = day.LowestSpo2;
        summary.AverageWakingRespiration = day.AverageWakingRespiration;
    }

    private async Task StoreSleepAsync(User user, HealthDataSource source, List<SleepSession> sessions,
        CancellationToken cancellationToken)
    {
        var dates = sessions.Select(x => x.Date).ToList();
        var existing = await db.SleepSummaries
            .Where(x => x.UserId == user.Id && x.Source == source && dates.Contains(x.Date))
            .ToDictionaryAsync(x => x.Date, cancellationToken);

        foreach (var session in sessions.DistinctBy(x => x.Date))
        {
            var sleep = existing.GetValueOrDefault(session.Date)
                        ?? db.SleepSummaries.Add(new SleepSummary { User = user, Source = source, Date = session.Date }).Entity;
            sleep.StartUtc = session.StartUtc;
            sleep.EndUtc = session.EndUtc;
            sleep.SleepSeconds = session.SleepSeconds;
            sleep.NapSeconds = session.NapSeconds;
            sleep.DeepSeconds = session.DeepSeconds;
            sleep.LightSeconds = session.LightSeconds;
            sleep.RemSeconds = session.RemSeconds;
            sleep.AwakeSeconds = session.AwakeSeconds;
            sleep.UnmeasurableSeconds = session.UnmeasurableSeconds;
            sleep.AwakeCount = session.AwakeCount;
            sleep.Score = session.Score;
            sleep.ScoreQualifier = session.ScoreQualifier;
            sleep.AverageSleepStress = session.AverageSleepStress;
            sleep.AverageRespiration = session.AverageRespiration;
            sleep.LowestRespiration = session.LowestRespiration;
            sleep.HighestRespiration = session.HighestRespiration;
        }
    }

    private async Task StoreBodyBatteryAsync(User user, HealthDataSource source, List<BodyBatteryDay> days,
        CancellationToken cancellationToken)
    {
        var dates = days.Select(x => x.Date).ToList();
        var readingTimestamps = days.SelectMany(x => x.Readings).Select(x => x.TimestampUtc).ToList();
        await db.BodyBatterySamples
            .Where(x => x.UserId == user.Id && x.Source == source
                        && (dates.Contains(x.Date) || readingTimestamps.Contains(x.Timestamp)))
            .ExecuteDeleteAsync(cancellationToken);
        var existing = await db.BodyBatteryDailySummaries
            .Where(x => x.UserId == user.Id && x.Source == source && dates.Contains(x.Date))
            .ToDictionaryAsync(x => x.Date, cancellationToken);

        var timestamps = new HashSet<DateTime>();
        foreach (var day in days.DistinctBy(x => x.Date))
        {
            var summary = existing.GetValueOrDefault(day.Date)
                          ?? db.BodyBatteryDailySummaries.Add(new BodyBatteryDailySummary { User = user, Source = source, Date = day.Date }).Entity;
            var samples = day.Readings
                .Where(x => timestamps.Add(x.TimestampUtc))
                .Select(x => new BodyBatterySample
                {
                    UserId = user.Id, Source = source, Date = day.Date, Timestamp = x.TimestampUtc, Level = (short)x.Level,
                })
                .ToList();
            db.BodyBatterySamples.AddRange(samples);
            summary.Charged = day.Charged;
            summary.Drained = day.Drained;
            summary.SampleCount = samples.Count;
        }
    }

    private async Task StoreVo2MaxAsync(User user, HealthDataSource source, List<Vo2MaxReading> readings,
        CancellationToken cancellationToken)
    {
        var dates = readings.Select(x => x.Date).ToList();
        var existing = await db.Vo2MaxMeasurements
            .Where(x => x.UserId == user.Id && x.Source == source && dates.Contains(x.Date))
            .ToDictionaryAsync(x => (x.Date, x.Sport), cancellationToken);

        foreach (var reading in readings.DistinctBy(x => (x.Date, x.Sport)))
        {
            var measurement = existing.GetValueOrDefault((reading.Date, reading.Sport))
                              ?? db.Vo2MaxMeasurements.Add(new Vo2MaxMeasurement
                              {
                                  User = user, Source = source, Date = reading.Date, Sport = reading.Sport,
                              }).Entity;
            measurement.Value = reading.Value;
        }
    }

    private async Task StoreBodyCompositionAsync(User user, HealthDataSource source, List<BodyCompositionReading> readings,
        CancellationToken cancellationToken)
    {
        var timestamps = readings.Select(x => x.TimestampUtc).ToList();
        var existing = await db.BodyCompositionMeasurements
            .Where(x => x.UserId == user.Id && x.Source == source && timestamps.Contains(x.Timestamp))
            .ToDictionaryAsync(x => x.Timestamp, cancellationToken);

        foreach (var reading in readings.DistinctBy(x => x.TimestampUtc))
        {
            var measurement = existing.GetValueOrDefault(reading.TimestampUtc)
                              ?? db.BodyCompositionMeasurements.Add(new BodyCompositionMeasurement
                              {
                                  User = user, Source = source, Timestamp = reading.TimestampUtc,
                              }).Entity;
            measurement.Date = reading.Date;
            measurement.WeightKg = reading.WeightKg;
            measurement.Bmi = reading.Bmi;
            measurement.BodyFatPercent = reading.BodyFatPercent;
            measurement.BodyWaterPercent = reading.BodyWaterPercent;
            measurement.BoneMassKg = reading.BoneMassKg;
            measurement.MuscleMassKg = reading.MuscleMassKg;
            measurement.VisceralFat = reading.VisceralFat;
            measurement.MetabolicAge = reading.MetabolicAge;
        }
    }

    private async Task StoreActivitiesAsync(User user, HealthDataSource source, List<ActivityRecord> activities,
        CancellationToken cancellationToken)
    {
        var ids = activities.Select(x => x.SourceActivityId).ToList();
        var existing = await db.ExerciseActivities
            .Where(x => x.UserId == user.Id && x.Source == source && ids.Contains(x.SourceActivityId))
            .ToDictionaryAsync(x => x.SourceActivityId, cancellationToken);

        foreach (var record in activities.DistinctBy(x => x.SourceActivityId))
        {
            var activity = existing.GetValueOrDefault(record.SourceActivityId)
                           ?? db.ExerciseActivities.Add(new ExerciseActivity
                           {
                               User = user, Source = source, SourceActivityId = record.SourceActivityId,
                           }).Entity;
            activity.Name = record.Name;
            activity.Type = record.Type;
            activity.StartUtc = record.StartUtc;
            activity.DurationSeconds = record.DurationSeconds;
            activity.MovingSeconds = record.MovingSeconds;
            activity.DistanceMeters = record.DistanceMeters;
            activity.Calories = record.Calories;
            activity.AverageHeartRate = record.AverageHeartRate;
            activity.MaxHeartRate = record.MaxHeartRate;
            activity.ElevationGain = record.ElevationGain;
            activity.ElevationLoss = record.ElevationLoss;
            activity.AverageSpeed = record.AverageSpeed;
            activity.MaxSpeed = record.MaxSpeed;
            activity.Steps = record.Steps;
            activity.LocationName = record.LocationName;
            activity.HeartRateZone1Seconds = record.HeartRateZone1Seconds;
            activity.HeartRateZone2Seconds = record.HeartRateZone2Seconds;
            activity.HeartRateZone3Seconds = record.HeartRateZone3Seconds;
            activity.HeartRateZone4Seconds = record.HeartRateZone4Seconds;
            activity.HeartRateZone5Seconds = record.HeartRateZone5Seconds;
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);

        // Imports write hundreds of thousands of rows through one context; keep only the bookkeeping rows tracked.
        foreach (var entry in db.ChangeTracker.Entries()
                     .Where(x => x.Entity is not (User or UserIntegration or ImportJob or ImportQueueItem)).ToList())
            entry.State = EntityState.Detached;
    }
}
