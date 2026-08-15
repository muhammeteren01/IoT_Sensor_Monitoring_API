using IoTSensorMonitoring.Domain.Common;
using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Domain.Entities;

public class Sensor : BaseEntity
{
    public Guid ZoneId { get; set; }
    public Guid DeviceModelId { get; set; }
    public required string Name { get; set; }
    public required string MacAddress { get; set; }
    public string? FirmwareVersion { get; set; }
    public SensorStatus Status { get; set; } = SensorStatus.Active;
    public DateTime? LastCalibrationDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Zone Zone { get; set; } = null!;
    public DeviceModel DeviceModel { get; set; } = null!;
    public ICollection<SensorMeasurement> Measurements { get; set; } = [];
    public ICollection<AlertRule> AlertRules { get; set; } = [];
    public ICollection<AlertHistory> AlertHistories { get; set; } = [];
    public ICollection<MaintenanceLog> MaintenanceLogs { get; set; } = [];
}
