using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Moq;

namespace IoTSensorMonitoring.Tests.Services.Auth;

public class AuthServiceRegisterTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly AuthService _sut;

    public AuthServiceRegisterTests()
    {
        _sut = AuthServiceTestHelper.CreateSut(
            _userRepository, _unitOfWork, _passwordService, _tokenService, _currentUser);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailEmpty_ThrowsValidationException()
    {
        var request = AuthServiceTestHelper.ValidRegister(email: "");

        var act = async () => await _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(exception => exception.Errors.ContainsKey(nameof(RegisterRequest.Email)));
        _userRepository.Verify(
            repository => repository.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenFirstNameEmpty_ThrowsValidationException()
    {
        var request = AuthServiceTestHelper.ValidRegister(firstName: "");

        var act = async () => await _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RegisterAsync_WhenPasswordShort_ThrowsValidationException()
    {
        var request = AuthServiceTestHelper.ValidRegister(password: "12345");

        var act = async () => await _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(exception => exception.Errors[nameof(RegisterRequest.Password)]
                .Contains("Password must be at least 6 characters."));
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyRegistered_ThrowsConflictException()
    {
        var companyId = Guid.NewGuid();
        AuthServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = AuthServiceTestHelper.ValidRegister(companyId: companyId);
        _userRepository
            .Setup(repository => repository.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthServiceTestHelper.CreateUser(email: request.Email));

        var act = async () => await _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("This email is already registered.");
        _userRepository.Verify(
            repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenSuccessful_HashesAddsSavesAndReturnsAuthResponse()
    {
        var companyId = Guid.NewGuid();
        AuthServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = AuthServiceTestHelper.ValidRegister(role: UserRole.Operator, companyId: null);
        _userRepository
            .Setup(repository => repository.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordService
            .Setup(service => service.HashPassword(request.Password))
            .Returns("hashed-password");
        AuthServiceTestHelper.SetupToken(_tokenService, "register-jwt");

        User? added = null;
        _userRepository
            .Setup(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => added = user)
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _sut.RegisterAsync(request);

        added.Should().NotBeNull();
        added!.Email.Should().Be(request.Email);
        added.FirstName.Should().Be(request.FirstName);
        added.LastName.Should().Be(request.LastName);
        added.PasswordHash.Should().Be("hashed-password");
        added.Role.Should().Be(UserRole.Operator);
        added.CompanyId.Should().Be(companyId);
        added.IsActive.Should().BeTrue();

        result.Token.Should().Be("register-jwt");
        result.Email.Should().Be(request.Email);
        result.CompanyId.Should().Be(companyId);
        result.Role.Should().Be(UserRole.Operator);
        result.UserId.Should().Be(added.Id);

        _passwordService.Verify(service => service.HashPassword(request.Password), Times.Once);
        _userRepository.Verify(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenSuperAdminRole_CompanyIdIsNull()
    {
        AuthServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var request = AuthServiceTestHelper.ValidRegister(role: UserRole.SuperAdmin, companyId: null);
        _userRepository
            .Setup(repository => repository.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordService.Setup(service => service.HashPassword(request.Password)).Returns("hash");
        AuthServiceTestHelper.SetupToken(_tokenService);

        User? added = null;
        _userRepository
            .Setup(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => added = user)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.RegisterAsync(request);

        added!.CompanyId.Should().BeNull();
        result.CompanyId.Should().BeNull();
        result.Role.Should().Be(UserRole.SuperAdmin);
    }

    [Fact]
    public async Task RegisterAsync_WhenCompanyAdminAssignsSuperAdmin_ThrowsForbiddenException()
    {
        AuthServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, Guid.NewGuid());
        var request = AuthServiceTestHelper.ValidRegister(role: UserRole.SuperAdmin);

        var act = async () => await _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Only SuperAdmin can assign the SuperAdmin role.");
        _userRepository.Verify(
            repository => repository.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
