using IoTSensorMonitoring.Api.Controllers;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IoTSensorMonitoring.Tests.Controllers.Auth;

public class AuthControllerLoginTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly AuthController _sut;

    public AuthControllerLoginTests()
    {
        _sut = new AuthController(_authService.Object);
    }

    [Fact]
    public async Task Login_WhenServiceSucceeds_ReturnsOkWithAuthResponse()
    {
        var request = new LoginRequest("ali@test.com", "Secret1!");
        var expected = AuthTestHelper.CreateAuthResponse(request.Email, UserRole.CompanyAdmin, Guid.NewGuid());
        _authService
            .Setup(service => service.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Login(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _authService.Verify(service => service.LoginAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WhenSuperAdmin_CompanyIdNull_ReturnsOk()
    {
        var request = new LoginRequest("root@test.com", "Secret1!");
        var expected = AuthTestHelper.CreateAuthResponse(request.Email, UserRole.SuperAdmin, companyId: null);
        _authService
            .Setup(service => service.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Login(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((AuthResponse)ok.Value!).CompanyId.Should().BeNull();
        ((AuthResponse)ok.Value!).Role.Should().Be(UserRole.SuperAdmin);
    }

    [Fact]
    public async Task Login_WhenCredentialsInvalid_ThrowsUnauthorizedException()
    {
        var request = new LoginRequest("ali@test.com", "yanlis");
        _authService
            .Setup(service => service.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedException("Email or password is incorrect."));

        var act = async () => await _sut.Login(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Email or password is incorrect.");
    }
}
