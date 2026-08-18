using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Application.Validations.Auth;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using Moq;

namespace IoTSensorMonitoring.Tests.Services.Auth;

internal static class AuthServiceTestHelper
{
    public static AuthService CreateSut(
        Mock<IUserRepository> userRepository,
        Mock<IPasswordService> passwordService,
        Mock<ITokenService> tokenService) =>
        new(
            userRepository.Object,
            passwordService.Object,
            tokenService.Object,
            new LoginRequestValidator());

    public static LoginRequest ValidLogin(string email = "ali@test.com", string password = "Secret1!") =>
        new(email, password);

    public static User CreateUser(
        string email = "ali@test.com",
        string passwordHash = "hashed",
        UserRole role = UserRole.Operator,
        Guid? companyId = null,
        bool isActive = true) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId ?? Guid.NewGuid(),
            FirstName = "Ali",
            LastName = "Veli",
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

    public static void SetupToken(Mock<ITokenService> tokenService, string token = "jwt-token")
    {
        var expiresAt = DateTime.UtcNow.AddHours(1);
        tokenService
            .Setup(service => service.CreateToken(It.IsAny<User>(), out expiresAt))
            .Returns(token);
    }

    public static void SetupCompanyAdminCurrentUser(Mock<ICurrentUser> currentUser, Guid companyId)
    {
        currentUser.SetupGet(user => user.IsAuthenticated).Returns(true);
        currentUser.SetupGet(user => user.IsSuperAdmin).Returns(false);
        currentUser.SetupGet(user => user.CompanyId).Returns(companyId);
        currentUser.SetupGet(user => user.Role).Returns(UserRole.CompanyAdmin);
    }

    public static void SetupSuperAdminCurrentUser(Mock<ICurrentUser> currentUser)
    {
        currentUser.SetupGet(user => user.IsAuthenticated).Returns(true);
        currentUser.SetupGet(user => user.IsSuperAdmin).Returns(true);
        currentUser.SetupGet(user => user.CompanyId).Returns((Guid?)null);
        currentUser.SetupGet(user => user.Role).Returns(UserRole.SuperAdmin);
    }
}
