using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs.Alerts;

public record AlertRuleDto(
    Guid Id,
    Guid SensorId,
    SensorMetric Metric,
    ComparisonOperator Operator,
    decimal Threshold,
    AlertSeverity Severity,
    bool IsActive);
