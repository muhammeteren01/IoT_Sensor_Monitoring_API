namespace IoTSensorMonitoring.Application.DTOs.Alerts;

public record AlertHistoryDto(
    Guid Id,
    Guid AlertRuleId,
    Guid SensorId,
    decimal TriggeredValue,
    string Message,
    DateTime TriggeredAt,
    bool IsResolved,
    DateTime? ResolvedAt,
    Guid? ResolvedByUserId,
    string Status);
