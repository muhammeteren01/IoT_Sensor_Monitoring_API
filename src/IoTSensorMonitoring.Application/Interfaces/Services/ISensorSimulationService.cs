namespace IoTSensorMonitoring.Application.Interfaces.Services;

public interface ISensorSimulationService
{
    Task RunCycleAsync(CancellationToken cancellationToken = default);
}
