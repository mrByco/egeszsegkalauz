using System.ComponentModel.DataAnnotations;
using egeszsegkalauz.api.Auth;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Services.HeartRate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace egeszsegkalauz.api.Controllers;

[ApiController]
[Authorize]
[Route("api/heart-rate")]
public class HeartRateController : ControllerBase
{
    private readonly IHeartRateService heartRateService;

    public HeartRateController(IHeartRateService heartRateService)
    {
        this.heartRateService = heartRateService;
    }

    [HttpGet("daily")]
    [ProducesResponseType<List<DailyHeartRateDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<List<DailyHeartRateDto>> Daily([Range(1, 366)] int days = 30, CancellationToken cancellationToken = default)
    {
        return await heartRateService.ListDailyAsync(User.GetUserNanoid(), days, cancellationToken);
    }
}
