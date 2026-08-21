using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs.Alerts;

public record UpdateAlertRuleRequest(
    SensorMetric Metric,
    ComparisonOperator Operator,
    decimal Threshold,
    AlertSeverity Severity,
    bool IsActive);
