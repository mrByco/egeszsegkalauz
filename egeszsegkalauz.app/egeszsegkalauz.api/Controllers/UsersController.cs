using egeszsegkalauz.api.Auth;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Services.Users;
using Microsoft.AspNetCore.Mvc;

namespace egeszsegkalauz.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class UsersController : ControllerBase
{
    private readonly IUserService userService;

    public UsersController(IUserService userService)
    {
        this.userService = userService;
    }

    [HttpGet]
    [HasPermission(Permission.UsersView)]
    [ProducesResponseType<List<UserDto>>(StatusCodes.Status200OK)]
    public async Task<List<UserDto>> List(CancellationToken cancellationToken)
    {
        return await userService.ListAsync(cancellationToken);
    }

    [HttpPut("{nanoid}/roles")]
    [HasPermission(Permission.UsersAssignRoles)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<UserDto> SetRoles(string nanoid, SetUserRolesRequest request, CancellationToken cancellationToken)
    {
        return await userService.SetRolesAsync(nanoid, request, cancellationToken);
    }
}
