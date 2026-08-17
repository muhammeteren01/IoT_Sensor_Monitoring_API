namespace IoTSensorMonitoring.Application.Settings;

public class GrafanaSettings
{
    public const string SectionName = "GrafanaSettings";
    public const string MainOrgName = "Main Org.";
    public const string DashboardUid = "iot-sensor-monitoring";
    public const string DatasourceUid = "postgres";
    public const string DatasourceName = "PostgreSQL";

    public bool Enabled { get; set; }
    public string InternalUrl { get; set; } = "http://localhost:3000";
    public string AdminUser { get; set; } = "admin";
    public string AdminPassword { get; set; } = "admin";
    public string ClientId { get; set; } = "grafana";
    public string ClientSecret { get; set; } = "";
    public string RedirectUris { get; set; } = "http://localhost:3000/login/generic_oauth,http://localhost:30300/login/generic_oauth";
    public string PostgresHost { get; set; } = "localhost";
    public int PostgresPort { get; set; } = 5432;
    public string PostgresDatabase { get; set; } = "iot_sensor_monitoring";
    public string RolePasswordSecret { get; set; } = "";

    public IReadOnlyList<string> AllowedRedirectUris =>
        RedirectUris
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
