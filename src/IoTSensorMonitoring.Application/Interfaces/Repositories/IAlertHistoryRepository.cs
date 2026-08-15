using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Interfaces.Repositories;

public interface IAlertHistoryRepository : IRepository<AlertHistory>
{
    Task<IReadOnlyList<AlertHistory>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertHistory>> GetUnresolvedBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertHistory>> ListAsync(Guid? sensorId, bool? isResolved, CancellationToken cancellationToken = default);
}
