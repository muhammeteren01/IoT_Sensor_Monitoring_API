using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs.Alerts;

public record CreateAlertRuleRequest(
    Guid SensorId,
    SensorMetric Metric,
    ComparisonOperator Operator,
    decimal Threshold,
    AlertSeverity Severity);
