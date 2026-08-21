using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Interfaces.Repositories;

public interface IIntegrationClientRepository : IRepository<IntegrationClient>
{
    Task<IntegrationClient?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
}
