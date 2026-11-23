using egeszsegkalauz.data.Models.Hevy;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.data;

public partial class AppDbContext
{
    private const string HevySchema = "hevy";

    public DbSet<Workout> HevyWorkouts => Set<Workout>();
    public DbSet<WorkoutExercise> HevyWorkoutExercises => Set<WorkoutExercise>();
    public DbSet<WorkoutSet> HevyWorkoutSets => Set<WorkoutSet>();
    public DbSet<ExerciseTemplate> HevyExerciseTemplates => Set<ExerciseTemplate>();
    public DbSet<BodyMeasurement> HevyBodyMeasurements => Set<BodyMeasurement>();

    private static void ConfigureHevy(ModelBuilder modelBuilder)
    {
        Buffer<Workout>(modelBuilder, HevySchema, "Workouts", x => x.HasIndex(w => new { w.UserId, w.StartTime }))
            .HasKey(x => new { x.UserId, x.Id });
        Buffer<ExerciseTemplate>(modelBuilder, HevySchema, "ExerciseTemplates").HasKey(x => new { x.UserId, x.Id });
        Buffer<BodyMeasurement>(modelBuilder, HevySchema, "BodyMeasurements").HasKey(x => new { x.UserId, x.Date });

        modelBuilder.Entity<WorkoutExercise>(exercise =>
        {
            exercise.ToTable("WorkoutExercises", HevySchema);
            exercise.HasKey(x => new { x.UserId, x.WorkoutId, x.Index });
        });
        modelBuilder.Entity<Workout>()
            .HasMany(x => x.Exercises).WithOne()
            .HasForeignKey(x => new { x.UserId, x.WorkoutId })
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkoutSet>(set =>
        {
            set.ToTable("WorkoutSets", HevySchema);
            set.HasKey(x => new { x.UserId, x.WorkoutId, x.ExerciseIndex, x.Index });
        });
        modelBuilder.Entity<WorkoutExercise>()
            .HasMany(x => x.Sets).WithOne()
            .HasForeignKey(x => new { x.UserId, x.WorkoutId, x.ExerciseIndex })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
