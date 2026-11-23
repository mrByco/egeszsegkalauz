using egeszsegkalauz.data.Models;
using egeszsegkalauz.data.Models.Garmin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace egeszsegkalauz.data;

public partial class AppDbContext
{
    private const string GarminSchema = "garmin";

    public DbSet<HeartRateDay> GarminHeartRateDays => Set<HeartRateDay>();
    public DbSet<HeartRateValue> GarminHeartRateValues => Set<HeartRateValue>();
    public DbSet<UserSummary> GarminUserSummaries => Set<UserSummary>();
    public DbSet<SleepSummary> GarminSleepSummaries => Set<SleepSummary>();
    public DbSet<BodyBatteryDay> GarminBodyBatteryDays => Set<BodyBatteryDay>();
    public DbSet<BodyBatteryValue> GarminBodyBatteryValues => Set<BodyBatteryValue>();
    public DbSet<Vo2Max> GarminVo2Max => Set<Vo2Max>();
    public DbSet<Weight> GarminWeights => Set<Weight>();
    public DbSet<Activity> GarminActivities => Set<Activity>();

    private static void ConfigureGarmin(ModelBuilder modelBuilder)
    {
        Buffer<HeartRateDay>(modelBuilder, GarminSchema, "HeartRateDays").HasKey(x => new { x.UserId, x.CalendarDate });
        Buffer<HeartRateValue>(modelBuilder, GarminSchema, "HeartRateValues", x => x.HasIndex(v => new { v.UserId, v.CalendarDate }))
            .HasKey(x => new { x.UserId, x.Timestamp });
        Buffer<UserSummary>(modelBuilder, GarminSchema, "UserSummaries").HasKey(x => new { x.UserId, x.CalendarDate });
        Buffer<SleepSummary>(modelBuilder, GarminSchema, "SleepSummaries").HasKey(x => new { x.UserId, x.CalendarDate });
        Buffer<BodyBatteryDay>(modelBuilder, GarminSchema, "BodyBatteryDays").HasKey(x => new { x.UserId, x.Date });
        Buffer<BodyBatteryValue>(modelBuilder, GarminSchema, "BodyBatteryValues", x => x.HasIndex(v => new { v.UserId, v.Date }))
            .HasKey(x => new { x.UserId, x.Timestamp });
        Buffer<Vo2Max>(modelBuilder, GarminSchema, "Vo2Max").HasKey(x => new { x.UserId, x.CalendarDate, x.Kind });
        Buffer<Weight>(modelBuilder, GarminSchema, "Weights").HasKey(x => new { x.UserId, x.TimestampGMT });
        Buffer<Activity>(modelBuilder, GarminSchema, "Activities", x => x.HasIndex(a => new { a.UserId, a.BeginTimestamp }))
            .HasKey(x => new { x.UserId, x.ActivityId });
    }

    // Source buffers mirror the source and are keyed by the source's own keys, so they have no Entity base.
    private static EntityTypeBuilder<T> Buffer<T>(ModelBuilder modelBuilder,
        string schema, string table, Action<EntityTypeBuilder<T>>? configure = null)
        where T : class
    {
        var entity = modelBuilder.Entity<T>();
        entity.ToTable(table, schema);
        entity.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade);
        configure?.Invoke(entity);
        return entity;
    }
}
