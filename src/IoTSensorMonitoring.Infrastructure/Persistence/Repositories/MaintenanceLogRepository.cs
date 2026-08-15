using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Persistence.Repositories;

public class MaintenanceLogRepository : Repository<MaintenanceLog>, IMaintenanceLogRepository
{
    public MaintenanceLogRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<MaintenanceLog>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default)
    {
        return await Set
            .AsNoTracking()
            .Where(log => log.SensorId == sensorId)
            .OrderByDescending(log => log.PerformedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceLog>> GetOverdueAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await Set
            .AsNoTracking()
            .Include(log => log.Sensor)
            .Where(log => log.NextDueDate.HasValue && log.NextDueDate.Value <= utcNow)
            .OrderBy(log => log.NextDueDate)
            .ToListAsync(cancellationToken);
    }
}
