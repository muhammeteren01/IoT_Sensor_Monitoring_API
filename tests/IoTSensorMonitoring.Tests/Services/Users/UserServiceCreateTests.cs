using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Application.Validations.Users;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Moq;

namespace IoTSensorMonitoring.Tests.Services.Users;

public class UserServiceCreateTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly UserService _sut;

    public UserServiceCreateTests()
    {
        _unitOfWork.SetupGet(unit => unit.Users).Returns(_users.Object);
        _sut = new UserService(
            _unitOfWork.Object,
            _currentUser.Object,
            _passwordService.Object,
            new CreateUserRequestValidator());
    }

    [Fact]
    public async Task CreateAsync_WhenEmailEmpty_ThrowsValidationException()
    {
        var request = ValidRequest(email: "");

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(exception => exception.Errors.ContainsKey(nameof(CreateUserRequest.Email)));
        _users.Verify(
            repository => repository.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenFirstNameEmpty_ThrowsValidationException()
    {
        var request = ValidRequest(firstName: "");

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_WhenPasswordShort_ThrowsValidationException()
    {
        var request = ValidRequest(password: "12345");

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(exception => exception.Errors[nameof(CreateUserRequest.Password)]
                .Contains("Password must be at least 6 characters."));
    }

    [Fact]
    public async Task CreateAsync_WhenEmailAlreadyInUse_ThrowsConflictException()
    {
        var companyId = Guid.NewGuid();
        SetupCompanyAdmin(companyId);
        var request = ValidRequest(companyId: companyId);
        _users
            .Setup(repository => repository.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Email = request.Email,
                FirstName = "Ali",
                LastName = "Veli",
                PasswordHash = "hash",
                Role = UserRole.Operator
            });

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("This email is already in use.");
        _users.Verify(
            repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSuccessful_HashesAddsSavesAndReturnsUserDto()
    {
        var companyId = Guid.NewGuid();
        SetupCompanyAdmin(companyId);
        var request = ValidRequest(role: UserRole.Operator, companyId: null);
        _users
            .Setup(repository => repository.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordService
            .Setup(service => service.HashPassword(request.Password))
            .Returns("hashed-password");

        User? added = null;
        _users
            .Setup(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => added = user)
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added.Should().NotBeNull();
        added!.Email.Should().Be(request.Email);
        added.FirstName.Should().Be(request.FirstName);
        added.LastName.Should().Be(request.LastName);
        added.PasswordHash.Should().Be("hashed-password");
        added.Role.Should().Be(UserRole.Operator);
        added.CompanyId.Should().Be(companyId);
        added.IsActive.Should().BeTrue();

        result.Email.Should().Be(request.Email);
        result.CompanyId.Should().Be(companyId);
        result.Role.Should().Be(UserRole.Operator);
        result.Id.Should().Be(added.Id);

        _passwordService.Verify(service => service.HashPassword(request.Password), Times.Once);
        _users.Verify(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenSuperAdminRole_CompanyIdIsNull()
    {
        SetupSuperAdmin();
        var request = ValidRequest(role: UserRole.SuperAdmin, companyId: null);
        _users
            .Setup(repository => repository.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordService.Setup(service => service.HashPassword(request.Password)).Returns("hash");

        User? added = null;
        _users
            .Setup(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => added = user)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added!.CompanyId.Should().BeNull();
        result.CompanyId.Should().BeNull();
        result.Role.Should().Be(UserRole.SuperAdmin);
    }

    [Fact]
    public async Task CreateAsync_WhenCompanyAdminAssignsSuperAdmin_ThrowsForbiddenException()
    {
        SetupCompanyAdmin(Guid.NewGuid());
        var request = ValidRequest(role: UserRole.SuperAdmin);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Only SuperAdmin can assign the SuperAdmin role.");
        _users.Verify(
            repository => repository.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupCompanyAdmin(Guid companyId)
    {
        _currentUser.SetupGet(user => user.IsAuthenticated).Returns(true);
        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(false);
        _currentUser.SetupGet(user => user.CompanyId).Returns(companyId);
        _currentUser.SetupGet(user => user.Role).Returns(UserRole.CompanyAdmin);
    }

    private void SetupSuperAdmin()
    {
        _currentUser.SetupGet(user => user.IsAuthenticated).Returns(true);
        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(true);
        _currentUser.SetupGet(user => user.CompanyId).Returns((Guid?)null);
        _currentUser.SetupGet(user => user.Role).Returns(UserRole.SuperAdmin);
    }

    private static CreateUserRequest ValidRequest(
        string email = "ali@test.com",
        UserRole role = UserRole.Operator,
        Guid? companyId = null,
        string firstName = "Ali",
        string lastName = "Veli",
        string password = "Secret1!") =>
        new(firstName, lastName, email, password, companyId ?? Guid.NewGuid(), role);
}
