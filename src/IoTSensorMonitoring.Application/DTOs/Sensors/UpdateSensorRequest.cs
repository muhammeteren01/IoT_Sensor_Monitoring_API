using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs.Sensors;

public record UpdateSensorRequest(
    string Name,
    string? FirmwareVersion,
    SensorStatus Status,
    DateTime? LastCalibrationDate);
