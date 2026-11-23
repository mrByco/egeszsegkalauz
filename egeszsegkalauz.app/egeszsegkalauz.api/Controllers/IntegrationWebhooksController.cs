using egeszsegkalauz.data.Models;
using egeszsegkalauz.services.Services.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace egeszsegkalauz.api.Controllers;

[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/integrations/{source}/webhook")]
public class IntegrationWebhooksController : ControllerBase
{
    private readonly ISourceSyncService sourceSyncService;

    public IntegrationWebhooksController(ISourceSyncService sourceSyncService)
    {
        this.sourceSyncService = sourceSyncService;
    }

    // The signature covers the exact bytes sent, so the body is read raw instead of model-bound.
    [HttpPost]
    public async Task<IActionResult> Receive(HealthDataSource source, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var accepted = await sourceSyncService.HandleWebhookAsync(source, body,
            name => Request.Headers.TryGetValue(name, out var value) ? value.ToString() : null, cancellationToken);
        return accepted ? Ok() : Unauthorized();
    }
}
