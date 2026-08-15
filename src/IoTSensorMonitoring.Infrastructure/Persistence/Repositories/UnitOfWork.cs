using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(
        AppDbContext context,
        IRepository<Company> companies,
        IUserRepository users,
        IFacilityRepository facilities,
        IZoneRepository zones,
        IRepository<DeviceModel> deviceModels,
        ISensorRepository sensors,
        ISensorMeasurementRepository sensorMeasurements,
        IAlertRuleRepository alertRules,
        IAlertHistoryRepository alertHistories,
        IMaintenanceLogRepository maintenanceLogs)
    {
        _context = context;
        Companies = companies;
        Users = users;
        Facilities = facilities;
        Zones = zones;
        DeviceModels = deviceModels;
        Sensors = sensors;
        SensorMeasurements = sensorMeasurements;
        AlertRules = alertRules;
        AlertHistories = alertHistories;
        MaintenanceLogs = maintenanceLogs;
    }

    public IRepository<Company> Companies { get; }
    public IUserRepository Users { get; }
    public IFacilityRepository Facilities { get; }
    public IZoneRepository Zones { get; }
    public IRepository<DeviceModel> DeviceModels { get; }
    public ISensorRepository Sensors { get; }
    public ISensorMeasurementRepository SensorMeasurements { get; }
    public IAlertRuleRepository AlertRules { get; }
    public IAlertHistoryRepository AlertHistories { get; }
    public IMaintenanceLogRepository MaintenanceLogs { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
