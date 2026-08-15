using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Validations.DeviceModels;
using FluentAssertions;

namespace IoTSensorMonitoring.Tests.Validations.DeviceModels;

public class CreateDeviceModelRequestValidatorTests
{
    private readonly CreateDeviceModelRequestValidator _sut = new();

    private static CreateDeviceModelRequest ValidRequest(
        string manufacturer = "Siemens",
        string modelNumber = "SITRANS-T",
        string supportedMetrics = "Temperature,Humidity,Pressure",
        int? calibrationPeriodDays = 180) =>
        new(manufacturer, modelNumber, supportedMetrics, calibrationPeriodDays);

    [Fact]
    public void Validate_WhenRequestValid_Succeeds()
    {
        var result = _sut.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenSupportedMetricsUnknown_ReturnsError()
    {
        var result = _sut.Validate(ValidRequest(supportedMetrics: "Temperature,Light"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateDeviceModelRequest.SupportedMetrics) &&
            error.ErrorMessage.Contains("known metrics"));
    }

    [Fact]
    public void Validate_WhenSupportedMetricsEmpty_ReturnsError()
    {
        var result = _sut.Validate(ValidRequest(supportedMetrics: ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateDeviceModelRequest.SupportedMetrics));
    }

    [Fact]
    public void Validate_WhenCalibrationPeriodZero_ReturnsError()
    {
        var result = _sut.Validate(ValidRequest(calibrationPeriodDays: 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(CreateDeviceModelRequest.CalibrationPeriodDays));
    }
}
