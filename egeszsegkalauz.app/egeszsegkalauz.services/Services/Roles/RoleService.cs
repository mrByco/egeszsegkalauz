using egeszsegkalauz.data;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace egeszsegkalauz.services.Services.Roles;

public class RoleService : IRoleService
{
    private readonly AppDbContext db;

    public RoleService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<List<RoleDto>> ListAsync(CancellationToken cancellationToken)
    {
        var roles = await db.Roles
            .AsNoTracking()
            .OrderByDescending(x => x.IsSystem)
            .ThenBy(x => x.Name)
            .Select(x => new { Role = x, UserCount = x.Users.Count })
            .ToListAsync(cancellationToken);

        return roles.Select(x => ToDto(x.Role, x.UserCount)).ToList();
    }

    public async Task<RoleDto> GetAsync(string nanoid, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .AsNoTracking()
            .Where(x => x.Nanoid == nanoid)
            .Select(x => new { Role = x, UserCount = x.Users.Count })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw RoleNotFound(nanoid);

        return ToDto(role.Role, role.UserCount);
    }

    public async Task<RoleDto> CreateAsync(SaveRoleRequest request, CancellationToken cancellationToken)
    {
        var role = new Role { Name = request.Name.Trim(), Permissions = Normalize(request.Permissions) };
        db.Roles.Add(role);
        await SaveAsync(role.Name, cancellationToken);
        return ToDto(role, 0);
    }

    public async Task<RoleDto> UpdateAsync(string nanoid, SaveRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await FindEditableAsync(nanoid, cancellationToken);
        role.Name = request.Name.Trim();
        role.Permissions = Normalize(request.Permissions);
        await SaveAsync(role.Name, cancellationToken);
        return await GetAsync(nanoid, cancellationToken);
    }

    public async Task DeleteAsync(string nanoid, CancellationToken cancellationToken)
    {
        var role = await FindEditableAsync(nanoid, cancellationToken);
        db.Roles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Role> FindEditableAsync(string nanoid, CancellationToken cancellationToken)
    {
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Nanoid == nanoid, cancellationToken)
                   ?? throw RoleNotFound(nanoid);
        if (role.IsSystem)
            throw new ConflictException($"The {role.Name} role is built in and cannot be changed.");
        return role;
    }

    private async Task SaveAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException($"A role named {name} already exists.");
        }
    }

    private static List<Permission> Normalize(IEnumerable<Permission> permissions) =>
        permissions.Distinct().Order().ToList();

    private static NotFoundException RoleNotFound(string nanoid) => new($"Role {nanoid} does not exist.");

    private static RoleDto ToDto(Role role, int userCount)
    {
        return new RoleDto
        {
            Nanoid = role.Nanoid,
            Name = role.Name,
            IsSystem = role.IsSystem,
            Permissions = role.IsSystem ? Enum.GetValues<Permission>().ToList() : role.Permissions,
            UserCount = userCount,
        };
    }
}
