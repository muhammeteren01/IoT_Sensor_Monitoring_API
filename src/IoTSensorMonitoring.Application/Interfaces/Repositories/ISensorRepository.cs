using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Interfaces.Repositories;

public interface ISensorRepository : IRepository<Sensor>
{
    Task<Sensor?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sensor?> GetByMacAddressAsync(string macAddress, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sensor>> GetByZoneIdAsync(Guid zoneId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sensor>> GetActiveWithDeviceModelAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsInCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
