using egeszsegkalauz.data.Models;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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

        modelBuilder.Entity<RefreshToken>(token =>
        {
            token.HasIndex(x => x.TokenHash).IsUnique();
            token.HasOne(x => x.User).WithMany().OnDelete(DeleteBehavior.Cascade);
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
