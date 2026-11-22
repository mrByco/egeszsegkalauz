using egeszsegkalauz.api.Auth;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Services.Roles;
using Microsoft.AspNetCore.Mvc;

namespace egeszsegkalauz.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class RolesController : ControllerBase
{
    private readonly IRoleService roleService;

    public RolesController(IRoleService roleService)
    {
        this.roleService = roleService;
    }

    [HttpGet]
    [HasPermission(Permission.RolesView)]
    [ProducesResponseType<List<RoleDto>>(StatusCodes.Status200OK)]
    public async Task<List<RoleDto>> List(CancellationToken cancellationToken)
    {
        return await roleService.ListAsync(cancellationToken);
    }

    [HttpGet("{nanoid}")]
    [HasPermission(Permission.RolesView)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<RoleDto>(StatusCodes.Status200OK)]
    public async Task<RoleDto> Get(string nanoid, CancellationToken cancellationToken)
    {
        return await roleService.GetAsync(nanoid, cancellationToken);
    }

    [HttpPost]
    [HasPermission(Permission.RolesEdit)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<RoleDto>(StatusCodes.Status200OK)]
    public async Task<RoleDto> Create(SaveRoleRequest request, CancellationToken cancellationToken)
    {
        return await roleService.CreateAsync(request, cancellationToken);
    }

    [HttpPut("{nanoid}")]
    [HasPermission(Permission.RolesEdit)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<RoleDto>(StatusCodes.Status200OK)]
    public async Task<RoleDto> Update(string nanoid, SaveRoleRequest request, CancellationToken cancellationToken)
    {
        return await roleService.UpdateAsync(nanoid, request, cancellationToken);
    }

    [HttpDelete("{nanoid}")]
    [HasPermission(Permission.RolesEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContentResult> Delete(string nanoid, CancellationToken cancellationToken)
    {
        await roleService.DeleteAsync(nanoid, cancellationToken);
        return NoContent();
    }
}
