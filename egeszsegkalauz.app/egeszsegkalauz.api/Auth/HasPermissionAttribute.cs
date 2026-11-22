using egeszsegkalauz.data.Models;
using Microsoft.AspNetCore.Authorization;

namespace egeszsegkalauz.api.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute, IAuthorizationRequirementData
{
    public HasPermissionAttribute(Permission permission)
    {
        Permission = permission;
    }

    public Permission Permission { get; }

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        return [new PermissionRequirement { Permission = Permission }];
    }
}
