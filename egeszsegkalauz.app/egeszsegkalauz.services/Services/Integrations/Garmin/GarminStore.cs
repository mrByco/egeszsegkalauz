using egeszsegkalauz.data;
using egeszsegkalauz.integrations.Common;
using egeszsegkalauz.integrations.Garmin;
using egeszsegkalauz.services.Services.HealthData;
using Microsoft.EntityFrameworkCore;
using Data = egeszsegkalauz.data.Models.Garmin;

namespace egeszsegkalauz.services.Services.Integrations.Garmin;

// Copies Garmin's payloads field by field into the garmin schema: same names, units and timestamps as Garmin.
public class GarminStore : ISourceStore
{
    private readonly AppDbContext db;

    public GarminStore(AppDbContext db)
    {
        this.db = db;
    }

    public async Task ApplyAsync(int userId, IReadOnlyList<RawRecord> records, CancellationToken cancellationToken)
    {
        foreach (var group in records.GroupBy(x => x.Endpoint))
        {
            var items = group.ToList();
            switch (group.Key)
            {
                case GarminAdapterService.HeartRateQuery:
                    await StoreHeartRateAsync(userId, items, cancellationToken);
                    break;
                case GarminAdapterService.UserSummaryQuery:
                    await StoreUserSummariesAsync(userId, items, cancellationToken);
                    break;
                case GarminAdapterService.SleepSummariesEndpoint:
                    await StoreSleepSummariesAsync(userId, items, cancellationToken);
                    break;
                case GarminAdapterService.BodyBatteryEndpoint:
                    await StoreBodyBatteryAsync(userId, items, cancellationToken);
                    break;
                case GarminAdapterService.Vo2MaxEndpoint:
                    await StoreVo2MaxAsync(userId, items, cancellationToken);
                    break;
                case GarminAdapterService.WeightEndpoint:
                    await StoreWeightsAsync(userId, items, cancellationToken);
                    break;
                case GarminAdapterService.ActivityEndpoint:
                    await StoreActivitiesAsync(userId, items, cancellationToken);
                    break;
            }
        }
    }

