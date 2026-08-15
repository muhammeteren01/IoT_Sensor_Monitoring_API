using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Interfaces.Services;

public interface ISensorMeasurementService
{
    Task<SensorMeasurementDto> CreateAsync(CreateSensorMeasurementRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SensorMeasurementDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SensorMeasurementDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SensorMeasurementDto> GetLatestBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SensorMeasurementDto>> GetBySensorIdAsync(Guid sensorId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
    Task<SensorStatisticsDto> GetStatisticsAsync(Guid sensorId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
