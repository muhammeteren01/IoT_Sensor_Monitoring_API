using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Validations.Alerts;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;

namespace IoTSensorMonitoring.Tests.Validations.Alerts;

public class CreateAlertRuleRequestValidatorTests
{
    private readonly CreateAlertRuleRequestValidator _sut = new();

    [Fact]
    public void Validate_WhenRequestValid_Succeeds()
    {
        var result = _sut.Validate(new CreateAlertRuleRequest(
            Guid.NewGuid(),
            SensorMetric.Temperature,
            ComparisonOperator.GreaterThan,
            40,
            AlertSeverity.Warning));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenMetricInvalid_ReturnsError()
    {
        var result = _sut.Validate(new CreateAlertRuleRequest(
            Guid.NewGuid(),
            (SensorMetric)999,
            ComparisonOperator.GreaterThan,
            40,
            AlertSeverity.Warning));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateAlertRuleRequest.Metric));
    }

    [Fact]
    public void Validate_WhenSensorIdEmpty_ReturnsError()
    {
        var result = _sut.Validate(new CreateAlertRuleRequest(
            Guid.Empty,
            SensorMetric.Humidity,
            ComparisonOperator.GreaterThan,
            80,
            AlertSeverity.Critical));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateAlertRuleRequest.SensorId));
    }
}