    public async Task DeleteAsync(int userId, IReadOnlyList<RawRecordKey> keys, CancellationToken cancellationToken)
    {
        var activityIds = keys.Where(x => x.Endpoint == GarminAdapterService.ActivityEndpoint)
            .Select(x => long.TryParse(x.Key, out var id) ? id : (long?)null)
            .OfType<long>()
            .ToList();
        if (activityIds.Count > 0)
            await db.GarminActivities.Where(x => x.UserId == userId && activityIds.Contains(x.ActivityId)).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(int userId, CancellationToken cancellationToken)
    {
        await db.GarminHeartRateDays.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.GarminHeartRateValues.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.GarminUserSummaries.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.GarminSleepSummaries.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.GarminBodyBatteryDays.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.GarminBodyBatteryValues.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.GarminVo2Max.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.GarminWeights.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.GarminActivities.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task StoreHeartRateAsync(int userId, List<RawRecord> records, CancellationToken cancellationToken)
    {
        var days = new List<Data.HeartRateDay>();
        var values = new Dictionary<long, Data.HeartRateValue>();
        foreach (var record in records.DistinctBy(x => x.Key))
        {
            if (GarminJson.Read<GarminHeartRateDay>(record.Json) is not { } day || (day.CalendarDate ?? record.Date) is not { } date)
                continue;

            days.Add(new Data.HeartRateDay
            {
                UserId = userId,
                CalendarDate = date,
                StartTimestampGMT = day.StartTimestampGMT,
                EndTimestampGMT = day.EndTimestampGMT,
                StartTimestampLocal = day.StartTimestampLocal,
                EndTimestampLocal = day.EndTimestampLocal,
                MaxHeartRate = day.MaxHeartRate,
                MinHeartRate = day.MinHeartRate,
                RestingHeartRate = day.RestingHeartRate,
                LastSevenDaysAvgRestingHeartRate = day.LastSevenDaysAvgRestingHeartRate,
            });
            foreach (var value in day.HeartRateValues ?? [])
            {
                if (value is [{ } timestamp, var heartRate])
                    values.TryAdd((long)timestamp, new Data.HeartRateValue
                    {
                        UserId = userId, Timestamp = (long)timestamp, CalendarDate = date, HeartRate = heartRate,
                    });
            }
        }

        // A day that now comes back empty ("null") clears what was buffered for it.
        var dates = records.Select(x => x.Date).OfType<DateOnly>().Concat(days.Select(x => x.CalendarDate)).Distinct().ToList();
        var timestamps = values.Keys.ToList();
        await db.GarminHeartRateDays.Where(x => x.UserId == userId && dates.Contains(x.CalendarDate)).ExecuteDeleteAsync(cancellationToken);
        await db.GarminHeartRateValues
            .Where(x => x.UserId == userId && (dates.Contains(x.CalendarDate) || timestamps.Contains(x.Timestamp)))
            .ExecuteDeleteAsync(cancellationToken);

        db.GarminHeartRateDays.AddRange(days.DistinctBy(x => x.CalendarDate));
        db.GarminHeartRateValues.AddRange(values.Values);
    }

    private async Task StoreUserSummariesAsync(int userId, List<RawRecord> records, CancellationToken cancellationToken)
    {
        // A day that now comes back empty ("null") clears what was buffered for it.
        var dates = records.Select(x => x.Date).OfType<DateOnly>().ToList();
        await db.GarminUserSummaries.Where(x => x.UserId == userId && dates.Contains(x.CalendarDate)).ExecuteDeleteAsync(cancellationToken);

        foreach (var record in records.DistinctBy(x => x.Key))
        {
            if (GarminJson.Read<GarminUserSummary>(record.Json) is not { } s || (s.CalendarDate ?? record.Date) is not { } date)
                continue;
            db.GarminUserSummaries.Add(new Data.UserSummary
            {
                UserId = userId,
                CalendarDate = date,
                TotalKilocalories = s.TotalKilocalories,
                ActiveKilocalories = s.ActiveKilocalories,
                BmrKilocalories = s.BmrKilocalories,
                TotalSteps = s.TotalSteps,
                DailyStepGoal = s.DailyStepGoal,
                TotalDistanceMeters = s.TotalDistanceMeters,
                WellnessDistanceMeters = s.WellnessDistanceMeters,
                WellnessStartTimeGmt = s.WellnessStartTimeGmt,
                WellnessEndTimeGmt = s.WellnessEndTimeGmt,
                HighlyActiveSeconds = s.HighlyActiveSeconds,
                ActiveSeconds = s.ActiveSeconds,
                SedentarySeconds = s.SedentarySeconds,
                SleepingSeconds = s.SleepingSeconds,
                ModerateIntensityMinutes = s.ModerateIntensityMinutes,
                VigorousIntensityMinutes = s.VigorousIntensityMinutes,
                IntensityMinutesGoal = s.IntensityMinutesGoal,
                FloorsAscended = s.FloorsAscended,
                FloorsDescended = s.FloorsDescended,
                MinHeartRate = s.MinHeartRate,
                MaxHeartRate = s.MaxHeartRate,
                RestingHeartRate = s.RestingHeartRate,
                LastSevenDaysAvgRestingHeartRate = s.LastSevenDaysAvgRestingHeartRate,
                MinAvgHeartRate = s.MinAvgHeartRate,
                MaxAvgHeartRate = s.MaxAvgHeartRate,
                AverageStressLevel = s.AverageStressLevel,
                MaxStressLevel = s.MaxStressLevel,
                StressDuration = s.StressDuration,
                RestStressDuration = s.RestStressDuration,
                ActivityStressDuration = s.ActivityStressDuration,
                UncategorizedStressDuration = s.UncategorizedStressDuration,
                TotalStressDuration = s.TotalStressDuration,
                LowStressDuration = s.LowStressDuration,
                MediumStressDuration = s.MediumStressDuration,
                HighStressDuration = s.HighStressDuration,
                StressQualifier = s.StressQualifier,
                MeasurableAwakeDuration = s.MeasurableAwakeDuration,
                MeasurableAsleepDuration = s.MeasurableAsleepDuration,
                BodyBatteryChargedValue = s.BodyBatteryChargedValue,
                BodyBatteryDrainedValue = s.BodyBatteryDrainedValue,
                BodyBatteryHighestValue = s.BodyBatteryHighestValue,
                BodyBatteryLowestValue = s.BodyBatteryLowestValue,
                BodyBatteryMostRecentValue = s.BodyBatteryMostRecentValue,
                BodyBatteryDuringSleep = s.BodyBatteryDuringSleep,
                BodyBatteryAtWakeTime = s.BodyBatteryAtWakeTime,
                AverageSpo2 = s.AverageSpo2,
                LowestSpo2 = s.LowestSpo2,
                LatestSpo2 = s.LatestSpo2,
                AvgWakingRespirationValue = s.AvgWakingRespirationValue,
                HighestRespirationValue = s.HighestRespirationValue,
                LowestRespirationValue = s.LowestRespirationValue,
                LatestRespirationValue = s.LatestRespirationValue,
            });
        }
    }

    private async Task StoreSleepSummariesAsync(int userId, List<RawRecord> records, CancellationToken cancellationToken)
    {
        var sleeps = records.Select(x => GarminJson.Read<GarminSleepSummary>(x.Json))
            .OfType<GarminSleepSummary>()
            .Where(x => x.CalendarDate is not null)
            .DistinctBy(x => x.CalendarDate)
            .ToList();
        var dates = sleeps.Select(x => x.CalendarDate ?? default).ToList();
        await db.GarminSleepSummaries.Where(x => x.UserId == userId && dates.Contains(x.CalendarDate)).ExecuteDeleteAsync(cancellationToken);

        db.GarminSleepSummaries.AddRange(sleeps.Select(s => new Data.SleepSummary
        {
            UserId = userId,
            CalendarDate = s.CalendarDate ?? default,
            SleepId = ToLong(s.Id),
            SleepTimeSeconds = s.SleepTimeSeconds,
            NapTimeSeconds = s.NapTimeSeconds,
            SleepWindowConfirmed = s.SleepWindowConfirmed,
            SleepStartTimestampGMT = ToLong(s.SleepStartTimestampGMT),
            SleepEndTimestampGMT = ToLong(s.SleepEndTimestampGMT),
            SleepStartTimestampLocal = ToLong(s.SleepStartTimestampLocal),
            SleepEndTimestampLocal = ToLong(s.SleepEndTimestampLocal),
            UnmeasurableSleepSeconds = s.UnmeasurableSleepSeconds,
            DeepSleepSeconds = s.DeepSleepSeconds,
            LightSleepSeconds = s.LightSleepSeconds,
            RemSleepSeconds = s.RemSleepSeconds,
            AwakeSleepSeconds = s.AwakeSleepSeconds,
            DeviceRemCapable = s.DeviceRemCapable,
            AverageRespirationValue = s.AverageRespirationValue,
            LowestRespirationValue = s.LowestRespirationValue,
            HighestRespirationValue = s.HighestRespirationValue,
            AwakeCount = s.AwakeCount,
            AvgSleepStress = s.AvgSleepStress,
            SleepScoreFeedback = s.SleepScoreFeedback,
            SleepScoreInsight = s.SleepScoreInsight,
            SleepScoresOverallValue = s.SleepScores?.Overall?.Value,
            SleepScoresOverallQualifierKey = s.SleepScores?.Overall?.QualifierKey,
        }));
    }

    private async Task StoreBodyBatteryAsync(int userId, List<RawRecord> records, CancellationToken cancellationToken)
    {
        var reports = records.Select(x => GarminJson.Read<GarminBodyBatteryReport>(x.Json))
            .OfType<GarminBodyBatteryReport>()
            .Where(x => x.Date is not null)
            .DistinctBy(x => x.Date)
            .ToList();
        var dates = reports.Select(x => x.Date ?? default).ToList();
        var values = reports
            .SelectMany(r => (r.BodyBatteryValuesArray ?? []).SelectMany(v => v is [{ } timestamp, var level]
                ? [new Data.BodyBatteryValue { UserId = userId, Timestamp = (long)timestamp, Date = r.Date ?? default, BodyBatteryLevel = level }]
                : Array.Empty<Data.BodyBatteryValue>()))
            .DistinctBy(x => x.Timestamp)
            .ToList();
        var timestamps = values.Select(x => x.Timestamp).ToList();

        await db.GarminBodyBatteryDays.Where(x => x.UserId == userId && dates.Contains(x.Date)).ExecuteDeleteAsync(cancellationToken);
        await db.GarminBodyBatteryValues
            .Where(x => x.UserId == userId && (dates.Contains(x.Date) || timestamps.Contains(x.Timestamp)))
            .ExecuteDeleteAsync(cancellationToken);

        db.GarminBodyBatteryDays.AddRange(reports.Select(r => new Data.BodyBatteryDay
        {
            UserId = userId,
            Date = r.Date ?? default,
            Charged = r.Charged,
            Drained = r.Drained,
            StartTimestampGMT = r.StartTimestampGMT,
            EndTimestampGMT = r.EndTimestampGMT,
            StartTimestampLocal = r.StartTimestampLocal,
            EndTimestampLocal = r.EndTimestampLocal,
        }));
        db.GarminBodyBatteryValues.AddRange(values);
    }

    private async Task StoreVo2MaxAsync(int userId, List<RawRecord> records, CancellationToken cancellationToken)
    {
        var rows = records.Select(x => GarminJson.Read<GarminVo2MaxDay>(x.Json))
            .OfType<GarminVo2MaxDay>()
            .SelectMany(day => new[] { ("generic", day.Generic), ("cycling", day.Cycling) })
            .Where(x => x.Item2?.CalendarDate is not null)
            .Select(x => new Data.Vo2Max
            {
                UserId = userId,
                CalendarDate = x.Item2?.CalendarDate ?? default,
                Kind = x.Item1,
                Sport = x.Item2?.Sport,
                SubSport = x.Item2?.SubSport,
                Vo2MaxPreciseValue = x.Item2?.Vo2MaxPreciseValue,
                Vo2MaxValue = x.Item2?.Vo2MaxValue,
            })
            .DistinctBy(x => (x.CalendarDate, x.Kind))
            .ToList();
        var dates = rows.Select(x => x.CalendarDate).Distinct().ToList();
        await db.GarminVo2Max.Where(x => x.UserId == userId && dates.Contains(x.CalendarDate)).ExecuteDeleteAsync(cancellationToken);
        db.GarminVo2Max.AddRange(rows);
    }

    private async Task StoreWeightsAsync(int userId, List<RawRecord> records, CancellationToken cancellationToken)
    {
        var weights = records.Select(x => GarminJson.Read<GarminWeight>(x.Json))
            .OfType<GarminWeight>()
            .Where(x => (x.TimestampGMT ?? x.Date) is not null)
            .Select(w => new Data.Weight
            {
                UserId = userId,
                TimestampGMT = ToLong(w.TimestampGMT ?? w.Date) ?? default,
                Date = ToLong(w.Date),
                CalendarDate = w.CalendarDate,
                WeightGrams = w.Weight,
                Bmi = w.Bmi,
                BodyFat = w.BodyFat,
                BodyWater = w.BodyWater,
                BoneMass = w.BoneMass,
                MuscleMass = w.MuscleMass,
                PhysiqueRating = w.PhysiqueRating,
                VisceralFat = w.VisceralFat,
                MetabolicAge = w.MetabolicAge,
                WeightDelta = w.WeightDelta,
            })
            .DistinctBy(x => x.TimestampGMT)
            .ToList();
        var timestamps = weights.Select(x => x.TimestampGMT).ToList();
        await db.GarminWeights.Where(x => x.UserId == userId && timestamps.Contains(x.TimestampGMT)).ExecuteDeleteAsync(cancellationToken);
        db.GarminWeights.AddRange(weights);
    }

    private async Task StoreActivitiesAsync(int userId, List<RawRecord> records, CancellationToken cancellationToken)
    {
        var activities = records.Select(x => GarminJson.Read<GarminActivity>(x.Json))
            .OfType<GarminActivity>()
            .DistinctBy(x => x.ActivityId)
            .Select(a => new Data.Activity
            {
                UserId = userId,
                ActivityId = a.ActivityId,
                ActivityName = a.ActivityName,
                ActivityTypeKey = a.ActivityType?.TypeKey,
                StartTimeLocal = a.StartTimeLocal,
                StartTimeGMT = a.StartTimeGMT,
                BeginTimestamp = ToLong(a.BeginTimestamp),
                Duration = a.Duration,
                ElapsedDuration = a.ElapsedDuration,
                MovingDuration = a.MovingDuration,
                Distance = a.Distance,
                Calories = a.Calories,
                BmrCalories = a.BmrCalories,
                AverageHR = a.AverageHR,
                MaxHR = a.MaxHR,
                ElevationGain = a.ElevationGain,
                ElevationLoss = a.ElevationLoss,
                AverageSpeed = a.AverageSpeed,
                MaxSpeed = a.MaxSpeed,
                Steps = a.Steps,
                LapCount = a.LapCount,
                StartLatitude = a.StartLatitude,
                StartLongitude = a.StartLongitude,
                LocationName = a.LocationName,
                Manufacturer = a.Manufacturer,
                HrTimeInZone1 = a.HrTimeInZone1,
                HrTimeInZone2 = a.HrTimeInZone2,
                HrTimeInZone3 = a.HrTimeInZone3,
                HrTimeInZone4 = a.HrTimeInZone4,
                HrTimeInZone5 = a.HrTimeInZone5,
            })
            .ToList();
        var ids = activities.Select(x => x.ActivityId).ToList();
        await db.GarminActivities.Where(x => x.UserId == userId && ids.Contains(x.ActivityId)).ExecuteDeleteAsync(cancellationToken);
        db.GarminActivities.AddRange(activities);
    }

    private static long? ToLong(double? value) => value is null ? null : (long)value.Value;
}
