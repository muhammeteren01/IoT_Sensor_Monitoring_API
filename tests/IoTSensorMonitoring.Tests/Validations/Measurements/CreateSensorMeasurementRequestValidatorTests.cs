using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Validations.Measurements;
using FluentAssertions;

namespace IoTSensorMonitoring.Tests.Validations.Measurements;

public class CreateSensorMeasurementRequestValidatorTests
{
    private readonly CreateSensorMeasurementRequestValidator _sut = new();

    [Fact]
    public void Validate_WhenTemperatureProvided_Succeeds()
    {
        var result = _sut.Validate(new CreateSensorMeasurementRequest(
            Guid.NewGuid(), 22.5m, null, null, null, null, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenNoMetricProvided_ReturnsError()
    {
        var result = _sut.Validate(new CreateSensorMeasurementRequest(
            Guid.NewGuid(), null, null, null, null, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "At least one measurement value is required.");
    }

    [Fact]
    public void Validate_WhenHumidityOutOfRange_ReturnsError()
    {
        var result = _sut.Validate(new CreateSensorMeasurementRequest(
            Guid.NewGuid(), null, 140, null, null, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateSensorMeasurementRequest.Humidity));
    }

    [Fact]
    public void Validate_WhenSensorIdEmpty_ReturnsError()
    {
        var result = _sut.Validate(new CreateSensorMeasurementRequest(
            Guid.Empty, 22, null, null, null, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateSensorMeasurementRequest.SensorId));
    }
}
