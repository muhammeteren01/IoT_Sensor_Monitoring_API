using IoTSensorMonitoring.Application.Simulation;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;

namespace IoTSensorMonitoring.Tests.Simulation;

public class AlertRuleEvaluatorTests
{
    [Theory]
    [InlineData(ComparisonOperator.GreaterThan, 41, 40, true)]
    [InlineData(ComparisonOperator.GreaterThan, 40, 40, false)]
    [InlineData(ComparisonOperator.GreaterOrEqual, 40, 40, true)]
    [InlineData(ComparisonOperator.LessThan, 10, 15, true)]
    [InlineData(ComparisonOperator.LessOrEqual, 15, 15, true)]
    [InlineData(ComparisonOperator.Equal, 20, 20, true)]
    [InlineData(ComparisonOperator.Equal, 20, 21, false)]
    public void IsTriggered_MatchesOperator(ComparisonOperator comparison, int value, int threshold, bool expected)
    {
        AlertRuleEvaluator.IsTriggered(comparison, value, threshold).Should().Be(expected);
    }

    [Fact]
    public void ReadValue_ReturnsMetricFromMeasurement()
    {
        var measurement = new SensorMeasurement
        {
            SensorId = Guid.NewGuid(),
            Temperature = 22.5m,
            Humidity = 40m,
            SignalStrength = -60
        };

        AlertRuleEvaluator.ReadValue(measurement, SensorMetric.Temperature).Should().Be(22.5m);
        AlertRuleEvaluator.ReadValue(measurement, SensorMetric.Humidity).Should().Be(40m);
        AlertRuleEvaluator.ReadValue(measurement, SensorMetric.Pressure).Should().BeNull();
        AlertRuleEvaluator.ReadValue(measurement, SensorMetric.SignalStrength).Should().Be(-60);
    }

    [Fact]
    public void FormatMessage_IncludesMetricAndValue()
    {
        var message = AlertRuleEvaluator.FormatMessage(SensorMetric.Temperature, ComparisonOperator.GreaterThan, 40, 41.2m);

        message.Should().Be("Temperature > 40 (value: 41.2)");
    }
}
