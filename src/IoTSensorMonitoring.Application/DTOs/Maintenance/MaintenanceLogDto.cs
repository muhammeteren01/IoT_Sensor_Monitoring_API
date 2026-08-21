using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs.Maintenance;

public record MaintenanceLogDto(
    Guid Id,
    Guid SensorId,
    MaintenanceActionType ActionType,
    string? Description,
    DateTime PerformedAt,
    DateTime? NextDueDate);
