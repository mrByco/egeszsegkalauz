using egeszsegkalauz.services.Services.Imports;

namespace egeszsegkalauz.api.Workers;

public class DailyImportWorker : BackgroundService
{
    private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<DailyImportWorker> logger;

    public DailyImportWorker(IServiceScopeFactory scopeFactory, ILogger<DailyImportWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan delay;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                delay = await scope.ServiceProvider.GetRequiredService<IDailyImportService>().ProcessNextAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Daily import step failed.");
                delay = ErrorDelay;
            }

            if (delay <= TimeSpan.Zero)
                continue;
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
