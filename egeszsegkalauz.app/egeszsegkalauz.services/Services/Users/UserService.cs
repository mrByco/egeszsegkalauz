using egeszsegkalauz.data;
using egeszsegkalauz.services.DTO;
using Microsoft.EntityFrameworkCore;

namespace egeszsegkalauz.services.Services.Users;

public class UserService(AppDbContext db) : IUserService
{
    public Task<List<UserDto>> ListAsync(CancellationToken cancellationToken) =>
        db.Users
            .AsNoTracking()
            .OrderBy(x => x.DisplayName)
            .Select(x => new UserDto(x.Nanoid, x.Email, x.DisplayName, x.CreatedAt))
            .ToListAsync(cancellationToken);
}
