using egeszsegkalauz.data.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.data;

public partial class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserIntegration> UserIntegrations => Set<UserIntegration>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<RawHealthRecord> RawHealthRecords => Set<RawHealthRecord>();
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
            integration.HasIndex(x => new { x.Source, x.ExternalUserId });
            integration.HasOne(x => x.User).WithMany().OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<RefreshToken>(token =>
        {
            token.HasIndex(x => x.TokenHash).IsUnique();
            token.HasOne(x => x.User).WithMany().OnDelete(DeleteBehavior.Cascade);
        });

        ConfigureGarmin(modelBuilder);
        ConfigureHevy(modelBuilder);
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
