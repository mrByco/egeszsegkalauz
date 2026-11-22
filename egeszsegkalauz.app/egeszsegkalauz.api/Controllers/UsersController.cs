using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Services.Users;
using Microsoft.AspNetCore.Mvc;

namespace egeszsegkalauz.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public Task<List<UserDto>> List(CancellationToken cancellationToken) =>
        userService.ListAsync(cancellationToken);
}
