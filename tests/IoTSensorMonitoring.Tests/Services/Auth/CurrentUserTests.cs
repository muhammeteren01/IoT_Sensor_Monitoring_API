using System.Security.Claims;
using IoTSensorMonitoring.Api.Services;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace IoTSensorMonitoring.Tests.Services.Auth;

public class CurrentUserTests
{
    [Fact]
    public void ApplyTenantFilter_WhenAnonymous_IsFalse()
    {
        var sut = CreateAnonymous();

        sut.IsAuthenticated.Should().BeFalse();
        sut.ApplyTenantFilter.Should().BeFalse();
    }

    [Fact]
    public void ApplyTenantFilter_WhenSuperAdmin_IsFalse()
    {
        var sut = Create(
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRole.SuperAdmin.ToString()));

        sut.IsAuthenticated.Should().BeTrue();
        sut.IsSuperAdmin.Should().BeTrue();
        sut.ApplyTenantFilter.Should().BeFalse();
        sut.CompanyId.Should().BeNull();
    }

    [Fact]
    public void ApplyTenantFilter_WhenCompanyAdmin_IsTrue()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var sut = Create(
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, UserRole.CompanyAdmin.ToString()),
            new Claim("company_id", companyId.ToString()));

        sut.IsAuthenticated.Should().BeTrue();
        sut.IsSuperAdmin.Should().BeFalse();
        sut.ApplyTenantFilter.Should().BeTrue();
        sut.UserId.Should().Be(userId);
        sut.CompanyId.Should().Be(companyId);
        sut.Role.Should().Be(UserRole.CompanyAdmin);
    }

    [Fact]
    public void ApplyTenantFilter_WhenOperator_IsTrue()
    {
        var sut = Create(
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRole.Operator.ToString()),
            new Claim("company_id", Guid.NewGuid().ToString()));

        sut.ApplyTenantFilter.Should().BeTrue();
        sut.Role.Should().Be(UserRole.Operator);
    }

    private static CurrentUser CreateAnonymous()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(item => item.HttpContext).Returns((HttpContext?)null);
        return new CurrentUser(accessor.Object);
    }

    private static CurrentUser Create(params Claim[] claims)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        accessor.SetupGet(item => item.HttpContext).Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        });

        return new CurrentUser(accessor.Object);
    }
}
