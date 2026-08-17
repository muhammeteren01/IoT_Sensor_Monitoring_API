using FluentValidation;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace IoTSensorMonitoring.Application.Services;

public class SensorService : ISensorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SensorService> _logger;
    private readonly IValidator<CreateSensorRequest> _createValidator;
    private readonly IValidator<UpdateSensorRequest> _updateValidator;
    private readonly IValidator<SetSensorStatusRequest> _statusValidator;

    public SensorService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<SensorService> logger,
        IValidator<CreateSensorRequest> createValidator,
        IValidator<UpdateSensorRequest> updateValidator,
        IValidator<SetSensorStatusRequest> statusValidator)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    public async Task<SensorDto> CreateAsync(CreateSensorRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        var zone = await GetZoneRequiredAsync(request.ZoneId, cancellationToken);
        var facility = await _unitOfWork.Facilities.GetByIdAsync(zone.FacilityId, cancellationToken)
            ?? throw new NotFoundException(nameof(Facility), zone.FacilityId);
        TenantGuard.EnsureCompanyAccess(_currentUser, facility.CompanyId);
        await EnsureDeviceModelForCompanyAsync(request.DeviceModelId, facility.CompanyId, cancellationToken);

        if (await _unitOfWork.Sensors.GetByMacAddressAsync(request.MacAddress, cancellationToken) is not null)
        {
            throw new ConflictException($"Sensor with MAC address '{request.MacAddress}' already exists.");
        }

        var sensor = new Sensor
        {
            ZoneId = request.ZoneId,
            DeviceModelId = request.DeviceModelId,
            Name = request.Name,
            MacAddress = request.MacAddress,
            FirmwareVersion = request.FirmwareVersion,
            Status = SensorStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Sensors.AddAsync(sensor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sensor created. Id: {SensorId}, Name: {SensorName}", sensor.Id, sensor.Name);

        return await MapByIdAsync(sensor.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<SensorDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sensors = await _unitOfWork.Sensors.GetAllAsync(cancellationToken);
        return sensors.Select(sensor => Map(sensor)).ToList();
    }

    public async Task<IReadOnlyList<SensorDto>> GetByZoneIdAsync(Guid zoneId, CancellationToken cancellationToken = default)
    {
        await EnsureZoneExistsAsync(zoneId, cancellationToken);
        var sensors = await _unitOfWork.Sensors.GetByZoneIdAsync(zoneId, cancellationToken);
        return sensors.Select(sensor => Map(sensor, sensor.DeviceModel)).ToList();
    }

    public async Task<SensorDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await MapByIdAsync(id, cancellationToken);
    }

    public async Task<SensorDto> UpdateAsync(Guid id, UpdateSensorRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_updateValidator, request, cancellationToken);

        var sensor = await GetRequiredAsync(id, cancellationToken);
        sensor.Name = request.Name;
        sensor.FirmwareVersion = request.FirmwareVersion;
        sensor.Status = request.Status;
        sensor.LastCalibrationDate = request.LastCalibrationDate;

        _unitOfWork.Sensors.Update(sensor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sensor updated. Id: {SensorId}", sensor.Id);

        return await MapByIdAsync(sensor.Id, cancellationToken);
    }

    public async Task<SensorDto> SetStatusAsync(Guid id, SensorStatus status, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_statusValidator, new SetSensorStatusRequest(status), cancellationToken);

        var sensor = await GetRequiredAsync(id, cancellationToken);
        sensor.Status = status;

        _unitOfWork.Sensors.Update(sensor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sensor status changed. Id: {SensorId}, Status: {Status}", sensor.Id, status);

        return await MapByIdAsync(sensor.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sensor = await GetRequiredAsync(id, cancellationToken);

        var histories = await _unitOfWork.AlertHistories.GetBySensorIdAsync(id, cancellationToken);
        foreach (var history in histories)
        {
            _unitOfWork.AlertHistories.Remove(history);
        }

        var rules = await _unitOfWork.AlertRules.GetBySensorIdAsync(id, cancellationToken);
        foreach (var rule in rules)
        {
            _unitOfWork.AlertRules.Remove(rule);
        }

        var measurements = await _unitOfWork.SensorMeasurements.GetBySensorIdAsync(id, null, null, cancellationToken);
        foreach (var measurement in measurements)
        {
            _unitOfWork.SensorMeasurements.Remove(measurement);
        }

        var logs = await _unitOfWork.MaintenanceLogs.GetBySensorIdAsync(id, cancellationToken);
        foreach (var log in logs)
        {
            _unitOfWork.MaintenanceLogs.Remove(log);
        }

        _unitOfWork.Sensors.Remove(sensor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sensor deleted. Id: {SensorId}, Name: {SensorName}", id, sensor.Name);
    }

    private async Task EnsureZoneExistsAsync(Guid zoneId, CancellationToken cancellationToken)
    {
        await GetZoneRequiredAsync(zoneId, cancellationToken);
    }

    private async Task<Zone> GetZoneRequiredAsync(Guid zoneId, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Zones.GetByIdAsync(zoneId, cancellationToken)
            ?? throw new NotFoundException(nameof(Zone), zoneId);
    }

    private async Task EnsureDeviceModelForCompanyAsync(
        Guid deviceModelId,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var deviceModel = await _unitOfWork.DeviceModels.GetByIdAsync(deviceModelId, cancellationToken)
            ?? throw new NotFoundException(nameof(DeviceModel), deviceModelId);

        if (deviceModel.CompanyId != companyId)
        {
            throw new Common.Exceptions.ValidationException("Device model does not belong to this company.");
        }
    }

    private async Task<Sensor> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Sensors.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Sensor), id);
    }

    private async Task<SensorDto> MapByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var sensor = await _unitOfWork.Sensors.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Sensor), id);

        return Map(sensor, sensor.DeviceModel, sensor.Zone);
    }

    private static SensorDto Map(Sensor sensor, DeviceModel? deviceModel = null, Zone? zone = null) =>
        new(
            sensor.Id,
            sensor.ZoneId,
            sensor.DeviceModelId,
            sensor.Name,
            sensor.MacAddress,
            sensor.FirmwareVersion,
            sensor.Status,
            sensor.LastCalibrationDate,
            sensor.CreatedAt,
            zone?.Name,
            deviceModel is null ? null : $"{deviceModel.Manufacturer} {deviceModel.ModelNumber}");
}
