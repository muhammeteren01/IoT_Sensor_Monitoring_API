using FluentValidation;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.Services;

public class MaintenanceLogService : IMaintenanceLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateMaintenanceLogRequest> _createValidator;

    public MaintenanceLogService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateMaintenanceLogRequest> createValidator)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _createValidator = createValidator;
    }

    public async Task<MaintenanceLogDto> CreateAsync(CreateMaintenanceLogRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        var sensor = await _unitOfWork.Sensors.GetByIdWithDetailsAsync(request.SensorId, cancellationToken)
            ?? throw new NotFoundException(nameof(Sensor), request.SensorId);

        TenantGuard.EnsureCompanyAccess(_currentUser, sensor.Zone.Facility.CompanyId);

        var performedAt = request.PerformedAt ?? DateTime.UtcNow;
        var log = new MaintenanceLog
        {
            SensorId = request.SensorId,
            ActionType = request.ActionType,
            Description = request.Description,
            PerformedAt = performedAt,
            NextDueDate = request.NextDueDate
        };

        if (request.ActionType == MaintenanceActionType.Calibration)
        {
            sensor.LastCalibrationDate = performedAt;
            _unitOfWork.Sensors.Update(sensor);
        }

        await _unitOfWork.MaintenanceLogs.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(log);
    }

    public async Task<IReadOnlyList<MaintenanceLogDto>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default)
    {
        if (!await _unitOfWork.Sensors.AnyAsync(sensor => sensor.Id == sensorId, cancellationToken))
        {
            throw new NotFoundException(nameof(Sensor), sensorId);
        }

        var logs = await _unitOfWork.MaintenanceLogs.GetBySensorIdAsync(sensorId, cancellationToken);
        return logs.Select(Map).ToList();
    }

    private static MaintenanceLogDto Map(MaintenanceLog log) =>
        new(log.Id, log.SensorId, log.ActionType, log.Description, log.PerformedAt, log.NextDueDate);
}
