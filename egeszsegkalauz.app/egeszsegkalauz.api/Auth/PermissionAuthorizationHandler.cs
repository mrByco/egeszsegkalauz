using egeszsegkalauz.services.Services.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace egeszsegkalauz.api.Auth;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService permissionService;
    private readonly IHttpContextAccessor httpContextAccessor;

    public PermissionAuthorizationHandler(IPermissionService permissionService, IHttpContextAccessor httpContextAccessor)
    {
        this.permissionService = permissionService;
        this.httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userNanoid = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userNanoid is null)
            return;

        var cancellationToken = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var permissions = await permissionService.GetPermissionsAsync(userNanoid, cancellationToken);
        if (permissions.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}
