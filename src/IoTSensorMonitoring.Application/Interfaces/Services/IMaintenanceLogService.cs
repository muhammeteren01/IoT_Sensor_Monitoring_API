using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Interfaces.Services;

public interface IMaintenanceLogService
{
    Task<MaintenanceLogDto> CreateAsync(CreateMaintenanceLogRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceLogDto>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default);
}
