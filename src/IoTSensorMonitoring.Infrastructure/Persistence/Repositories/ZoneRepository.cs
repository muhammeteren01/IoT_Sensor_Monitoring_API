using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Persistence.Repositories;

public class ZoneRepository : Repository<Zone>, IZoneRepository
{
    public ZoneRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Zone>> GetByFacilityIdAsync(Guid facilityId, CancellationToken cancellationToken = default)
    {
        return await Set
            .AsNoTracking()
            .Where(zone => zone.FacilityId == facilityId)
            .OrderBy(zone => zone.FloorLevel)
            .ThenBy(zone => zone.Name)
            .ToListAsync(cancellationToken);
    }
}
