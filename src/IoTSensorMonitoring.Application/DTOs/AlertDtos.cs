using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs;

public record AlertRuleDto(
    Guid Id,
    Guid SensorId,
    SensorMetric Metric,
    ComparisonOperator Operator,
    decimal Threshold,
    AlertSeverity Severity,
    bool IsActive);

public record CreateAlertRuleRequest(
    Guid SensorId,
    SensorMetric Metric,
    ComparisonOperator Operator,
    decimal Threshold,
    AlertSeverity Severity);

public record UpdateAlertRuleRequest(
    SensorMetric Metric,
    ComparisonOperator Operator,
    decimal Threshold,
    AlertSeverity Severity,
    bool IsActive);

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

