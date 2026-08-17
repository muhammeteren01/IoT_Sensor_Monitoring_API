using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Infrastructure.Grafana;
using Microsoft.Extensions.Options;
using IoTSensorMonitoring.Application.Settings;

namespace IoTSensorMonitoring.Api.HostedServices;

public sealed class GrafanaTenantSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GrafanaTenantSyncHostedService> _logger;
    private readonly GrafanaSettings _settings;

    public GrafanaTenantSyncHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<GrafanaSettings> settings,
        ILogger<GrafanaTenantSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Grafana tenant sync is disabled");
            return;
        }

        var delay = TimeSpan.FromSeconds(8);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(delay, stoppingToken);
                await using var scope = _scopeFactory.CreateAsyncScope();
                var provisioner = scope.ServiceProvider.GetRequiredService<IGrafanaTenantProvisioner>();
                await provisioner.ProvisionAllAsync(stoppingToken);
                delay = TimeSpan.FromMinutes(2);
            }
            catch (GrafanaAdminAuthException exception)
            {
                _logger.LogWarning(exception, "Grafana admin API unauthorized; backing off tenant sync");
                delay = TimeSpan.FromMinutes(15);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Grafana tenant sync cycle failed");
                delay = TimeSpan.FromMinutes(5);
            }
        }
    }
}
