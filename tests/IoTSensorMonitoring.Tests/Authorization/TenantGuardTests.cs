using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Moq;

namespace IoTSensorMonitoring.Tests.Authorization;

public class TenantGuardTests
{
    private readonly Mock<ICurrentUser> _currentUser = new();

    [Fact]
    public void ResolveCompanyId_WhenSuperAdmin_ReturnsRequestCompanyId()
    {
        var companyId = Guid.NewGuid();
        SetupSuperAdmin();

        var result = TenantGuard.ResolveCompanyId(_currentUser.Object, companyId);

        result.Should().Be(companyId);
    }

    [Fact]
    public void ResolveCompanyId_WhenSuperAdminMissingCompanyId_ThrowsValidationException()
    {
        SetupSuperAdmin();

        var act = () => TenantGuard.ResolveCompanyId(_currentUser.Object, null);

        act.Should().Throw<ValidationException>()
            .WithMessage("SuperAdmin requires CompanyId.");
    }

    [Fact]
    public void ResolveCompanyId_WhenCompanyAdmin_ReturnsTokenCompanyId()
    {
        var tokenCompanyId = Guid.NewGuid();
        SetupCompanyAdmin(tokenCompanyId);

        var result = TenantGuard.ResolveCompanyId(_currentUser.Object, Guid.NewGuid());

        result.Should().Be(tokenCompanyId);
    }

    [Fact]
    public void ResolveCompanyId_WhenCompanyAdminMissingCompanyId_ThrowsForbiddenException()
    {
        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(false);
        _currentUser.SetupGet(user => user.CompanyId).Returns((Guid?)null);

        var act = () => TenantGuard.ResolveCompanyId(_currentUser.Object, Guid.NewGuid());

        act.Should().Throw<ForbiddenException>()
            .WithMessage("Company context was not found.");
    }

    [Fact]
    public void RequireUserId_WhenPresent_ReturnsId()
    {
        var userId = Guid.NewGuid();
        _currentUser.SetupGet(user => user.UserId).Returns(userId);

        TenantGuard.RequireUserId(_currentUser.Object).Should().Be(userId);
    }

    [Fact]
    public void RequireUserId_WhenMissing_ThrowsForbiddenException()
    {
        _currentUser.SetupGet(user => user.UserId).Returns((Guid?)null);

        var act = () => TenantGuard.RequireUserId(_currentUser.Object);

        act.Should().Throw<ForbiddenException>()
            .WithMessage("User identity was not found.");
    }

    [Fact]
    public void EnsureCompanyAccess_WhenSuperAdmin_DoesNotThrow()
    {
        SetupSuperAdmin();

        var act = () => TenantGuard.EnsureCompanyAccess(_currentUser.Object, Guid.NewGuid());

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCompanyAccess_WhenOtherCompany_ThrowsForbiddenException()
    {
        SetupCompanyAdmin(Guid.NewGuid());

        var act = () => TenantGuard.EnsureCompanyAccess(_currentUser.Object, Guid.NewGuid());

        act.Should().Throw<ForbiddenException>()
            .WithMessage("You do not have access to this company.");
    }

    [Fact]
    public void EnsureCanAssignRole_WhenCompanyAdminAssignsSuperAdmin_ThrowsForbiddenException()
    {
        SetupCompanyAdmin(Guid.NewGuid());

        var act = () => TenantGuard.EnsureCanAssignRole(_currentUser.Object, UserRole.SuperAdmin);

        act.Should().Throw<ForbiddenException>()
            .WithMessage("Only SuperAdmin can assign the SuperAdmin role.");
    }

    [Fact]
    public void ResolveUserCompanyId_WhenSuperAdminRole_ReturnsNull()
    {
        SetupSuperAdmin();

        TenantGuard.ResolveUserCompanyId(_currentUser.Object, UserRole.SuperAdmin, null)
            .Should().BeNull();
    }

    private void SetupSuperAdmin()
    {
        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(true);
        _currentUser.SetupGet(user => user.CompanyId).Returns((Guid?)null);
    }

    private void SetupCompanyAdmin(Guid companyId)
    {
        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(false);
        _currentUser.SetupGet(user => user.CompanyId).Returns(companyId);
        _currentUser.SetupGet(user => user.Role).Returns(UserRole.CompanyAdmin);
    }
}
