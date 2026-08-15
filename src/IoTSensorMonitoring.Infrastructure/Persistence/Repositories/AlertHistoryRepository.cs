using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Persistence.Repositories;

public class AlertHistoryRepository : Repository<AlertHistory>, IAlertHistoryRepository
{
    public AlertHistoryRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<AlertHistory>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default)
    {
        return await ListAsync(sensorId, isResolved: null, cancellationToken);
    }

    public async Task<IReadOnlyList<AlertHistory>> GetUnresolvedBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default)
    {
        return await Set
            .Where(history => history.SensorId == sensorId && !history.IsResolved)
            .OrderByDescending(history => history.TriggeredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertHistory>> ListAsync(
        Guid? sensorId,
        bool? isResolved,
        CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (sensorId.HasValue)
        {
            query = query.Where(history => history.SensorId == sensorId.Value);
        }

        if (isResolved.HasValue)
        {
            query = query.Where(history => history.IsResolved == isResolved.Value);
        }

        return await query
            .OrderByDescending(history => history.TriggeredAt)
            .ToListAsync(cancellationToken);
    }
}
