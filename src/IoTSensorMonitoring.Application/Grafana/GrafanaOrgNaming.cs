using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Grafana;

public static class GrafanaOrgNaming
{
    public static string ForCompany(Company company) =>
        $"{company.Name} · {company.Id.ToString("N")[..8]}";
}
