namespace IoTSensorMonitoring.Application.DTOs.DeviceModels;

public record UpdateDeviceModelRequest(
    string Manufacturer,
    string ModelNumber,
    string SupportedMetrics,
    int? CalibrationPeriodDays);
