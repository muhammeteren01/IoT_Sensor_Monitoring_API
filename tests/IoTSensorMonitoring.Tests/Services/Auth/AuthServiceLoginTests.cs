using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Domain.Entities;
using FluentAssertions;
using Moq;

namespace IoTSensorMonitoring.Tests.Services.Auth;

public class AuthServiceLoginTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly AuthService _sut;

    public AuthServiceLoginTests()
    {
        _sut = AuthServiceTestHelper.CreateSut(
            _userRepository, _unitOfWork, _passwordService, _tokenService, _currentUser);
    }

    [Fact]
    public async Task LoginAsync_WhenEmailEmpty_ThrowsValidationException()
    {
        var request = AuthServiceTestHelper.ValidLogin(email: "");

        var act = async () => await _sut.LoginAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(exception => exception.Errors.ContainsKey(nameof(LoginRequest.Email)));
        _userRepository.Verify(
            repository => repository.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordEmpty_ThrowsValidationException()
    {
        var request = AuthServiceTestHelper.ValidLogin(password: "");

        var act = async () => await _sut.LoginAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(exception => exception.Errors.ContainsKey(nameof(LoginRequest.Password)));
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ThrowsUnauthorizedException()
    {
        var request = AuthServiceTestHelper.ValidLogin();
        _userRepository
            .Setup(repository => repository.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await _sut.LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Email or password is incorrect.");
        _passwordService.Verify(
            service => service.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordWrong_ThrowsUnauthorizedException()
    {
        var request = AuthServiceTestHelper.ValidLogin();
        var user = AuthServiceTestHelper.CreateUser(email: request.Email);
        _userRepository
            .Setup(repository => repository.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordService
            .Setup(service => service.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(false);

        var act = async () => await _sut.LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Email or password is incorrect.");
    }

    [Fact]
    public async Task LoginAsync_WhenUserInactive_ThrowsUnauthorizedException()
    {
        var request = AuthServiceTestHelper.ValidLogin();
        var user = AuthServiceTestHelper.CreateUser(email: request.Email, isActive: false);
        _userRepository
            .Setup(repository => repository.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordService
            .Setup(service => service.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        var act = async () => await _sut.LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("User account is inactive.");
        _tokenService.Verify(
            service => service.CreateToken(It.IsAny<User>(), out It.Ref<DateTime>.IsAny),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenSuccessful_ReturnsAuthResponse()
    {
        var request = AuthServiceTestHelper.ValidLogin();
        var user = AuthServiceTestHelper.CreateUser(email: request.Email);
        _userRepository
            .Setup(repository => repository.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordService
            .Setup(service => service.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);
        AuthServiceTestHelper.SetupToken(_tokenService, "login-jwt");

        var result = await _sut.LoginAsync(request);

        result.Token.Should().Be("login-jwt");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
        result.UserId.Should().Be(user.Id);
        result.CompanyId.Should().Be(user.CompanyId);
        result.Email.Should().Be(user.Email);
        result.FirstName.Should().Be(user.FirstName);
        result.LastName.Should().Be(user.LastName);
        result.Role.Should().Be(user.Role);
        _tokenService.Verify(
            service => service.CreateToken(user, out It.Ref<DateTime>.IsAny),
            Times.Once);
    }
}
