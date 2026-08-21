using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Company> Companies { get; }
    IIntegrationClientRepository IntegrationClients { get; }
    IUserRepository Users { get; }
    IFacilityRepository Facilities { get; }
    IZoneRepository Zones { get; }
    IRepository<DeviceModel> DeviceModels { get; }
    ISensorRepository Sensors { get; }
    ISensorMeasurementRepository SensorMeasurements { get; }
    IAlertRuleRepository AlertRules { get; }
    IAlertHistoryRepository AlertHistories { get; }
    IMaintenanceLogRepository MaintenanceLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
