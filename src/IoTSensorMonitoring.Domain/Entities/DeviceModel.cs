using IoTSensorMonitoring.Domain.Common;

namespace IoTSensorMonitoring.Domain.Entities;

public class DeviceModel : BaseEntity
{
    public required string Manufacturer { get; set; }
    public required string ModelNumber { get; set; }
    public required string SupportedMetrics { get; set; }
    public int? CalibrationPeriodDays { get; set; }

    public ICollection<Sensor> Sensors { get; set; } = [];
}
