using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
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

    public async Task<MaintenanceLog?> GetLatestBySensorIdAndActionTypeAsync(
        Guid sensorId,
        MaintenanceActionType actionType,
        CancellationToken cancellationToken = default)
    {
        return await Set
            .AsNoTracking()
            .Where(log => log.SensorId == sensorId && log.ActionType == actionType)
            .OrderByDescending(log => log.PerformedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceLog>> GetOverdueAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var latestCalibrationIds = await Set
            .AsNoTracking()
            .Where(log => log.ActionType == MaintenanceActionType.Calibration)
            .GroupBy(log => log.SensorId)
            .Select(group => group
                .OrderByDescending(log => log.PerformedAt)
                .Select(log => log.Id)
                .First())
            .ToListAsync(cancellationToken);

        if (latestCalibrationIds.Count == 0)
        {
            return [];
        }

        return await Set
            .AsNoTracking()
            .Include(log => log.Sensor)
            .Where(log =>
                latestCalibrationIds.Contains(log.Id) &&
                log.NextDueDate.HasValue &&
                log.NextDueDate.Value <= utcNow)
            .OrderBy(log => log.NextDueDate)
            .ToListAsync(cancellationToken);
    }
}
