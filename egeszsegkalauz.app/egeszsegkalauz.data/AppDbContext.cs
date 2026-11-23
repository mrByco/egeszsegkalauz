using System.Linq.Expressions;
using egeszsegkalauz.data.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.data;

public class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserIntegration> UserIntegrations => Set<UserIntegration>();
    public DbSet<DailyHeartRateSummary> DailyHeartRateSummaries => Set<DailyHeartRateSummary>();
    public DbSet<HeartRateSample> HeartRateSamples => Set<HeartRateSample>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<RawHealthRecord> RawHealthRecords => Set<RawHealthRecord>();
    public DbSet<DailyActivitySummary> DailyActivitySummaries => Set<DailyActivitySummary>();
    public DbSet<SleepSummary> SleepSummaries => Set<SleepSummary>();
    public DbSet<BodyBatteryDailySummary> BodyBatteryDailySummaries => Set<BodyBatteryDailySummary>();
    public DbSet<BodyBatterySample> BodyBatterySamples => Set<BodyBatterySample>();
    public DbSet<Vo2MaxMeasurement> Vo2MaxMeasurements => Set<Vo2MaxMeasurement>();
    public DbSet<BodyCompositionMeasurement> BodyCompositionMeasurements => Set<BodyCompositionMeasurement>();
    public DbSet<ExerciseActivity> ExerciseActivities => Set<ExerciseActivity>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<ImportQueueItem> ImportQueueItems => Set<ImportQueueItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(Entity).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType).HasIndex(nameof(Entity.Nanoid)).IsUnique();
        }

        modelBuilder.Entity<User>(user =>
        {
            user.Property(x => x.Email).HasColumnType("citext");
            user.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(role =>
        {
            role.Property(x => x.Name).HasColumnType("citext").HasMaxLength(100);
            role.HasIndex(x => x.Name).IsUnique();
            role.PrimitiveCollection(x => x.Permissions).ElementType(e => e.HasConversion<string>());
            role.HasMany(x => x.Users).WithMany(x => x.Roles).UsingEntity("UserRoles");
        });

        modelBuilder.Entity<UserIntegration>(integration =>
        {
            integration.Property(x => x.Source).HasConversion<string>();
            integration.HasIndex(x => new { x.UserId, x.Source }).IsUnique();
            integration.HasOne(x => x.User).WithMany().OnDelete(DeleteBehavior.Cascade);
        });

        ConfigureDaily<DailyHeartRateSummary>(modelBuilder, x => new { x.UserId, x.Source, x.Date });
        ConfigureDaily<DailyActivitySummary>(modelBuilder, x => new { x.UserId, x.Source, x.Date });
        ConfigureDaily<SleepSummary>(modelBuilder, x => new { x.UserId, x.Source, x.Date });
        ConfigureDaily<BodyBatteryDailySummary>(modelBuilder, x => new { x.UserId, x.Source, x.Date });
        ConfigureDaily<Vo2MaxMeasurement>(modelBuilder, x => new { x.UserId, x.Source, x.Date, x.Sport });
        ConfigureDaily<BodyCompositionMeasurement>(modelBuilder, x => new { x.UserId, x.Source, x.Timestamp });
        ConfigureDaily<ExerciseActivity>(modelBuilder, x => new { x.UserId, x.Source, x.SourceActivityId });
        modelBuilder.Entity<ExerciseActivity>().HasIndex(x => new { x.UserId, x.StartUtc });

        modelBuilder.Entity<BodyBatterySample>(sample =>
        {
            sample.HasKey(x => new { x.UserId, x.Source, x.Timestamp });
            sample.Property(x => x.Source).HasConversion<string>();
            sample.HasIndex(x => new { x.UserId, x.Source, x.Date });
            sample.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RawHealthRecord>(record =>
        {
            record.HasKey(x => new { x.UserId, x.Source, x.Endpoint, x.Key });
            record.Property(x => x.Source).HasConversion<string>();
            record.Property(x => x.Payload).HasColumnType("jsonb");
            record.HasIndex(x => new { x.UserId, x.Source, x.Endpoint, x.Date });
            record.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImportJob>(job =>
        {
            job.Property(x => x.Source).HasConversion<string>();
            job.Property(x => x.Status).HasConversion<string>();
            job.HasIndex(x => new { x.UserId, x.Source, x.Status });
            job.HasOne(x => x.User).WithMany().OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImportQueueItem>(item =>
        {
            item.Property(x => x.Status).HasConversion<string>();
            item.HasIndex(x => new { x.Status, x.NotBefore });
            item.HasIndex(x => new { x.JobId, x.Status });
            item.HasOne(x => x.Job).WithMany(x => x.Items).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HeartRateSample>(sample =>
        {
            sample.HasKey(x => new { x.UserId, x.Source, x.Timestamp });
            sample.Property(x => x.Source).HasConversion<string>();
            sample.HasIndex(x => new { x.UserId, x.Source, x.Date });
            sample.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(token =>
        {
            token.HasIndex(x => x.TokenHash).IsUnique();
            token.HasOne(x => x.User).WithMany().OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDaily<T>(ModelBuilder modelBuilder, Expression<Func<T, object?>> uniqueKey)
        where T : Entity
    {
        modelBuilder.Entity<T>(entity =>
        {
            entity.Property<HealthDataSource>("Source").HasConversion<string>();
            entity.HasIndex(uniqueKey).IsUnique();
            entity.HasOne(typeof(User), "User").WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade);
        });
    }

    // The parameterless overloads delegate to these, so overriding them too would stamp twice.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IHasTimestamps>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
}
