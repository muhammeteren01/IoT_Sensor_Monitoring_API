using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.Authorization;

public static class TenantGuard
{
    public static Guid ResolveCompanyId(ICurrentUser currentUser, Guid? requestCompanyId)
    {
        if (currentUser.IsSuperAdmin)
        {
            if (!requestCompanyId.HasValue || requestCompanyId.Value == Guid.Empty)
            {
                throw new ValidationException("SuperAdmin requires CompanyId.");
            }

            return requestCompanyId.Value;
        }

        if (!currentUser.CompanyId.HasValue || currentUser.CompanyId.Value == Guid.Empty)
        {
            throw new ForbiddenException("Company context was not found.");
        }

        return currentUser.CompanyId.Value;
    }

    public static Guid RequireUserId(ICurrentUser currentUser)
    {
        if (!currentUser.UserId.HasValue || currentUser.UserId.Value == Guid.Empty)
        {
            throw new ForbiddenException("User identity was not found.");
        }

        return currentUser.UserId.Value;
    }

    public static void EnsureCompanyAccess(ICurrentUser currentUser, Guid companyId)
    {
        if (currentUser.IsSuperAdmin)
        {
            return;
        }

        if (!currentUser.CompanyId.HasValue || currentUser.CompanyId.Value != companyId)
        {
            throw new ForbiddenException("You do not have access to this company.");
        }
    }

    public static void EnsureCanAssignRole(ICurrentUser currentUser, UserRole targetRole)
    {
        if (targetRole == UserRole.SuperAdmin && !currentUser.IsSuperAdmin)
        {
            throw new ForbiddenException("Only SuperAdmin can assign the SuperAdmin role.");
        }
    }

    public static Guid? ResolveUserCompanyId(ICurrentUser currentUser, UserRole role, Guid? requestCompanyId)
    {
        EnsureCanAssignRole(currentUser, role);
        if (role == UserRole.SuperAdmin)
        {
            return null;
        }

        return ResolveCompanyId(currentUser, requestCompanyId);
    }
}
