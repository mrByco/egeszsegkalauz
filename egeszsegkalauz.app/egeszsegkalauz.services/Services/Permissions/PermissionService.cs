using egeszsegkalauz.data;
using egeszsegkalauz.data.Models;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.Permissions;

public class PermissionService : IPermissionService
{
    private static readonly IReadOnlySet<Permission> AllPermissions = Enum.GetValues<Permission>().ToHashSet();

    private readonly AppDbContext db;
    private readonly Dictionary<string, IReadOnlySet<Permission>> cache = [];

    public PermissionService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<IReadOnlySet<Permission>> GetPermissionsAsync(string userNanoid, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(userNanoid, out var cached))
            return cached;

        var roles = await db.Users
            .Where(x => x.Nanoid == userNanoid)
            .SelectMany(x => x.Roles)
            .Select(x => new { x.IsSystem, x.Permissions })
            .ToListAsync(cancellationToken);

        var permissions = roles.Any(x => x.IsSystem)
            ? AllPermissions
            : roles.SelectMany(x => x.Permissions).ToHashSet();

        cache[userNanoid] = permissions;
        return permissions;
    }
}
