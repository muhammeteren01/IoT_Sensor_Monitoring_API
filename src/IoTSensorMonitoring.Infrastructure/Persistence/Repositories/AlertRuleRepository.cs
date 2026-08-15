using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Persistence.Repositories;

public class AlertRuleRepository : Repository<AlertRule>, IAlertRuleRepository
{
    public AlertRuleRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<AlertRule>> GetActiveBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default)
    {
        return await Set
            .Where(rule => rule.SensorId == sensorId && rule.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertRule>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default)
    {
        return await Set
            .AsNoTracking()
            .Where(rule => rule.SensorId == sensorId)
            .ToListAsync(cancellationToken);
    }
}
