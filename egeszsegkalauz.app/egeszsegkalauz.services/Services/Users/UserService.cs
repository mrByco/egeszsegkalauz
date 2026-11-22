using System.Linq.Expressions;
using egeszsegkalauz.data;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.Users;

public class UserService : IUserService
{
    private static readonly Expression<Func<User, UserDto>> ToDto = user => new UserDto
    {
        Nanoid = user.Nanoid,
        Email = user.Email,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
        Roles = user.Roles
            .OrderByDescending(r => r.IsSystem)
            .ThenBy(r => r.Name)
            .Select(r => new RoleSummaryDto { Nanoid = r.Nanoid, Name = r.Name })
            .ToList(),
    };

    private readonly AppDbContext db;

    public UserService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<List<UserDto>> ListAsync(CancellationToken cancellationToken)
    {
        return await db.Users
            .AsNoTracking()
            .OrderBy(x => x.Email)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDto> SetRolesAsync(string userNanoid, SetUserRolesRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.Include(x => x.Roles).SingleOrDefaultAsync(x => x.Nanoid == userNanoid, cancellationToken)
                   ?? throw new NotFoundException($"User {userNanoid} does not exist.");

        var roleNanoids = request.RoleNanoids.Distinct().ToList();
        var roles = await db.Roles.Where(x => roleNanoids.Contains(x.Nanoid)).ToListAsync(cancellationToken);
        var missing = roleNanoids.Except(roles.Select(x => x.Nanoid)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException($"Role {string.Join(", ", missing)} does not exist.");

        var losesAdmin = user.Roles.Any(x => x.IsSystem) && !roles.Any(x => x.IsSystem);
        if (losesAdmin && !await db.Users.AnyAsync(x => x.Id != user.Id && x.Roles.Any(r => r.IsSystem), cancellationToken))
            throw new ConflictException("At least one user must keep the Admin role.");

        user.Roles.Clear();
        user.Roles.AddRange(roles);
        await db.SaveChangesAsync(cancellationToken);

        return await db.Users.AsNoTracking().Where(x => x.Id == user.Id).Select(ToDto).SingleAsync(cancellationToken);
    }

    public async Task<bool> GrantAdminIfNoneAsync(string email, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(x => x.Roles.Any(r => r.IsSystem), cancellationToken))
            return false;

        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null)
            return false;

        user.Roles.Add(await db.Roles.SingleAsync(x => x.IsSystem, cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
