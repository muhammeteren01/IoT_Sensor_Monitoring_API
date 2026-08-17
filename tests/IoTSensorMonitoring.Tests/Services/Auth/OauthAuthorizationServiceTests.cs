using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Application.Settings;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace IoTSensorMonitoring.Tests.Services.Auth;

public class OauthAuthorizationServiceTests
{
    private readonly Mock<IAuthCodeStore> _codes = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<Company>> _companies = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IGrafanaTenantProvisioner> _grafana = new();
    private readonly OauthAuthorizationService _sut;

    public OauthAuthorizationServiceTests()
    {
        _unitOfWork.SetupGet(unit => unit.Companies).Returns(_companies.Object);
        _sut = new OauthAuthorizationService(
            _codes.Object,
            _users.Object,
            _unitOfWork.Object,
            _tokens.Object,
            _grafana.Object,
            Options.Create(new GrafanaSettings
            {
                RedirectUris = "http://localhost:3000/login/generic_oauth"
            }));
    }

    [Fact]
    public void IsAllowedRedirectUri_AcceptsConfiguredGrafanaCallback()
    {
        _sut.IsAllowedRedirectUri("http://localhost:3000/login/generic_oauth").Should().BeTrue();
        _sut.IsAllowedRedirectUri("http://evil.example/callback").Should().BeFalse();
    }

    [Fact]
    public async Task GetUserInfoAsync_WhenCompanyUser_ReturnsViewerAndCompanyOrg()
    {
        var company = new Company { Name = "trex" };
        var user = new User
        {
            FirstName = "Ada",
            LastName = "Admin",
            Email = "ada@acme.test",
            PasswordHash = "hash",
            Role = UserRole.CompanyAdmin,
            CompanyId = company.Id,
            IsActive = true
        };
        _users.Setup(repository => repository.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _companies.Setup(repository => repository.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _grafana.Setup(provisioner => provisioner.EnsureUserAccessAsync(user, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var info = await _sut.GetUserInfoAsync(user.Id);

        info.Role.Should().Be("Viewer");
        info.Login.Should().Be(user.Email);
        info.GrafanaOrg.Should().Be($"trex · {company.Id.ToString("N")[..8]}");
        info.Orgs.Should().ContainSingle().Which.Should().Be(info.GrafanaOrg);
    }

    [Fact]
    public async Task GetUserInfoAsync_WhenGrafanaSyncFails_StillReturnsCompanyOrg()
    {
        var company = new Company { Name = "trex" };
        var user = new User
        {
            FirstName = "Ada",
            LastName = "Admin",
            Email = "ada@trex.test",
            PasswordHash = "hash",
            Role = UserRole.CompanyAdmin,
            CompanyId = company.Id,
            IsActive = true
        };
        _users.Setup(repository => repository.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _companies.Setup(repository => repository.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _grafana
            .Setup(provisioner => provisioner.EnsureUserAccessAsync(user, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Grafana org create failed"));

        var info = await _sut.GetUserInfoAsync(user.Id);

        info.Role.Should().Be("Viewer");
        info.GrafanaOrg.Should().StartWith("trex ·");
    }

    [Fact]
    public async Task GetUserInfoAsync_WhenSuperAdmin_ReturnsGrafanaAdminInMainOrg()
    {
        var user = new User
        {
            FirstName = "Super",
            LastName = "Admin",
            Email = "admin@iot.local",
            PasswordHash = "hash",
            Role = UserRole.SuperAdmin,
            IsActive = true
        };
        _users.Setup(repository => repository.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var info = await _sut.GetUserInfoAsync(user.Id);

        info.Role.Should().Be("GrafanaAdmin");
        info.GrafanaOrg.Should().Be("Main Org.");
        info.Orgs.Should().ContainSingle().Which.Should().Be("Main Org.");
    }
}
