using FluentValidation;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IoTSensorMonitoring.Application.Services;

public class SensorMeasurementService : ISensorMeasurementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SensorMeasurementService> _logger;
    private readonly IValidator<CreateSensorMeasurementRequest> _createValidator;

    public SensorMeasurementService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<SensorMeasurementService> logger,
        IValidator<CreateSensorMeasurementRequest> createValidator)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
        _createValidator = createValidator;
    }

    public async Task<SensorMeasurementDto> CreateAsync(CreateSensorMeasurementRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        await EnsureSensorTenantAsync(request.SensorId, cancellationToken);

        var measurement = new SensorMeasurement
        {
            SensorId = request.SensorId,
            Temperature = request.Temperature,
            Humidity = request.Humidity,
            Pressure = request.Pressure,
            BatteryLevel = request.BatteryLevel,
            SignalStrength = request.SignalStrength,
            MeasurementDate = request.MeasurementDate ?? DateTime.UtcNow
        };

        await _unitOfWork.SensorMeasurements.AddAsync(measurement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sensor measurement created. SensorId: {SensorId}, MeasurementId: {MeasurementId}", request.SensorId, measurement.Id);

        return Map(measurement);
    }

    public async Task<IReadOnlyList<SensorMeasurementDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var measurements = await _unitOfWork.SensorMeasurements.GetAllAsync(cancellationToken);
        return measurements.Select(Map).ToList();
    }

    public async Task<SensorMeasurementDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var measurement = await _unitOfWork.SensorMeasurements.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(SensorMeasurement), id);

        return Map(measurement);
    }

    public async Task<SensorMeasurementDto> GetLatestBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default)
    {
        await EnsureSensorExistsAsync(sensorId, cancellationToken);

        var measurement = await _unitOfWork.SensorMeasurements.GetLatestBySensorIdAsync(sensorId, cancellationToken)
            ?? throw new NotFoundException(nameof(SensorMeasurement), sensorId);

        return Map(measurement);
    }

    public async Task<IReadOnlyList<SensorMeasurementDto>> GetBySensorIdAsync(
        Guid sensorId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        await EnsureSensorExistsAsync(sensorId, cancellationToken);
        var measurements = await _unitOfWork.SensorMeasurements.GetBySensorIdAsync(sensorId, from, to, cancellationToken);
        return measurements.Select(Map).ToList();
    }

    public async Task<SensorStatisticsDto> GetStatisticsAsync(
        Guid sensorId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        await EnsureSensorExistsAsync(sensorId, cancellationToken);
        var measurements = await _unitOfWork.SensorMeasurements.GetBySensorIdAsync(sensorId, from, to, cancellationToken);

        var temperatures = measurements.Where(item => item.Temperature.HasValue).Select(item => item.Temperature!.Value).ToList();
        var humidities = measurements.Where(item => item.Humidity.HasValue).Select(item => item.Humidity!.Value).ToList();
        var pressures = measurements.Where(item => item.Pressure.HasValue).Select(item => item.Pressure!.Value).ToList();

        return new SensorStatisticsDto(
            sensorId,
            from,
            to,
            measurements.Count,
            temperatures.Count == 0 ? null : temperatures.Average(),
            temperatures.Count == 0 ? null : temperatures.Min(),
            temperatures.Count == 0 ? null : temperatures.Max(),
            humidities.Count == 0 ? null : humidities.Average(),
            humidities.Count == 0 ? null : humidities.Min(),
            humidities.Count == 0 ? null : humidities.Max(),
            pressures.Count == 0 ? null : pressures.Min(),
            pressures.Count == 0 ? null : pressures.Max());
    }

    private async Task EnsureSensorExistsAsync(Guid sensorId, CancellationToken cancellationToken)
    {
        await EnsureSensorTenantAsync(sensorId, cancellationToken);
    }

    private async Task EnsureSensorTenantAsync(Guid sensorId, CancellationToken cancellationToken)
    {
        var sensor = await _unitOfWork.Sensors.GetByIdWithDetailsAsync(sensorId, cancellationToken)
            ?? throw new NotFoundException(nameof(Sensor), sensorId);

        TenantGuard.EnsureCompanyAccess(_currentUser, sensor.Zone.Facility.CompanyId);
    }

    private static SensorMeasurementDto Map(SensorMeasurement measurement) =>
        new(
            measurement.Id,
            measurement.SensorId,
            measurement.Temperature,
            measurement.Humidity,
            measurement.Pressure,
            measurement.BatteryLevel,
            measurement.SignalStrength,
            measurement.MeasurementDate);
}
