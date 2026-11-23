using System.Globalization;
using egeszsegkalauz.data;
using egeszsegkalauz.integrations.Common;
using egeszsegkalauz.integrations.Hevy;
using egeszsegkalauz.services.Services.HealthData;
using Microsoft.EntityFrameworkCore;
using Data = egeszsegkalauz.data.Models.Hevy;

namespace egeszsegkalauz.services.Services.Integrations.Hevy;

// Copies Hevy's payloads field by field into the hevy schema: same names and units as Hevy.
public class HevyStore : ISourceStore
{
    private readonly AppDbContext db;

    public HevyStore(AppDbContext db)
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
                case HevyAdapterService.WorkoutEndpoint:
                    await StoreWorkoutsAsync(userId, items, cancellationToken);
                    break;
                case HevyAdapterService.ExerciseTemplateEndpoint:
                    await StoreExerciseTemplatesAsync(userId, items, cancellationToken);
                    break;
                case HevyAdapterService.BodyMeasurementEndpoint:
                    await StoreBodyMeasurementsAsync(userId, items, cancellationToken);
                    break;
            }
        }
    }

    public async Task DeleteAsync(int userId, IReadOnlyList<RawRecordKey> keys, CancellationToken cancellationToken)
    {
        var workoutIds = KeysOf(keys, HevyAdapterService.WorkoutEndpoint);
        if (workoutIds.Count > 0)
            await db.HevyWorkouts.Where(x => x.UserId == userId && workoutIds.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);

        var templateIds = KeysOf(keys, HevyAdapterService.ExerciseTemplateEndpoint);
        if (templateIds.Count > 0)
            await db.HevyExerciseTemplates.Where(x => x.UserId == userId && templateIds.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);

        var measurementDates = KeysOf(keys, HevyAdapterService.BodyMeasurementEndpoint)
            .Select(x => DateOnly.TryParse(x, CultureInfo.InvariantCulture, out var date) ? date : (DateOnly?)null)
            .OfType<DateOnly>()
            .ToList();
        if (measurementDates.Count > 0)
            await db.HevyBodyMeasurements.Where(x => x.UserId == userId && measurementDates.Contains(x.Date)).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(int userId, CancellationToken cancellationToken)
    {
        await db.HevyWorkouts.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.HevyExerciseTemplates.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.HevyBodyMeasurements.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task StoreWorkoutsAsync(int userId, List<RawRecord> records, CancellationToken cancellationToken)
    {
        var workouts = records.Select(x => HevyJson.Read<HevyWorkout>(x.Json))
            .OfType<HevyWorkout>()
            .Where(x => x.Id is not null)
            .DistinctBy(x => x.Id)
            .ToList();
        var ids = workouts.Select(x => x.Id ?? "").ToList();
        await db.HevyWorkouts.Where(x => x.UserId == userId && ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);

        foreach (var workout in workouts)
        {
            var id = workout.Id ?? "";
            db.HevyWorkouts.Add(new Data.Workout
            {
                UserId = userId,
                Id = id,
                Title = workout.Title,
                RoutineId = workout.RoutineId,
                Description = workout.Description,
                StartTime = workout.StartTime?.UtcDateTime,
                EndTime = workout.EndTime?.UtcDateTime,
                UpdatedAt = workout.UpdatedAt?.UtcDateTime,
                CreatedAt = workout.CreatedAt?.UtcDateTime,
                Exercises = (workout.Exercises ?? [])
                    .Select((exercise, position) => (Index: (int?)exercise.Index ?? position, Exercise: exercise))
                    .DistinctBy(x => x.Index)
                    .Select(x => new Data.WorkoutExercise
                    {
                        UserId = userId,
                        WorkoutId = id,
                        Index = x.Index,
                        Title = x.Exercise.Title,
                        Notes = x.Exercise.Notes,
                        ExerciseTemplateId = x.Exercise.ExerciseTemplateId,
                        SupersetId = x.Exercise.SupersetId,
                        Sets = (x.Exercise.Sets ?? [])
                            .Select((set, setPosition) => (Index: (int?)set.Index ?? setPosition, Set: set))
                            .DistinctBy(s => s.Index)
                            .Select(s => new Data.WorkoutSet
                            {
                                UserId = userId,
                                WorkoutId = id,
                                ExerciseIndex = x.Index,
                                Index = s.Index,
                                Type = s.Set.Type,
                                WeightKg = s.Set.WeightKg,
                                Reps = s.Set.Reps,
                                DistanceMeters = s.Set.DistanceMeters,
                                DurationSeconds = s.Set.DurationSeconds,
                                Rpe = s.Set.Rpe,
                                CustomMetric = s.Set.CustomMetric,
                            })
                            .ToList(),
                    })
                    .ToList(),
            });
        }
    }

    private async Task StoreExerciseTemplatesAsync(int userId, List<RawRecord> records, CancellationToken cancellationToken)
    {
        var templates = records.Select(x => HevyJson.Read<HevyExerciseTemplate>(x.Json))
            .OfType<HevyExerciseTemplate>()
            .Where(x => x.Id is not null)
            .DistinctBy(x => x.Id)
            .ToList();
        var ids = templates.Select(x => x.Id ?? "").ToList();
        await db.HevyExerciseTemplates.Where(x => x.UserId == userId && ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);

        db.HevyExerciseTemplates.AddRange(templates.Select(t => new Data.ExerciseTemplate
        {
            UserId = userId,
            Id = t.Id ?? "",
            Title = t.Title,
            Type = t.Type,
            PrimaryMuscleGroup = t.PrimaryMuscleGroup,
            SecondaryMuscleGroups = t.SecondaryMuscleGroups ?? [],
            Equipment = t.Equipment,
            IsCustom = t.IsCustom,
        }));
    }

    private async Task StoreBodyMeasurementsAsync(int userId, List<RawRecord> records, CancellationToken cancellationToken)
    {
        var measurements = records.Select(x => HevyJson.Read<HevyBodyMeasurement>(x.Json))
            .OfType<HevyBodyMeasurement>()
            .Where(x => x.Date is not null)
            .DistinctBy(x => x.Date)
            .ToList();
        var dates = measurements.Select(x => x.Date ?? default).ToList();
        await db.HevyBodyMeasurements.Where(x => x.UserId == userId && dates.Contains(x.Date)).ExecuteDeleteAsync(cancellationToken);

        db.HevyBodyMeasurements.AddRange(measurements.Select(m => new Data.BodyMeasurement
        {
            UserId = userId,
            Date = m.Date ?? default,
            WeightKg = m.WeightKg,
            LeanMassKg = m.LeanMassKg,
            FatPercent = m.FatPercent,
            NeckCm = m.NeckCm,
            ShoulderCm = m.ShoulderCm,
            ChestCm = m.ChestCm,
            LeftBicepCm = m.LeftBicepCm,
            RightBicepCm = m.RightBicepCm,
            LeftForearmCm = m.LeftForearmCm,
            RightForearmCm = m.RightForearmCm,
            Abdomen = m.Abdomen,
            Waist = m.Waist,
            Hips = m.Hips,
            LeftThigh = m.LeftThigh,
            RightThigh = m.RightThigh,
            LeftCalf = m.LeftCalf,
            RightCalf = m.RightCalf,
            PictureUrl = m.PictureUrl,
        }));
    }

    private static List<string> KeysOf(IReadOnlyList<RawRecordKey> keys, string endpoint) =>
        keys.Where(x => x.Endpoint == endpoint).Select(x => x.Key).ToList();
}
