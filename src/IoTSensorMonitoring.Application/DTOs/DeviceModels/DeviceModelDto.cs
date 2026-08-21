namespace IoTSensorMonitoring.Application.DTOs.DeviceModels;

public record DeviceModelDto(
    Guid Id,
    Guid CompanyId,
    string Manufacturer,
    string ModelNumber,
    string SupportedMetrics,
    int? CalibrationPeriodDays);
