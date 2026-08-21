using IoTSensorMonitoring.Domain.Common;

namespace IoTSensorMonitoring.Domain.Entities;

public class IntegrationClient : BaseEntity
{
    public Guid CompanyId { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecretHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
}
