using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs.Sensors;

public record SensorDto(
    Guid Id,
    Guid ZoneId,
    Guid DeviceModelId,
    string Name,
    string MacAddress,
    string? FirmwareVersion,
    SensorStatus Status,
    DateTime? LastCalibrationDate,
    DateTime CreatedAt,
    string? ZoneName,
    string? DeviceModelName,
    string SupportedMetrics,
    int? CalibrationPeriodDays);
