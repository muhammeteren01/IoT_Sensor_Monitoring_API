namespace IoTSensorMonitoring.Application.DTOs.DeviceModels;

public record CreateDeviceModelRequest(
    Guid CompanyId,
    string Manufacturer,
    string ModelNumber,
    string SupportedMetrics,
    int? CalibrationPeriodDays);
