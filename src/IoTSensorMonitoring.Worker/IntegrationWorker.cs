using IoTSensorMonitoring.Worker.Integration;
using IoTSensorMonitoring.Worker.Integration.Store;
using IoTSensorMonitoring.Worker.Settings;
using Microsoft.Extensions.Options;

namespace IoTSensorMonitoring.Worker;

public sealed class IntegrationWorker(
    IServiceScopeFactory scopeFactory,
    ILocalQueueStore store,
    IOptions<IntegrationSettings> integrationSettings,
    ILogger<IntegrationWorker> logger) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = integrationSettings.Value;
        logger.LogInformation(
            "Integration Worker starting. ApiBaseUrl={ApiBaseUrl}, IntervalSeconds={IntervalSeconds}, CalibrationWarningDays={CalibrationWarningDays}, SensorSyncIntervalSeconds={SensorSyncIntervalSeconds}, FlushBatchSize={FlushBatchSize}, ClientCount={ClientCount}, LocalStorePath={LocalStorePath}",
            settings.ApiBaseUrl,
            Math.Max(1, settings.IntervalSeconds),
            Math.Max(0, settings.CalibrationWarningDays),
            Math.Max(30, settings.SensorSyncIntervalSeconds),
            Math.Max(1, settings.FlushBatchSize),
            settings.Clients.Count,
            settings.LocalStorePath);

        if (settings.Clients.Count == 0)
        {
            logger.LogWarning("Integration Worker has no clients configured. Add IntegrationSettings:Clients in config.");
        }

        await store.InitializeAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Integration Worker stopping");
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, integrationSettings.Value.IntervalSeconds));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var pipeline = scope.ServiceProvider.GetRequiredService<IIntegrationPipeline>();
                    await pipeline.RunCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Integration cycle failed");
                }

                await Task.Delay(interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Integration Worker failed");
            throw;
        }
    }
}
