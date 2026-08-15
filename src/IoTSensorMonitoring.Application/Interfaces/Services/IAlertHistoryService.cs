using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Interfaces.Services;

public interface IAlertHistoryService
{
    Task<IReadOnlyList<AlertHistoryDto>> ListAsync(Guid? sensorId, bool? isResolved, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertHistoryDto>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default);
    Task<AlertHistoryDto> ResolveAsync(Guid id, CancellationToken cancellationToken = default);
}
