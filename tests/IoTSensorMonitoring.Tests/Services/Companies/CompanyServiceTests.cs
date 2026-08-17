using System.Linq.Expressions;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Validations.Companies;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IoTSensorMonitoring.Tests.Services.Companies;

public class CompanyServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<Company>> _companies = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRepository<DeviceModel>> _deviceModels = new();
    private readonly Mock<ISensorRepository> _sensors = new();
    private readonly Mock<IGrafanaTenantProvisioner> _grafana = new();
    private readonly CompanyService _sut;

    public CompanyServiceTests()
    {
        _unitOfWork.SetupGet(unit => unit.Companies).Returns(_companies.Object);
        _unitOfWork.SetupGet(unit => unit.Users).Returns(_users.Object);
        _unitOfWork.SetupGet(unit => unit.DeviceModels).Returns(_deviceModels.Object);
        _unitOfWork.SetupGet(unit => unit.Sensors).Returns(_sensors.Object);
        _grafana
            .Setup(provisioner => provisioner.ProvisionAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _grafana
            .Setup(provisioner => provisioner.DeprovisionAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sut = new CompanyService(
            _unitOfWork.Object,
            _grafana.Object,
            new CreateCompanyRequestValidator(),
            new UpdateCompanyRequestValidator(),
            NullLogger<CompanyService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_AddsCompanyAndReturnsDto()
    {
        Company? added = null;
        _companies
            .Setup(repository => repository.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()))
            .Callback<Company, CancellationToken>((company, _) => added = company)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(new CreateCompanyRequest("Acme", "acme@test.com"));

        added.Should().NotBeNull();
        added!.Name.Should().Be("Acme");
        added.ContactEmail.Should().Be("acme@test.com");
        added.IsActive.Should().BeTrue();
        result.Name.Should().Be("Acme");
        result.Id.Should().Be(added.Id);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _grafana.Verify(
            provisioner => provisioner.ProvisionAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _companies
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);

        var act = async () => await _sut.GetByIdAsync(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenCompanyHasUsers_ThrowsConflictException()
    {
        var company = new Company { Name = "Acme" };
        _companies
            .Setup(repository => repository.GetByIdAsync(company.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _users
            .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _sut.DeleteAsync(company.Id);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Company cannot be deleted while it still has users.");
        _companies.Verify(repository => repository.Remove(It.IsAny<Company>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenCompanyHasSensors_ThrowsConflictException()
    {
        var company = new Company { Name = "Acme" };
        _companies
            .Setup(repository => repository.GetByIdAsync(company.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _users
            .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _deviceModels
            .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<DeviceModel, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _sensors
            .Setup(repository => repository.ExistsInCompanyAsync(company.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _sut.DeleteAsync(company.Id);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Company cannot be deleted while it still has sensors.");
    }

    [Fact]
    public async Task DeleteAsync_WhenEmpty_RemovesCompany()
    {
        var company = new Company { Name = "Acme" };
        _companies
            .Setup(repository => repository.GetByIdAsync(company.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _users
            .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _deviceModels
            .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<DeviceModel, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _sensors
            .Setup(repository => repository.ExistsInCompanyAsync(company.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.DeleteAsync(company.Id);

        _grafana.Verify(
            provisioner => provisioner.DeprovisionAsync(company, It.IsAny<CancellationToken>()),
            Times.Once);
        _companies.Verify(repository => repository.Remove(company), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
