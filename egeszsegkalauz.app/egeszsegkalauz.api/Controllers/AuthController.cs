using egeszsegkalauz.api.Auth;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace egeszsegkalauz.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService authService;

    public AuthController(IAuthService authService)
    {
        this.authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CurrentUserDto>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request.Email, request.Password, cancellationToken);
        if (result is null)
            return Conflict();

        AuthCookies.Append(Response, result.Tokens);
        return result.User;
    }

    [HttpPost("login")]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserDto>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        if (result is null)
            return Unauthorized();

        AuthCookies.Append(Response, result.Tokens);
        return result.User;
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<NoContentResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = AuthCookies.ReadRefreshToken(Request);
        if (refreshToken is not null)
            await authService.LogoutAsync(refreshToken, cancellationToken);

        AuthCookies.Delete(Response);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var user = await authService.GetCurrentUserAsync(User.GetUserNanoid(), cancellationToken);
        if (user is null)
        {
            AuthCookies.Delete(Response);
            return Unauthorized();
        }

        return user;
    }
}
