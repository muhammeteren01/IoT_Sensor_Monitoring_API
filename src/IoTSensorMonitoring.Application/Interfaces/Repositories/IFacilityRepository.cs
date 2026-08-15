using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Interfaces.Repositories;

public interface IFacilityRepository : IRepository<Facility>
{
    Task<IReadOnlyList<Facility>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
