using IoTSensorMonitoring.Domain.Common;
using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Domain.Entities;

public class User : BaseEntity
{
    /// <summary>Null ise sistem geneli yöneticidir (SuperAdmin).</summary>
    public Guid? CompanyId { get; set; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company? Company { get; set; }
}
