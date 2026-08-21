using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Worker.Integration.Contracts;

/// <summary>GET /api/sensors yanıtı — Worker sensör sync için.</summary>
public record SyncSensorContract(
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
