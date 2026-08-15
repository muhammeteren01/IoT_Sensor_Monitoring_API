using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Interfaces.Services;

public interface IAlertRuleService
{
    Task<AlertRuleDto> CreateAsync(CreateAlertRuleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertRuleDto>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default);
    Task<AlertRuleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AlertRuleDto> UpdateAsync(Guid id, UpdateAlertRuleRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
