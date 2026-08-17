namespace IoTSensorMonitoring.Infrastructure.Grafana;

public sealed class GrafanaAdminAuthException : InvalidOperationException
{
    public GrafanaAdminAuthException(string message)
        : base(message)
    {
    }
}
