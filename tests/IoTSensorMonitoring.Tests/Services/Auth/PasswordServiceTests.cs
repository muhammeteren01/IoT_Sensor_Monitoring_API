using IoTSensorMonitoring.Application.Services;
using FluentAssertions;

namespace IoTSensorMonitoring.Tests.Services.Auth;

public class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    [Fact]
    public void HashPassword_ThenVerify_ReturnsTrue()
    {
        var hash = _sut.HashPassword("Secret1!");

        hash.Should().NotBe("Secret1!");
        _sut.VerifyPassword("Secret1!", hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WhenWrong_ReturnsFalse()
    {
        var hash = _sut.HashPassword("Secret1!");

        _sut.VerifyPassword("wrong", hash).Should().BeFalse();
    }
}
