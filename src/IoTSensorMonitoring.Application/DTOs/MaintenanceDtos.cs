using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs;

public record MaintenanceLogDto(
    Guid Id,
    Guid SensorId,
    MaintenanceActionType ActionType,
    string? Description,
    DateTime PerformedAt,
    DateTime? NextDueDate);

public record CreateMaintenanceLogRequest(
    Guid SensorId,
    MaintenanceActionType ActionType,
    string? Description,
    DateTime? PerformedAt,
    DateTime? NextDueDate);
