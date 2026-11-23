using System.ComponentModel.DataAnnotations;
using egeszsegkalauz.api.Auth;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Services.Garmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace egeszsegkalauz.api.Controllers;

[ApiController]
[Authorize]
[Route("api/garmin")]
public class GarminController : ControllerBase
{
    private readonly IGarminDataService garminDataService;

    public GarminController(IGarminDataService garminDataService)
    {
        this.garminDataService = garminDataService;
    }

    [HttpGet("heart-rate-days")]
    [ProducesResponseType<List<GarminHeartRateDayDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<List<GarminHeartRateDayDto>> HeartRateDays([Range(1, 366)] int days = 30,
        CancellationToken cancellationToken = default)
    {
        return await garminDataService.ListHeartRateDaysAsync(User.GetUserNanoid(), days, cancellationToken);
    }
}
