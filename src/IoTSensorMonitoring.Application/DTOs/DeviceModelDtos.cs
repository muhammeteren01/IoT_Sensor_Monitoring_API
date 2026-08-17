namespace IoTSensorMonitoring.Application.DTOs;

public record DeviceModelDto(
    Guid Id,
    Guid CompanyId,
    string Manufacturer,
    string ModelNumber,
    string SupportedMetrics,
    int? CalibrationPeriodDays);

public record CreateDeviceModelRequest(
    Guid CompanyId,
    string Manufacturer,
    string ModelNumber,
    string SupportedMetrics,
    int? CalibrationPeriodDays);

public record UpdateDeviceModelRequest(
    string Manufacturer,
    string ModelNumber,
    string SupportedMetrics,
    int? CalibrationPeriodDays);
