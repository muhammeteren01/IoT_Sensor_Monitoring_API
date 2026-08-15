using IoTSensorMonitoring.Domain.Common;

namespace IoTSensorMonitoring.Domain.Entities;

public class AlertHistory : BaseEntity
{
    public Guid AlertRuleId { get; set; }
    public Guid SensorId { get; set; }
    public decimal TriggeredValue { get; set; }
    public required string Message { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }

    public AlertRule AlertRule { get; set; } = null!;
    public Sensor Sensor { get; set; } = null!;
}
