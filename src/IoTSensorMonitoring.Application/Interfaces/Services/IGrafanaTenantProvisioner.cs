using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Interfaces.Services;

public interface IGrafanaTenantProvisioner
{
    Task ProvisionAsync(Company company, CancellationToken cancellationToken = default);
    Task ProvisionAllAsync(CancellationToken cancellationToken = default);
    Task EnsureUserAccessAsync(User user, CancellationToken cancellationToken = default);
    Task DeprovisionAsync(Company company, CancellationToken cancellationToken = default);
}
