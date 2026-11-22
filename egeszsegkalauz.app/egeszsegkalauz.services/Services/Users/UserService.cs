using egeszsegkalauz.data;
using egeszsegkalauz.services.DTO;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.Users;

public class UserService : IUserService
{
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
            .Select(x => new UserDto
            {
                Nanoid = x.Nanoid,
                Email = x.Email,
                CreatedAt = x.CreatedAt,
                LastLoginAt = x.LastLoginAt,
            })
            .ToListAsync(cancellationToken);
    }
}
