using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    bool BypassTenantFilters { get; }
    Guid? UserId { get; }
    Guid? CompanyId { get; }
    UserRole? Role { get; }
    bool IsSuperAdmin { get; }

    /// <summary>Authenticated şirket kullanıcısı için tenant filtresi aktif.</summary>
    bool ApplyTenantFilter { get; }
}
