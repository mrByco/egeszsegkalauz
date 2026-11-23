using egeszsegkalauz.services.Services.Sync;

namespace egeszsegkalauz.api.Workers;

public class SourceSyncWorker : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<SourceSyncWorker> logger;

    public SourceSyncWorker(IServiceScopeFactory scopeFactory, ILogger<SourceSyncWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var synced = false;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                synced = await scope.ServiceProvider.GetRequiredService<ISourceSyncService>().SyncNextDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Scheduled source sync failed.");
            }

            if (synced)
                continue;
            try
            {
                await Task.Delay(IdleDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
