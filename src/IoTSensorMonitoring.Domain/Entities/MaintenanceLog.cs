using IoTSensorMonitoring.Domain.Common;
using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Domain.Entities;

public class MaintenanceLog : BaseEntity
{
    public Guid SensorId { get; set; }
    public MaintenanceActionType ActionType { get; set; }
    public string? Description { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NextDueDate { get; set; }

    public Sensor Sensor { get; set; } = null!;
}
