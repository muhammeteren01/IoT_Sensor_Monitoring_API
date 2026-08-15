using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTSensorMonitoring.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ICurrentUser _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public bool ApplyTenantFilter => _currentUser.ApplyTenantFilter;
    public Guid CurrentTenantId => _currentUser.CompanyId ?? Guid.Empty;

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<DeviceModel> DeviceModels => Set<DeviceModel>();
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<SensorMeasurement> SensorMeasurements => Set<SensorMeasurement>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<AlertHistory> AlertHistories => Set<AlertHistory>();
    public DbSet<MaintenanceLog> MaintenanceLogs => Set<MaintenanceLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<Company>().HasQueryFilter(entity =>
            !ApplyTenantFilter || entity.Id == CurrentTenantId);

        modelBuilder.Entity<User>().HasQueryFilter(entity =>
            !ApplyTenantFilter || entity.CompanyId == CurrentTenantId);

        modelBuilder.Entity<Facility>().HasQueryFilter(entity =>
            !ApplyTenantFilter || entity.CompanyId == CurrentTenantId);

        modelBuilder.Entity<Zone>().HasQueryFilter(entity =>
            !ApplyTenantFilter || entity.Facility.CompanyId == CurrentTenantId);

        modelBuilder.Entity<Sensor>().HasQueryFilter(entity =>
            !ApplyTenantFilter || entity.Zone.Facility.CompanyId == CurrentTenantId);

        modelBuilder.Entity<SensorMeasurement>().HasQueryFilter(entity =>
            !ApplyTenantFilter || entity.Sensor.Zone.Facility.CompanyId == CurrentTenantId);

        modelBuilder.Entity<AlertRule>().HasQueryFilter(entity =>
            !ApplyTenantFilter || entity.Sensor.Zone.Facility.CompanyId == CurrentTenantId);

        modelBuilder.Entity<AlertHistory>().HasQueryFilter(entity =>
            !ApplyTenantFilter || entity.Sensor.Zone.Facility.CompanyId == CurrentTenantId);

        modelBuilder.Entity<MaintenanceLog>().HasQueryFilter(entity =>
            !ApplyTenantFilter || entity.Sensor.Zone.Facility.CompanyId == CurrentTenantId);
    }
}
