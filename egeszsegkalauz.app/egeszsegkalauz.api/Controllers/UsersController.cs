using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace egeszsegkalauz.api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService userService;

    public UsersController(IUserService userService)
    {
        this.userService = userService;
    }

    [HttpGet]
    public async Task<List<UserDto>> List(CancellationToken cancellationToken)
    {
        return await userService.ListAsync(cancellationToken);
    }
}
