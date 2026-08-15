using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Infrastructure.Identity;

public sealed class SystemCurrentUser : ICurrentUser
{
    public bool IsAuthenticated => true;
    public bool BypassTenantFilters => true;
    public Guid? UserId => null;
    public Guid? CompanyId => null;
    public UserRole? Role => UserRole.SuperAdmin;
    public bool IsSuperAdmin => true;
    public bool ApplyTenantFilter => false;
}
