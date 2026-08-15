namespace IoTSensorMonitoring.Application.DTOs;

public record DeviceModelDto(
    Guid Id,
    string Manufacturer,
    string ModelNumber,
    string SupportedMetrics,
    int? CalibrationPeriodDays);

public record CreateDeviceModelRequest(
    string Manufacturer,
    string ModelNumber,
    string SupportedMetrics,
    int? CalibrationPeriodDays);

public record UpdateDeviceModelRequest(
    string Manufacturer,
    string ModelNumber,
    string SupportedMetrics,
    int? CalibrationPeriodDays);
