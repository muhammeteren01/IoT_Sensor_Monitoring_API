using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Persistence.Repositories;

public class SensorRepository : Repository<Sensor>, ISensorRepository
{
    public SensorRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<Sensor?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Set
            .Include(sensor => sensor.Zone)
                .ThenInclude(zone => zone.Facility)
                    .ThenInclude(facility => facility.Company)
            .Include(sensor => sensor.DeviceModel)
            .FirstOrDefaultAsync(sensor => sensor.Id == id, cancellationToken);
    }

    public async Task<Sensor?> GetByMacAddressAsync(string macAddress, CancellationToken cancellationToken = default)
    {
        return await Set
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(sensor => sensor.MacAddress == macAddress, cancellationToken);
    }

    public async Task<IReadOnlyList<Sensor>> GetByZoneIdAsync(Guid zoneId, CancellationToken cancellationToken = default)
    {
        return await Set
            .AsNoTracking()
            .Include(sensor => sensor.DeviceModel)
            .Where(sensor => sensor.ZoneId == zoneId)
            .OrderBy(sensor => sensor.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sensor>> GetActiveWithDeviceModelAsync(CancellationToken cancellationToken = default)
    {
        return await Set
            .AsNoTracking()
            .Include(sensor => sensor.DeviceModel)
            .Where(sensor => sensor.Status == SensorStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sensor>> GetAllWithDeviceModelAsync(CancellationToken cancellationToken = default)
    {
        return await Set
            .AsNoTracking()
            .Include(sensor => sensor.DeviceModel)
            .Include(sensor => sensor.Zone)
            .OrderBy(sensor => sensor.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsInCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await Set.AnyAsync(
            sensor => sensor.Zone.Facility.CompanyId == companyId,
            cancellationToken);
    }
}
