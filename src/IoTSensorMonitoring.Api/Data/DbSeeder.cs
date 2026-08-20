using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Settings;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using IoTSensorMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IoTSensorMonitoring.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<SeedSettings>>().Value;

        if (!settings.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
        {
            throw new InvalidOperationException(
                "SeedSettings.Enabled=true requires Email and Password.");
        }

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var passwordHash = passwordService.HashPassword(settings.Password);

        await SeedSuperAdminAsync(db, settings, passwordHash, logger, cancellationToken);
        await SeedDemoCatalogAsync(db, passwordHash, logger, cancellationToken);
    }

    public static Task SeedSuperAdminAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default) =>
        SeedAsync(services, cancellationToken);

    private static async Task SeedSuperAdminAsync(
        AppDbContext db,
        SeedSettings settings,
        string passwordHash,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var exists = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(user => user.Email == settings.Email, cancellationToken);

        if (exists)
        {
            logger.LogInformation("SuperAdmin seed skipped; email already exists: {Email}", settings.Email);
            return;
        }

        db.Users.Add(new User
        {
            CompanyId = null,
            FirstName = settings.FirstName,
            LastName = settings.LastName,
            Email = settings.Email.Trim(),
            PasswordHash = passwordHash,
            Role = UserRole.SuperAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("SuperAdmin seed completed: {Email}", settings.Email);
    }

    private static async Task SeedDemoCatalogAsync(
        AppDbContext db,
        string passwordHash,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        await SeedNovaEnerjiAsync(db, passwordHash, logger, cancellationToken);
        await SeedAtlasLojistikAsync(db, passwordHash, logger, cancellationToken);
    }

    private static async Task SeedNovaEnerjiAsync(
        AppDbContext db,
        string passwordHash,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        const string contactEmail = "nova@iot.local";
        if (await CompanyExistsAsync(db, contactEmail, cancellationToken))
        {
            logger.LogInformation("Demo seed skipped; company already exists: {Email}", contactEmail);
            return;
        }

        var company = NewCompany("Nova Enerji", contactEmail);
        var facility = NewFacility(company.Id, "İstanbul Fabrikası", "İstanbul", "Organize Sanayi Bölgesi, Tuzla", 3);
        var production = NewZone(facility.Id, "Üretim Hattı", 1);
        var coldStorage = NewZone(facility.Id, "Soğuk Hava", 0);
        var roof = NewZone(facility.Id, "Çatı Odası", 3);
        var workshop = NewZone(facility.Id, "Bakım Atölyesi", 1);
        var th200 = NewDeviceModel(company.Id, "Bosch", "TH-200", "Temperature,Humidity,BatteryLevel", 90);
        var p410 = NewDeviceModel(company.Id, "Siemens", "P-410", "Pressure,SignalStrength", null);

        var temp = NewSensor(production.Id, th200.Id, "TEMP-NOVA-01", "00:1A:2B:3C:4D:01", SensorStatus.Active, DateTime.UtcNow.AddDays(-85));
        var humidity = NewSensor(coldStorage.Id, th200.Id, "HUM-NOVA-02", "00:1A:2B:3C:4D:02", SensorStatus.Active, DateTime.UtcNow.AddDays(-20));
        var pressure = NewSensor(roof.Id, p410.Id, "PRES-NOVA-03", "00:1A:2B:3C:4D:03", SensorStatus.Active, DateTime.UtcNow.AddDays(-10));
        var battery = NewSensor(workshop.Id, th200.Id, "BAT-NOVA-04", "00:1A:2B:3C:4D:04", SensorStatus.Maintenance, DateTime.UtcNow.AddDays(-5));

        db.Companies.Add(company);
        db.Facilities.Add(facility);
        db.Zones.AddRange(production, coldStorage, roof, workshop);
        db.DeviceModels.AddRange(th200, p410);
        db.Sensors.AddRange(temp, humidity, pressure, battery);
        db.Users.AddRange(
            NewUser(company.Id, "Ayşe", "Kaya", "ayse.kaya@nova.local", passwordHash, UserRole.CompanyAdmin),
            NewUser(company.Id, "Mehmet", "Demir", "mehmet.demir@nova.local", passwordHash, UserRole.Operator));
        db.AlertRules.AddRange(
            NewRule(temp.Id, SensorMetric.Temperature, ComparisonOperator.GreaterThan, 35m, AlertSeverity.Critical),
            NewRule(humidity.Id, SensorMetric.Humidity, ComparisonOperator.GreaterThan, 80m, AlertSeverity.Warning),
            NewRule(pressure.Id, SensorMetric.Pressure, ComparisonOperator.LessThan, 990m, AlertSeverity.Warning));

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Demo seed completed: {Company}", company.Name);
    }

    private static async Task SeedAtlasLojistikAsync(
        AppDbContext db,
        string passwordHash,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        const string contactEmail = "atlas@iot.local";
        if (await CompanyExistsAsync(db, contactEmail, cancellationToken))
        {
            logger.LogInformation("Demo seed skipped; company already exists: {Email}", contactEmail);
            return;
        }

        var company = NewCompany("Atlas Lojistik", contactEmail);
        var facility = NewFacility(company.Id, "Ankara Depo", "Ankara", "ASO 1. OSB, Sincan", 2);
        var dispatch = NewZone(facility.Id, "Sevkiyat", 1);
        var packing = NewZone(facility.Id, "Paketleme", 1);
        var archive = NewZone(facility.Id, "Arşiv", 2);
        var ht90 = NewDeviceModel(company.Id, "Honeywell", "HT-90", "Temperature,Humidity", 180);

        var temp = NewSensor(dispatch.Id, ht90.Id, "TEMP-ATL-01", "00:2B:3C:4D:5E:01", SensorStatus.Active, DateTime.UtcNow.AddDays(-30));
        var signal = NewSensor(packing.Id, ht90.Id, "SIG-ATL-02", "00:2B:3C:4D:5E:02", SensorStatus.Active, DateTime.UtcNow.AddDays(-12));
        var humidity = NewSensor(archive.Id, ht90.Id, "HUM-ATL-03", "00:2B:3C:4D:5E:03", SensorStatus.Inactive, DateTime.UtcNow.AddDays(-40));

        db.Companies.Add(company);
        db.Facilities.Add(facility);
        db.Zones.AddRange(dispatch, packing, archive);
        db.DeviceModels.Add(ht90);
        db.Sensors.AddRange(temp, signal, humidity);
        db.Users.AddRange(
            NewUser(company.Id, "Elif", "Yıldız", "elif.yildiz@atlas.local", passwordHash, UserRole.CompanyAdmin),
            NewUser(company.Id, "Can", "Öz", "can.oz@atlas.local", passwordHash, UserRole.Operator));
        db.AlertRules.Add(
            NewRule(temp.Id, SensorMetric.Temperature, ComparisonOperator.GreaterThan, 30m, AlertSeverity.Warning));

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Demo seed completed: {Company}", company.Name);
    }

    private static Task<bool> CompanyExistsAsync(
        AppDbContext db,
        string contactEmail,
        CancellationToken cancellationToken) =>
        db.Companies.IgnoreQueryFilters().AnyAsync(company => company.ContactEmail == contactEmail, cancellationToken);

    private static Company NewCompany(string name, string contactEmail) =>
        new()
        {
            Name = name,
            ContactEmail = contactEmail,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    private static Facility NewFacility(Guid companyId, string name, string city, string address, int floorCount) =>
        new()
        {
            CompanyId = companyId,
            Name = name,
            City = city,
            Address = address,
            FloorCount = floorCount
        };

    private static Zone NewZone(Guid facilityId, string name, int floorLevel) =>
        new()
        {
            FacilityId = facilityId,
            Name = name,
            FloorLevel = floorLevel
        };

    private static DeviceModel NewDeviceModel(
        Guid companyId,
        string manufacturer,
        string modelNumber,
        string supportedMetrics,
        int? calibrationPeriodDays) =>
        new()
        {
            CompanyId = companyId,
            Manufacturer = manufacturer,
            ModelNumber = modelNumber,
            SupportedMetrics = supportedMetrics,
            CalibrationPeriodDays = calibrationPeriodDays
        };

    private static Sensor NewSensor(
        Guid zoneId,
        Guid deviceModelId,
        string name,
        string macAddress,
        SensorStatus status,
        DateTime lastCalibrationDate) =>
        new()
        {
            ZoneId = zoneId,
            DeviceModelId = deviceModelId,
            Name = name,
            MacAddress = macAddress,
            FirmwareVersion = "1.2.0",
            Status = status,
            LastCalibrationDate = lastCalibrationDate,
            CreatedAt = DateTime.UtcNow
        };

    private static User NewUser(
        Guid companyId,
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        UserRole role) =>
        new()
        {
            CompanyId = companyId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    private static AlertRule NewRule(
        Guid sensorId,
        SensorMetric metric,
        ComparisonOperator comparisonOperator,
        decimal threshold,
        AlertSeverity severity) =>
        new()
        {
            SensorId = sensorId,
            Metric = metric,
            Operator = comparisonOperator,
            Threshold = threshold,
            Severity = severity,
            IsActive = true
        };
}
