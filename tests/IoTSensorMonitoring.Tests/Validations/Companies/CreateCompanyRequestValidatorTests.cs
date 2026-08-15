using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Validations.Companies;
using FluentAssertions;

namespace IoTSensorMonitoring.Tests.Validations.Companies;

public class CreateCompanyRequestValidatorTests
{
    private readonly CreateCompanyRequestValidator _sut = new();

    [Fact]
    public void Validate_WhenRequestValid_Succeeds()
    {
        var result = _sut.Validate(new CreateCompanyRequest("Acme", "info@acme.test"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameEmpty_ReturnsError(string name)
    {
        var result = _sut.Validate(new CreateCompanyRequest(name, "info@acme.test"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCompanyRequest.Name));
    }

    [Fact]
    public void Validate_WhenNameTooLong_ReturnsError()
    {
        var result = _sut.Validate(new CreateCompanyRequest(new string('A', 201), null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCompanyRequest.Name));
    }

    [Fact]
    public void Validate_WhenEmailInvalid_ReturnsError()
    {
        var result = _sut.Validate(new CreateCompanyRequest("Acme", "not-an-email"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCompanyRequest.ContactEmail));
    }

    [Fact]
    public void Validate_WhenEmailNull_Succeeds()
    {
        var result = _sut.Validate(new CreateCompanyRequest("Acme", null));

        result.IsValid.Should().BeTrue();
    }
}
