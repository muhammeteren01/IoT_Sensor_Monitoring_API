using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Services;

public class AlertHistoryService : IAlertHistoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AlertHistoryService(IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AlertHistoryDto>> ListAsync(
        Guid? sensorId,
        bool? isResolved,
        CancellationToken cancellationToken = default)
    {
        if (sensorId.HasValue)
        {
            if (!await _unitOfWork.Sensors.AnyAsync(sensor => sensor.Id == sensorId.Value, cancellationToken))
            {
                throw new NotFoundException(nameof(Sensor), sensorId.Value);
            }
        }

        var histories = await _unitOfWork.AlertHistories.ListAsync(sensorId, isResolved, cancellationToken);
        return histories.Select(Map).ToList();
    }

    public Task<IReadOnlyList<AlertHistoryDto>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default)
        => ListAsync(sensorId, isResolved: null, cancellationToken);

    public async Task<AlertHistoryDto> ResolveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var history = await _unitOfWork.AlertHistories.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(AlertHistory), id);

        if (history.IsResolved)
        {
            throw new ConflictException("Alert is already resolved.");
        }

        history.IsResolved = true;
        history.ResolvedAt = DateTime.UtcNow;
        history.ResolvedByUserId = TenantGuard.RequireUserId(_currentUser);

        _unitOfWork.AlertHistories.Update(history);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(history);
    }

    private static AlertHistoryDto Map(AlertHistory history) =>
        new(
            history.Id,
            history.AlertRuleId,
            history.SensorId,
            history.TriggeredValue,
            history.Message,
            history.TriggeredAt,
            history.IsResolved,
            history.ResolvedAt,
            history.ResolvedByUserId,
            history.IsResolved ? "Resolved" : "Open");
}
