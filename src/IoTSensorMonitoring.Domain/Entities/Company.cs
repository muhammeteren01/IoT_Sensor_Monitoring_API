using IoTSensorMonitoring.Domain.Common;

namespace IoTSensorMonitoring.Domain.Entities;

public class Company : BaseEntity
{
    public required string Name { get; set; }
    public string? ContactEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Facility> Facilities { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];
}
