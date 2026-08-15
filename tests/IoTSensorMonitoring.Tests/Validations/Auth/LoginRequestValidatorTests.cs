using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Validations.Auth;
using FluentAssertions;

namespace IoTSensorMonitoring.Tests.Validations.Auth;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _sut = new();

    [Fact]
    public void Validate_WhenRequestValid_Succeeds()
    {
        var result = _sut.Validate(new LoginRequest("ali@test.com", "Secret1!"));

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenEmailEmpty_ReturnsError(string email)
    {
        var result = _sut.Validate(new LoginRequest(email, "Secret1!"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(LoginRequest.Email) &&
            error.ErrorMessage == "Email is required.");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("ali@")]
    [InlineData("@test.com")]
    public void Validate_WhenEmailFormatInvalid_ReturnsError(string email)
    {
        var result = _sut.Validate(new LoginRequest(email, "Secret1!"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(LoginRequest.Email) &&
            error.ErrorMessage == "A valid email is required.");
    }

    [Fact]
    public void Validate_WhenPasswordEmpty_ReturnsError()
    {
        var result = _sut.Validate(new LoginRequest("ali@test.com", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(LoginRequest.Password) &&
            error.ErrorMessage == "Password is required.");
    }

    [Fact]
    public void Validate_WhenPasswordShort_IsValidUnderLoginRules()
    {
        var result = _sut.Validate(new LoginRequest("ali@test.com", "123"));

        result.IsValid.Should().BeTrue();
    }
}
