using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Interfaces.Repositories;

public interface IMaintenanceLogRepository : IRepository<MaintenanceLog>
{
    Task<IReadOnlyList<MaintenanceLog>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceLog>> GetOverdueAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
