namespace IoTSensorMonitoring.Application.DTOs.Sensors;

public record CreateSensorRequest(
    Guid ZoneId,
    Guid DeviceModelId,
    string Name,
    string MacAddress,
    string? FirmwareVersion);
