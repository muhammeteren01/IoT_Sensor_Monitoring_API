using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Application.Settings;
using IoTSensorMonitoring.Application.Simulation;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace IoTSensorMonitoring.Tests.Services.Simulation;

public class SensorSimulationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ISensorRepository> _sensors = new();
    private readonly Mock<ISensorMeasurementRepository> _measurements = new();
    private readonly Mock<IAlertRuleRepository> _rules = new();
    private readonly Mock<IAlertHistoryRepository> _histories = new();
    private readonly Mock<ILogger<SensorSimulationService>> _logger = new();
    private readonly SensorSimulationService _sut;

    public SensorSimulationServiceTests()
    {
        _unitOfWork.SetupGet(unit => unit.Sensors).Returns(_sensors.Object);
        _unitOfWork.SetupGet(unit => unit.SensorMeasurements).Returns(_measurements.Object);
        _unitOfWork.SetupGet(unit => unit.AlertRules).Returns(_rules.Object);
        _unitOfWork.SetupGet(unit => unit.AlertHistories).Returns(_histories.Object);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _sut = new SensorSimulationService(
            _unitOfWork.Object,
            Options.Create(new WorkerSettings { IntervalSeconds = 10, CalibrationWarningDays = 7 }),
            _logger.Object,
            new MeasurementGenerator(new Random(1)));
    }

    [Fact]
    public async Task RunCycleAsync_WhenNoActiveSensors_DoesNotSave()
    {
        _sensors
            .Setup(repository => repository.GetActiveWithDeviceModelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.RunCycleAsync();

        _measurements.Verify(
            repository => repository.AddAsync(It.IsAny<SensorMeasurement>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunCycleAsync_WhenActiveSensor_AddsMeasurement()
    {
        var sensor = CreateSensor("Temperature,Humidity");
        SetupSensorCycle(sensor, previous: null, rules: [], unresolved: []);

        SensorMeasurement? added = null;
        _measurements
            .Setup(repository => repository.AddAsync(It.IsAny<SensorMeasurement>(), It.IsAny<CancellationToken>()))
            .Callback<SensorMeasurement, CancellationToken>((measurement, _) => added = measurement)
            .Returns(Task.CompletedTask);

        await _sut.RunCycleAsync();

        added.Should().NotBeNull();
        added!.SensorId.Should().Be(sensor.Id);
        added.Temperature.Should().NotBeNull();
        added.Humidity.Should().NotBeNull();
        added.Pressure.Should().BeNull();
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunCycleAsync_WhenRuleTriggered_AddsAlertHistory()
    {
        var sensor = CreateSensor("Temperature");
        var rule = new AlertRule
        {
            SensorId = sensor.Id,
            Metric = SensorMetric.Temperature,
            Operator = ComparisonOperator.GreaterThan,
            Threshold = 0,
            Severity = AlertSeverity.Warning,
            IsActive = true
        };
        SetupSensorCycle(sensor, previous: null, rules: [rule], unresolved: []);

        AlertHistory? alert = null;
        _histories
            .Setup(repository => repository.AddAsync(It.IsAny<AlertHistory>(), It.IsAny<CancellationToken>()))
            .Callback<AlertHistory, CancellationToken>((history, _) => alert = history)
            .Returns(Task.CompletedTask);

        await _sut.RunCycleAsync();

        alert.Should().NotBeNull();
        alert!.AlertRuleId.Should().Be(rule.Id);
        alert.SensorId.Should().Be(sensor.Id);
        alert.IsResolved.Should().BeFalse();
        alert.Message.Should().Contain("Temperature");
    }

    [Fact]
    public async Task RunCycleAsync_WhenUnresolvedAlertExists_DoesNotDuplicate()
    {
        var sensor = CreateSensor("Temperature");
        var rule = new AlertRule
        {
            SensorId = sensor.Id,
            Metric = SensorMetric.Temperature,
            Operator = ComparisonOperator.GreaterThan,
            Threshold = 0,
            Severity = AlertSeverity.Critical,
            IsActive = true
        };
        var unresolved = new AlertHistory
        {
            AlertRuleId = rule.Id,
            SensorId = sensor.Id,
            TriggeredValue = 50,
            Message = "open",
            IsResolved = false
        };
        SetupSensorCycle(sensor, previous: null, rules: [rule], unresolved: [unresolved]);

        await _sut.RunCycleAsync();

        _histories.Verify(
            repository => repository.AddAsync(It.IsAny<AlertHistory>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupSensorCycle(
        Sensor sensor,
        SensorMeasurement? previous,
        IReadOnlyList<AlertRule> rules,
        IReadOnlyList<AlertHistory> unresolved)
    {
        _sensors
            .Setup(repository => repository.GetActiveWithDeviceModelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([sensor]);
        _measurements
            .Setup(repository => repository.GetLatestBySensorIdAsync(sensor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previous);
        _measurements
            .Setup(repository => repository.AddAsync(It.IsAny<SensorMeasurement>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _rules
            .Setup(repository => repository.GetActiveBySensorIdAsync(sensor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);
        _histories
            .Setup(repository => repository.GetUnresolvedBySensorIdAsync(sensor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unresolved);
    }

    private static Sensor CreateSensor(string supportedMetrics) =>
        new()
        {
            Name = "Lab sensor",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            Status = SensorStatus.Active,
            DeviceModel = new DeviceModel
            {
                Manufacturer = "Test",
                ModelNumber = "T-1",
                SupportedMetrics = supportedMetrics,
                CalibrationPeriodDays = 90
            }
        };
}
