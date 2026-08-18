using IoTSensorMonitoring.Api.Controllers;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IoTSensorMonitoring.Tests.Controllers.Users;

public class UsersControllerCreateTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly UsersController _sut;

    public UsersControllerCreateTests()
    {
        _sut = new UsersController(_userService.Object);
    }

    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedWithUserDto()
    {
        var request = new CreateUserRequest("Ali", "Veli", "ali@test.com", "Secret1!", Guid.NewGuid(), UserRole.Operator);
        var expected = new UserDto(
            Guid.NewGuid(),
            request.CompanyId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Role,
            true,
            DateTime.UtcNow);
        _userService
            .Setup(service => service.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedResult>().Subject;
        created.Location.Should().Be($"/api/users/{expected.Id}");
        created.Value.Should().BeEquivalentTo(expected);
        _userService.Verify(service => service.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenForbidden_ThrowsForbiddenException()
    {
        var request = new CreateUserRequest("Ali", "Veli", "ali@test.com", "Secret1!", Guid.NewGuid(), UserRole.SuperAdmin);
        _userService
            .Setup(service => service.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Only SuperAdmin can assign the SuperAdmin role."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
