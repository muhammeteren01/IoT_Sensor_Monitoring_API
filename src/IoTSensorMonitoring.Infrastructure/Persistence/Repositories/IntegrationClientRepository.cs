using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Persistence.Repositories;

public class IntegrationClientRepository : Repository<IntegrationClient>, IIntegrationClientRepository
{
    public IntegrationClientRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IntegrationClient?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await Set
            .IgnoreQueryFilters()
            .Include(client => client.Company)
            .FirstOrDefaultAsync(client => client.ClientId == clientId, cancellationToken);
    }
}
