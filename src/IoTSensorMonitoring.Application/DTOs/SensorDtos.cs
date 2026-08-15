using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs;

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
    string? DeviceModelName);

public record CreateSensorRequest(
    Guid ZoneId,
    Guid DeviceModelId,
    string Name,
    string MacAddress,
    string? FirmwareVersion);

public record UpdateSensorRequest(
    string Name,
    string? FirmwareVersion,
    SensorStatus Status,
    DateTime? LastCalibrationDate);

public record SetSensorStatusRequest(SensorStatus Status);
