using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Validations.Sensors;
using FluentAssertions;

namespace IoTSensorMonitoring.Tests.Validations.Sensors;

public class CreateSensorRequestValidatorTests
{
    private readonly CreateSensorRequestValidator _sut = new();

    [Fact]
    public void Validate_WhenRequestValid_Succeeds()
    {
        var result = _sut.Validate(new CreateSensorRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Boiler-Temp-01",
            "AA:BB:CC:DD:EE:01",
            "1.0.0"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenMacEmpty_ReturnsError()
    {
        var result = _sut.Validate(new CreateSensorRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Boiler-Temp-01",
            "",
            null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateSensorRequest.MacAddress));
    }

    [Fact]
    public void Validate_WhenZoneIdEmpty_ReturnsError()
    {
        var result = _sut.Validate(new CreateSensorRequest(
            Guid.Empty,
            Guid.NewGuid(),
            "Boiler-Temp-01",
            "AA:BB:CC:DD:EE:01",
            null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateSensorRequest.ZoneId));
    }
}
