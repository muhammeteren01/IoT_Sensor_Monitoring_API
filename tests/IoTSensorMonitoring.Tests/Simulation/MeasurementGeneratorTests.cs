using IoTSensorMonitoring.Application.Simulation;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;

namespace IoTSensorMonitoring.Tests.Simulation;

public class MeasurementGeneratorTests
{
    [Fact]
    public void Next_OnlyFillsSupportedMetrics()
    {
        var generator = new MeasurementGenerator(new Random(1));
        var metrics = new HashSet<SensorMetric> { SensorMetric.Temperature, SensorMetric.BatteryLevel };

        var measurement = generator.Next(Guid.NewGuid(), previous: null, metrics);

        measurement.Temperature.Should().NotBeNull();
        measurement.BatteryLevel.Should().NotBeNull();
        measurement.Humidity.Should().BeNull();
        measurement.Pressure.Should().BeNull();
        measurement.SignalStrength.Should().BeNull();
    }

    [Fact]
    public void Next_BatteryDoesNotIncrease()
    {
        var generator = new MeasurementGenerator(new Random(42));
        var previous = new SensorMeasurement
        {
            SensorId = Guid.NewGuid(),
            BatteryLevel = 80m
        };

        var measurement = generator.Next(previous.SensorId, previous, new HashSet<SensorMetric> { SensorMetric.BatteryLevel });

        measurement.BatteryLevel.Should().BeLessThanOrEqualTo(80m);
        measurement.BatteryLevel.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void Next_WalksFromPreviousTemperature()
    {
        var generator = new MeasurementGenerator(new Random(7));
        var previous = new SensorMeasurement
        {
            SensorId = Guid.NewGuid(),
            Temperature = 22.00m
        };

        var measurement = generator.Next(
            previous.SensorId,
            previous,
            new HashSet<SensorMetric> { SensorMetric.Temperature });

        measurement.Temperature.Should().NotBeNull();
        measurement.Temperature.Should().BeInRange(10m, 45m);
        Math.Abs(measurement.Temperature!.Value - 22.00m).Should().BeLessThanOrEqualTo(0.4m);
    }
}
