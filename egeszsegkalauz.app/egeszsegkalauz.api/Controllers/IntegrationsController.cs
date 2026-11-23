using egeszsegkalauz.api.Auth;
using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.DTO;
using egeszsegkalauz.services.Services.Imports;
using egeszsegkalauz.services.Services.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace egeszsegkalauz.api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationService integrationService;
    private readonly ISummaryImportService summaryImportService;
    private readonly IDailyImportService dailyImportService;

    public IntegrationsController(IIntegrationService integrationService, ISummaryImportService summaryImportService,
        IDailyImportService dailyImportService)
    {
        this.integrationService = integrationService;
        this.summaryImportService = summaryImportService;
        this.dailyImportService = dailyImportService;
    }

    [HttpGet]
    [ProducesResponseType<List<IntegrationDto>>(StatusCodes.Status200OK)]
    public async Task<List<IntegrationDto>> List(CancellationToken cancellationToken)
    {
        return await integrationService.ListAsync(User.GetUserNanoid(), cancellationToken);
    }

    [HttpPut("{source}")]
    [ProducesResponseType<IntegrationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<IntegrationDto> Connect(HealthDataSource source, ConnectIntegrationRequest request,
        CancellationToken cancellationToken)
    {
        return await integrationService.ConnectAsync(User.GetUserNanoid(), source, request.ApiKey, cancellationToken);
    }

    [HttpDelete("{source}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<NoContentResult> Disconnect(HealthDataSource source, CancellationToken cancellationToken)
    {
        await integrationService.DisconnectAsync(User.GetUserNanoid(), source, cancellationToken);
        return NoContent();
    }

    [HttpPost("{source}/summaries/import")]
    [ProducesResponseType<SummaryImportResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<SummaryImportResultDto> ImportSummaries(HealthDataSource source, CancellationToken cancellationToken)
    {
        return await summaryImportService.ImportAsync(User.GetUserNanoid(), source, cancellationToken);
    }

    [HttpGet("{source}/daily-import")]
    [ProducesResponseType<DailyImportStatusDto>(StatusCodes.Status200OK)]
    public async Task<DailyImportStatusDto> DailyImportStatus(HealthDataSource source, CancellationToken cancellationToken)
    {
        return await dailyImportService.GetStatusAsync(User.GetUserNanoid(), source, cancellationToken);
    }

    [HttpPost("{source}/daily-import")]
    [ProducesResponseType<ImportJobDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ImportJobDto> StartDailyImport(HealthDataSource source, CancellationToken cancellationToken)
    {
        return await dailyImportService.StartAsync(User.GetUserNanoid(), source, cancellationToken);
    }

    [HttpDelete("{source}/daily-import")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<NoContentResult> CancelDailyImport(HealthDataSource source, CancellationToken cancellationToken)
    {
        await dailyImportService.CancelAsync(User.GetUserNanoid(), source, cancellationToken);
        return NoContent();
    }
}
