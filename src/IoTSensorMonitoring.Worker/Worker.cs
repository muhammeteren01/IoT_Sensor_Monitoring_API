using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Settings;
using Microsoft.Extensions.Options;

namespace IoTSensorMonitoring.Worker;

public class Worker(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerSettings> settings,
    ILogger<Worker> logger) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "DirectDb Worker starting. IntervalSeconds={IntervalSeconds}",
            Math.Max(1, settings.Value.IntervalSeconds));
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Worker Service stopping");
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, settings.Value.IntervalSeconds));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var simulation = scope.ServiceProvider.GetRequiredService<ISensorSimulationService>();
                    await simulation.RunCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Simulation cycle failed");
                }

                await Task.Delay(interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Worker Service failed");
            throw;
        }
    }
}
