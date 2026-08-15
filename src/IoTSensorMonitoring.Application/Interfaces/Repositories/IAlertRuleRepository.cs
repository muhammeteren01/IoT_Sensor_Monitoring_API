using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Interfaces.Repositories;

public interface IAlertRuleRepository : IRepository<AlertRule>
{
    Task<IReadOnlyList<AlertRule>> GetActiveBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertRule>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default);
}
