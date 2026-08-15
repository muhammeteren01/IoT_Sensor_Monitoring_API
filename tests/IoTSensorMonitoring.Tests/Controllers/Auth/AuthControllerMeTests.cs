using System.Security.Claims;
using IoTSensorMonitoring.Api.Controllers;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IoTSensorMonitoring.Tests.Controllers.Auth;

public class AuthControllerMeTests
{
    private readonly AuthController _sut = new(new Mock<IAuthService>().Object);

    [Fact]
    public void Me_WhenAllClaimsPresent_ReturnsOkWithAnonymousObject()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        AuthTestHelper.SetUser(_sut,
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "ali@test.com"),
            new Claim(ClaimTypes.Name, "Ali Veli"),
            new Claim(ClaimTypes.Role, UserRole.Operator.ToString()),
            new Claim("company_id", companyId.ToString()));

        var result = _sut.Me();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            id = userId.ToString(),
            email = "ali@test.com",
            name = "Ali Veli",
            role = UserRole.Operator.ToString(),
            companyId = companyId.ToString()
        });
    }

    [Fact]
    public void Me_WhenCompanyIdClaimMissing_ReturnsNullCompanyId()
    {
        var userId = Guid.NewGuid();
        AuthTestHelper.SetUser(_sut,
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "root@test.com"),
            new Claim(ClaimTypes.Name, "Root Admin"),
            new Claim(ClaimTypes.Role, UserRole.SuperAdmin.ToString()));

        var result = _sut.Me();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            id = userId.ToString(),
            email = "root@test.com",
            name = "Root Admin",
            role = UserRole.SuperAdmin.ToString(),
            companyId = (string?)null
        });
    }

    [Fact]
    public void Me_WhenNoClaims_ReturnsOkWithAllFieldsNull()
    {
        AuthTestHelper.SetUser(_sut);

        var result = _sut.Me();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            id = (string?)null,
            email = (string?)null,
            name = (string?)null,
            role = (string?)null,
            companyId = (string?)null
        });
    }
}
