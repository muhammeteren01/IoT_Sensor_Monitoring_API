using IoTSensorMonitoring.Domain.Common;

namespace IoTSensorMonitoring.Domain.Entities;

public class Facility : BaseEntity
{
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    /// <summary>Tesis binasının kat sayısı. Bölgenin bulunduğu kat Zone.FloorLevel.</summary>
    public int FloorCount { get; set; } = 1;

    public Company Company { get; set; } = null!;
    public ICollection<Zone> Zones { get; set; } = [];
}
