using egeszsegkalauz.data.Dev;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.integrations.Common;
using egeszsegkalauz.integrations.Hevy;
using egeszsegkalauz.tests.Integrations;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.tests.Services;

public class HevyStoreTests
{
    private int userId;

    [SetUp]
    public async Task SeedAsync()
    {
        await PostgresFixture.ResetAsync();
        await using var db = PostgresFixture.CreateDbContext();
        await DevDataSeeder.SeedAsync(db);
        userId = await db.Users.Where(x => x.Email == DevDataSeeder.AdminEmail).Select(x => x.Id).SingleAsync();
    }

    [Test]
    public async Task AWorkoutIsCopiedWithItsExercisesAndSets()
    {
        await StoreAsync(new SyncChunk { Records = [Workout("w1")] });

        await using var db = PostgresFixture.CreateDbContext();
        var workout = await db.HevyWorkouts.Include(x => x.Exercises).ThenInclude(x => x.Sets).SingleAsync();
        Assert.That((workout.Id, workout.Title, workout.StartTime), Is.EqualTo(("w1", (string?)"Afternoon workout",
            (DateTime?)new DateTime(2026, 11, 18, 10, 42, 0, DateTimeKind.Utc))));
        var sets = workout.Exercises.Single().Sets.OrderBy(x => x.Index).ToList();
        Assert.That(sets.Select(x => (x.Type, x.WeightKg, x.Reps)), Is.EqualTo(new[] { ((string?)"warmup", (double?)40, (double?)10), ("normal", 80, 5) }));
        Assert.That(sets[1].Rpe, Is.EqualTo(8.5));
    }

    [Test]
    public async Task AnUpdatedWorkoutReplacesItsExercisesAndADeletedOneIsRemoved()
    {
        await StoreAsync(new SyncChunk { Records = [Workout("w1"), Workout("w2")] });

        await StoreAsync(new SyncChunk
        {
            Records = [Workout("w1", exercises: 2)],
            Deleted = [new RawRecordKey { Endpoint = HevyAdapterService.WorkoutEndpoint, Key = "w2" }],
        });

        await using var db = PostgresFixture.CreateDbContext();
        Assert.That(await db.HevyWorkouts.Select(x => x.Id).ToListAsync(), Is.EqualTo(new[] { "w1" }));
        Assert.That(await db.HevyWorkoutExercises.CountAsync(), Is.EqualTo(2));
        Assert.That(await db.HevyWorkoutSets.CountAsync(), Is.EqualTo(4));
        Assert.That(await db.RawHealthRecords.Select(x => x.Key).ToListAsync(), Is.EqualTo(new[] { "w1" }));
    }

    [Test]
    public async Task TemplatesAndBodyMeasurementsKeepHevysFields()
    {
        await StoreAsync(new SyncChunk
        {
            Records =
            [
                new RawRecord { Endpoint = HevyAdapterService.ExerciseTemplateEndpoint, Key = "T1", Json = HevySamples.Template },
                new RawRecord
                {
                    Endpoint = HevyAdapterService.BodyMeasurementEndpoint, Key = "2026-11-20", Date = new DateOnly(2026, 11, 20),
                    Json = HevySamples.BodyMeasurement,
                },
            ],
        });

        await using var db = PostgresFixture.CreateDbContext();
        var template = await db.HevyExerciseTemplates.SingleAsync();
        Assert.That((template.PrimaryMuscleGroup, template.SecondaryMuscleGroups), Is.EqualTo(((string?)"chest", new List<string> { "triceps", "shoulders" })));
        var measurement = await db.HevyBodyMeasurements.SingleAsync();
        Assert.That((measurement.WeightKg, measurement.FatPercent, measurement.Waist, measurement.LeftCalf),
            Is.EqualTo(((double?)71.2, (double?)17.5, (double?)80, (double?)38)));
    }

    private async Task StoreAsync(SyncChunk chunk)
    {
        await using var db = PostgresFixture.CreateDbContext();
        await TestStores.Create(db, TimeProvider.System).StoreChunkAsync(userId, HealthDataSource.Hevy, chunk, CancellationToken.None);
    }

    private static RawRecord Workout(string id, int exercises = 1) =>
        new() { Endpoint = HevyAdapterService.WorkoutEndpoint, Key = id, Json = HevySamples.Workout(id, exercises) };
}
