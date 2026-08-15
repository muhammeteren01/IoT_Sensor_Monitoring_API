using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Interfaces.Repositories;

public interface IZoneRepository : IRepository<Zone>
{
    Task<IReadOnlyList<Zone>> GetByFacilityIdAsync(Guid facilityId, CancellationToken cancellationToken = default);
}
