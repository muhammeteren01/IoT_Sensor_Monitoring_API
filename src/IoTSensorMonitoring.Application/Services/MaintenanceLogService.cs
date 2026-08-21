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

        switch (request.ActionType)
        {
            case MaintenanceActionType.Calibration:
                sensor.LastCalibrationDate = performedAt;
                log.NextDueDate = ComputeNextCalibrationDue(sensor.DeviceModel, performedAt);
                _unitOfWork.Sensors.Update(sensor);
                break;

            case MaintenanceActionType.BatteryReplacement:
                await ApplyBatteryReplacementAsync(sensor, performedAt, cancellationToken);
                break;
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

    private static DateTime? ComputeNextCalibrationDue(DeviceModel deviceModel, DateTime performedAt)
    {
        if (deviceModel.CalibrationPeriodDays is not int periodDays || periodDays <= 0)
        {
            return null;
        }

        return performedAt.AddDays(periodDays);
    }

    private async Task ApplyBatteryReplacementAsync(Sensor sensor, DateTime performedAt, CancellationToken cancellationToken)
    {
        var previous = await _unitOfWork.SensorMeasurements.GetLatestBySensorIdAsync(sensor.Id, cancellationToken);
        var measurementDate = ResolveMeasurementDateAfter(previous, performedAt);

        await _unitOfWork.SensorMeasurements.AddAsync(
            new SensorMeasurement
            {
                SensorId = sensor.Id,
                MeasurementDate = measurementDate,
                BatteryLevel = 100m,
                Temperature = previous?.Temperature,
                Humidity = previous?.Humidity,
                Pressure = previous?.Pressure,
                SignalStrength = previous?.SignalStrength
            },
            cancellationToken);
    }

    private static DateTime ResolveMeasurementDateAfter(SensorMeasurement? previous, DateTime performedAt)
    {
        var measurementDate = DateTime.Now.ToUniversalTime();
        if (measurementDate < performedAt)
        {
            measurementDate = performedAt;
        }

        if (previous?.MeasurementDate >= measurementDate)
        {
            measurementDate = previous.MeasurementDate.AddMilliseconds(1);
        }

        return measurementDate;
    }

    private static MaintenanceLogDto Map(MaintenanceLog log) =>
        new(log.Id, log.SensorId, log.ActionType, log.Description, log.PerformedAt, log.NextDueDate);
}
