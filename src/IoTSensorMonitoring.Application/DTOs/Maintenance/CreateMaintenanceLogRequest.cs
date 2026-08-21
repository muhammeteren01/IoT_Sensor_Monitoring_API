using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs.Maintenance;

public record CreateMaintenanceLogRequest(
    Guid SensorId,
    MaintenanceActionType ActionType,
    string? Description,
    DateTime? PerformedAt,
    DateTime? NextDueDate);
