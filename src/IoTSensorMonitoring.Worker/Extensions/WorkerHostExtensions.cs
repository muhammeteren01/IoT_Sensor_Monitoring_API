using IoTSensorMonitoring.Worker.Integration;
using IoTSensorMonitoring.Worker.Integration.Api;
using IoTSensorMonitoring.Worker.Integration.Store;
using IoTSensorMonitoring.Worker.Settings;

namespace IoTSensorMonitoring.Worker.Extensions;

public static class WorkerHostExtensions
{
    public static WorkerExecutionMode ResolveExecutionMode(IConfiguration configuration)
    {
        var section = configuration.GetSection(WorkerExecutionSettings.SectionName);
        if (!section.Exists())
        {
            return WorkerExecutionMode.DirectDb;
        }

        var settings = section.Get<WorkerExecutionSettings>();
        return settings?.Mode ?? WorkerExecutionMode.DirectDb;
    }

    public static void AddWorkerExecution(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WorkerExecutionSettings>(configuration.GetSection(WorkerExecutionSettings.SectionName));
        services.Configure<IntegrationSettings>(configuration.GetSection(IntegrationSettings.SectionName));

        var mode = ResolveExecutionMode(configuration);
        switch (mode)
        {
            case WorkerExecutionMode.DirectDb:
                services.AddHostedService<Worker>();
                break;
            case WorkerExecutionMode.ApiIntegration:
                services.AddSingleton<IntegrationTokenCache>();
                services.AddSingleton<ILocalQueueStore, SqliteLocalQueueStore>();
                services.AddHttpClient<IIntegrationApiClient, IntegrationApiClient>();
                services.AddScoped<IIntegrationPipeline, IntegrationPipeline>();
                services.AddHostedService<IntegrationWorker>();
                break;
            default:
                throw new InvalidOperationException($"Unsupported worker execution mode: {mode}");
        }
    }
}
