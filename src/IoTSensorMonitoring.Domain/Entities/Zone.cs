using IoTSensorMonitoring.Domain.Common;

namespace IoTSensorMonitoring.Domain.Entities;

public class Zone : BaseEntity
{
    public Guid FacilityId { get; set; }
    public required string Name { get; set; }
    public int FloorLevel { get; set; }

    public Facility Facility { get; set; } = null!;
    public ICollection<Sensor> Sensors { get; set; } = [];
}
