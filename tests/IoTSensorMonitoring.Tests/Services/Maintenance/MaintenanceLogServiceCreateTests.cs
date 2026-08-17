using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Application.Validations.Maintenance;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Moq;

namespace IoTSensorMonitoring.Tests.Services.Maintenance;

public class MaintenanceLogServiceCreateTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ISensorRepository> _sensors = new();
    private readonly Mock<IMaintenanceLogRepository> _maintenanceLogs = new();
    private readonly Mock<ISensorMeasurementRepository> _measurements = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly MaintenanceLogService _sut;

    public MaintenanceLogServiceCreateTests()
    {
        _unitOfWork.SetupGet(unit => unit.Sensors).Returns(_sensors.Object);
        _unitOfWork.SetupGet(unit => unit.MaintenanceLogs).Returns(_maintenanceLogs.Object);
        _unitOfWork.SetupGet(unit => unit.SensorMeasurements).Returns(_measurements.Object);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(true);

        _sut = new MaintenanceLogService(
            _unitOfWork.Object,
            _currentUser.Object,
            new CreateMaintenanceLogRequestValidator());
    }

    [Fact]
    public async Task CreateAsync_WhenCalibration_SetsNextDueDateFromDeviceModelPeriod()
    {
        var performedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var sensor = CreateSensor(calibrationPeriodDays: 180, supportedMetrics: "Temperature");
        _sensors
            .Setup(repository => repository.GetByIdWithDetailsAsync(sensor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor);

        MaintenanceLog? added = null;
        _maintenanceLogs
            .Setup(repository => repository.AddAsync(It.IsAny<MaintenanceLog>(), It.IsAny<CancellationToken>()))
            .Callback<MaintenanceLog, CancellationToken>((log, _) => added = log)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(new CreateMaintenanceLogRequest(
            sensor.Id,
            MaintenanceActionType.Calibration,
            "Annual calibration",
            performedAt,
            NextDueDate: performedAt.AddDays(30)));

        sensor.LastCalibrationDate.Should().Be(performedAt);
        added.Should().NotBeNull();
        added!.NextDueDate.Should().Be(performedAt.AddDays(180));
        result.NextDueDate.Should().Be(performedAt.AddDays(180));
        _sensors.Verify(repository => repository.Update(sensor), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenBatteryReplacement_AddsMeasurementWithFullBattery()
    {
        var performedAt = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc);
        var sensor = CreateSensor(calibrationPeriodDays: 90, supportedMetrics: "Temperature,BatteryLevel");
        _sensors
            .Setup(repository => repository.GetByIdWithDetailsAsync(sensor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor);
        _measurements
            .Setup(repository => repository.GetLatestBySensorIdAsync(sensor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SensorMeasurement
            {
                SensorId = sensor.Id,
                Temperature = 22.5m,
                BatteryLevel = 12m,
                MeasurementDate = performedAt.AddHours(-1)
            });
        _maintenanceLogs
            .Setup(repository => repository.AddAsync(It.IsAny<MaintenanceLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SensorMeasurement? addedMeasurement = null;
        _measurements
            .Setup(repository => repository.AddAsync(It.IsAny<SensorMeasurement>(), It.IsAny<CancellationToken>()))
            .Callback<SensorMeasurement, CancellationToken>((measurement, _) => addedMeasurement = measurement)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateMaintenanceLogRequest(
            sensor.Id,
            MaintenanceActionType.BatteryReplacement,
            "Battery swap",
            performedAt,
            NextDueDate: null));

        addedMeasurement.Should().NotBeNull();
        addedMeasurement!.BatteryLevel.Should().Be(100m);
        addedMeasurement.Temperature.Should().Be(22.5m);
        addedMeasurement.MeasurementDate.Should().BeOnOrAfter(performedAt);
    }

    [Fact]
    public async Task CreateAsync_WhenBatteryReplacementWithoutBatteryMetric_StillAddsFullBatteryMeasurement()
    {
        var sensor = CreateSensor(calibrationPeriodDays: 90, supportedMetrics: "Temperature");
        _sensors
            .Setup(repository => repository.GetByIdWithDetailsAsync(sensor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sensor);
        _maintenanceLogs
            .Setup(repository => repository.AddAsync(It.IsAny<MaintenanceLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SensorMeasurement? addedMeasurement = null;
        _measurements
            .Setup(repository => repository.AddAsync(It.IsAny<SensorMeasurement>(), It.IsAny<CancellationToken>()))
            .Callback<SensorMeasurement, CancellationToken>((measurement, _) => addedMeasurement = measurement)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateMaintenanceLogRequest(
            sensor.Id,
            MaintenanceActionType.BatteryReplacement,
            "Battery swap",
            DateTime.UtcNow,
            NextDueDate: null));

        addedMeasurement.Should().NotBeNull();
        addedMeasurement!.BatteryLevel.Should().Be(100m);
    }

    private static Sensor CreateSensor(int calibrationPeriodDays, string supportedMetrics)
    {
        var companyId = Guid.NewGuid();
        return new Sensor
        {
            Name = "Line sensor",
            MacAddress = "AA:BB:CC:DD:EE:01",
            Zone = new Zone
            {
                Name = "Zone A",
                Facility = new Facility
                {
                    Name = "Plant",
                    CompanyId = companyId,
                    Company = new Company { Name = "Acme" }
                }
            },
            DeviceModel = new DeviceModel
            {
                CompanyId = companyId,
                Manufacturer = "Bosch",
                ModelNumber = "BME680",
                SupportedMetrics = supportedMetrics,
                CalibrationPeriodDays = calibrationPeriodDays
            }
        };
    }
}
