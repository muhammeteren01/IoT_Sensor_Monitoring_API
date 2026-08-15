using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Interfaces.Repositories;

public interface ISensorMeasurementRepository : IRepository<SensorMeasurement>
{
    Task<SensorMeasurement?> GetLatestBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SensorMeasurement>> GetBySensorIdAsync(Guid sensorId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
