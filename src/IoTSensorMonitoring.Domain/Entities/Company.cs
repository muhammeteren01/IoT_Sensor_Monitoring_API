using IoTSensorMonitoring.Domain.Common;

namespace IoTSensorMonitoring.Domain.Entities;

public class Company : BaseEntity
{
    public required string Name { get; set; }
    public string? ContactEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public int? GrafanaOrgId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Facility> Facilities { get; set; } = [];
    public ICollection<DeviceModel> DeviceModels { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];
    public ICollection<IntegrationClient> IntegrationClients { get; set; } = [];
}
