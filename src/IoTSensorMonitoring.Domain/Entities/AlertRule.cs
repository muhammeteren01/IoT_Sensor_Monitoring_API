using IoTSensorMonitoring.Domain.Common;
using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Domain.Entities;

public class AlertRule : BaseEntity
{
    public Guid SensorId { get; set; }
    public SensorMetric Metric { get; set; }
    public ComparisonOperator Operator { get; set; }
    public decimal Threshold { get; set; }
    public AlertSeverity Severity { get; set; }
    public bool IsActive { get; set; } = true;

    public Sensor Sensor { get; set; } = null!;
    public ICollection<AlertHistory> AlertHistories { get; set; } = [];
}
