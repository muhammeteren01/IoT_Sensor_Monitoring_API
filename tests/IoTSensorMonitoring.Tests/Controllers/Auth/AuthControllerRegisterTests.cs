using IoTSensorMonitoring.Api.Controllers;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IoTSensorMonitoring.Tests.Controllers.Auth;

public class AuthControllerRegisterTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly AuthController _sut;

    public AuthControllerRegisterTests()
    {
        _sut = new AuthController(_authService.Object);
    }

    [Fact]
    public async Task Register_WhenServiceSucceeds_ReturnsOkWithAuthResponse()
    {
        var request = new RegisterRequest("Ali", "Veli", "ali@test.com", "Secret1!", Guid.NewGuid(), UserRole.Operator);
        var expected = AuthTestHelper.CreateAuthResponse(request.Email, UserRole.Operator, request.CompanyId);
        _authService
            .Setup(service => service.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Register(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _authService.Verify(service => service.RegisterAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WhenForbidden_ThrowsForbiddenException()
    {
        var request = new RegisterRequest("Ali", "Veli", "ali@test.com", "Secret1!", Guid.NewGuid(), UserRole.SuperAdmin);
        _authService
            .Setup(service => service.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Only SuperAdmin can assign the SuperAdmin role."));

        var act = async () => await _sut.Register(request, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
