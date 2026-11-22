using egeszsegkalauz.data.Models;
using Microsoft.AspNetCore.Authorization;

namespace egeszsegkalauz.api.Auth;

public class PermissionRequirement : IAuthorizationRequirement
{
    public required Permission Permission { get; init; }
}
