using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.Interfaces.Services;

public interface ISensorService
{
    Task<SensorDto> CreateAsync(CreateSensorRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SensorDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SensorDto>> GetByZoneIdAsync(Guid zoneId, CancellationToken cancellationToken = default);
    Task<SensorDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SensorDto> UpdateAsync(Guid id, UpdateSensorRequest request, CancellationToken cancellationToken = default);
    Task<SensorDto> SetStatusAsync(Guid id, SensorStatus status, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
