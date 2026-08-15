using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Persistence.Repositories;

public class FacilityRepository : Repository<Facility>, IFacilityRepository
{
    public FacilityRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<Facility>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await Set
            .AsNoTracking()
            .Where(facility => facility.CompanyId == companyId)
            .OrderBy(facility => facility.Name)
            .ToListAsync(cancellationToken);
    }
}
