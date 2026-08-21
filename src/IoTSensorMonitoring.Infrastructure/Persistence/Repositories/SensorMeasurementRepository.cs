using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Persistence.Repositories;

public class SensorMeasurementRepository : Repository<SensorMeasurement>, ISensorMeasurementRepository
{
    public SensorMeasurementRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<SensorMeasurement?> GetLatestBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default)
    {
        return await Set
            .AsNoTracking()
            .Where(measurement => measurement.SensorId == sensorId)
            .OrderByDescending(measurement => measurement.MeasurementDate)
            .ThenByDescending(measurement => measurement.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SensorMeasurement?> GetBySensorIdAndMeasurementDateAsync(
        Guid sensorId,
        DateTime measurementDate,
        CancellationToken cancellationToken = default)
    {
        return await Set
            .AsNoTracking()
            .FirstOrDefaultAsync(
                measurement => measurement.SensorId == sensorId && measurement.MeasurementDate == measurementDate,
                cancellationToken);
    }

    public async Task<bool> HasFullBatteryMeasurementSinceAsync(
        Guid sensorId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        return await Set
            .AsNoTracking()
            .AnyAsync(
                measurement =>
                    measurement.SensorId == sensorId &&
                    measurement.MeasurementDate >= since &&
                    measurement.BatteryLevel >= 99m,
                cancellationToken);
    }

    public async Task<IReadOnlyList<SensorMeasurement>> GetBySensorIdAsync(
        Guid sensorId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().Where(measurement => measurement.SensorId == sensorId);

        if (from.HasValue)
        {
            query = query.Where(measurement => measurement.MeasurementDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(measurement => measurement.MeasurementDate <= to.Value);
        }

        return await query
            .OrderByDescending(measurement => measurement.MeasurementDate)
            .ToListAsync(cancellationToken);
    }
}
