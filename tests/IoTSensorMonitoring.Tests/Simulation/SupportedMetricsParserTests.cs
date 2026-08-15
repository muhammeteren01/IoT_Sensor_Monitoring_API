using IoTSensorMonitoring.Application.Simulation;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;

namespace IoTSensorMonitoring.Tests.Simulation;

public class SupportedMetricsParserTests
{
    [Fact]
    public void Parse_WhenCommaSeparated_ReturnsKnownMetrics()
    {
        var metrics = SupportedMetricsParser.Parse("Temperature, Humidity, Pressure");

        metrics.Should().BeEquivalentTo(
        [
            SensorMetric.Temperature,
            SensorMetric.Humidity,
            SensorMetric.Pressure
        ]);
    }

    [Fact]
    public void Parse_WhenEmpty_ReturnsEmptySet()
    {
        SupportedMetricsParser.Parse("").Should().BeEmpty();
        SupportedMetricsParser.Parse(null).Should().BeEmpty();
    }

    [Fact]
    public void Parse_WhenUnknownToken_IgnoresIt()
    {
        var metrics = SupportedMetricsParser.Parse("Temperature,Light,BatteryLevel");

        metrics.Should().BeEquivalentTo(
        [
            SensorMetric.Temperature,
            SensorMetric.BatteryLevel
        ]);
    }
}
