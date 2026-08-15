using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.Authorization;

public static class AppRoles
{
    public const string SuperAdmin = nameof(UserRole.SuperAdmin);
    public const string CompanyAdmin = nameof(UserRole.CompanyAdmin);
    public const string Operator = nameof(UserRole.Operator);

    public const string All = $"{SuperAdmin},{CompanyAdmin},{Operator}";
    public const string CompanyAdmins = $"{SuperAdmin},{CompanyAdmin}";
    public const string Writers = $"{SuperAdmin},{CompanyAdmin}";
    public const string SuperAdminOnly = SuperAdmin;
}
