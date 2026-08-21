using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.Authorization;

public static class AppRoles
{
    public const string SuperAdmin = nameof(UserRole.SuperAdmin);
    public const string CompanyAdmin = nameof(UserRole.CompanyAdmin);
    public const string Operator = nameof(UserRole.Operator);

    public const string IntegrationClient = "IntegrationClient";

    public const string All = $"{SuperAdmin},{CompanyAdmin},{Operator}";
    public const string SensorReaders = $"{All},{IntegrationClient}";
    public const string CompanyAdmins = $"{SuperAdmin},{CompanyAdmin}";
    public const string Writers = $"{SuperAdmin},{CompanyAdmin}";
    public const string SuperAdminOnly = SuperAdmin;
    public const string MeasurementWriters = $"{SuperAdmin},{CompanyAdmin},{Operator},{IntegrationClient}";
}
