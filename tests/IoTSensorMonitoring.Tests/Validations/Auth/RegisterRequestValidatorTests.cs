using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Validations.Auth;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;

namespace IoTSensorMonitoring.Tests.Validations.Auth;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _sut = new();

    private static RegisterRequest ValidRequest(
        string firstName = "Ali",
        string lastName = "Veli",
        string email = "ali@test.com",
        string password = "Secret1!",
        UserRole role = UserRole.Operator) =>
        new(firstName, lastName, email, password, Guid.NewGuid(), role);

    [Fact]
    public void Validate_WhenRequestValid_Succeeds()
    {
        var result = _sut.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WhenFirstNameEmpty_ReturnsError()
    {
        var result = _sut.Validate(ValidRequest(firstName: ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(RegisterRequest.FirstName) &&
            error.ErrorMessage == "First name is required.");
    }

    [Fact]
    public void Validate_WhenEmailFormatInvalid_ReturnsError()
    {
        var result = _sut.Validate(ValidRequest(email: "not-an-email"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(RegisterRequest.Email) &&
            error.ErrorMessage == "A valid email is required.");
    }

    [Fact]
    public void Validate_WhenPasswordShort_ReturnsError()
    {
        var result = _sut.Validate(ValidRequest(password: "12345"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(RegisterRequest.Password) &&
            error.ErrorMessage == "Password must be at least 6 characters.");
    }

    [Fact]
    public void Validate_WhenPasswordExactly6Chars_Succeeds()
    {
        var result = _sut.Validate(ValidRequest(password: "123456"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenRoleInvalid_ReturnsError()
    {
        var result = _sut.Validate(ValidRequest(role: (UserRole)999));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(RegisterRequest.Role) &&
            error.ErrorMessage == "Invalid user role.");
    }
}
