using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Application.Settings;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace IoTSensorMonitoring.Tests.Services.Auth;

public class TokenServiceTests
{
    private readonly TokenService _sut = new(Options.Create(new JwtSettings
    {
        Secret = "IoTSensorMonitoring_TestJwtSecret_32chars!",
        Issuer = "IoTSensorMonitoringApi",
        Audience = "IoTSensorMonitoringApi",
        ExpirationInMinutes = 60
    }));

    [Fact]
    public void CreateToken_WhenCompanyUser_IncludesRoleAndCompanyId()
    {
        var companyId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FirstName = "Ali",
            LastName = "Veli",
            Email = "ali@test.com",
            PasswordHash = "hash",
            Role = UserRole.CompanyAdmin
        };

        var token = _sut.CreateToken(user, out var expiresAt);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        token.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeAfter(DateTime.UtcNow);
        jwt.Claims.Should().Contain(claim => claim.Type == ClaimTypes.Role && claim.Value == UserRole.CompanyAdmin.ToString());
        jwt.Claims.Should().Contain(claim => claim.Type == "company_id" && claim.Value == companyId.ToString());
        jwt.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == user.Email);
    }

    [Fact]
    public void CreateToken_WhenSuperAdmin_OmitsCompanyId()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            CompanyId = null,
            FirstName = "Super",
            LastName = "Admin",
            Email = "admin@iot.local",
            PasswordHash = "hash",
            Role = UserRole.SuperAdmin
        };

        var token = _sut.CreateToken(user, out _);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().NotContain(claim => claim.Type == "company_id");
        jwt.Claims.Should().Contain(claim => claim.Type == ClaimTypes.Role && claim.Value == UserRole.SuperAdmin.ToString());
    }
}
