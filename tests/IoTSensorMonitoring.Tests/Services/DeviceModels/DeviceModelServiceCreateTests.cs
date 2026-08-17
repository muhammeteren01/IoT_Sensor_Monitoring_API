using System.Linq.Expressions;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Application.Validations.DeviceModels;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Moq;

namespace IoTSensorMonitoring.Tests.Services.DeviceModels;

public class DeviceModelServiceCreateTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<Company>> _companies = new();
    private readonly Mock<IRepository<DeviceModel>> _deviceModels = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly DeviceModelService _sut;

    public DeviceModelServiceCreateTests()
    {
        _unitOfWork.SetupGet(unit => unit.Companies).Returns(_companies.Object);
        _unitOfWork.SetupGet(unit => unit.DeviceModels).Returns(_deviceModels.Object);
        _sut = new DeviceModelService(
            _unitOfWork.Object,
            _currentUser.Object,
            new CreateDeviceModelRequestValidator(),
            new UpdateDeviceModelRequestValidator());
    }

    [Fact]
    public async Task CreateAsync_WhenCompanyAdmin_UsesTokenCompanyId()
    {
        var tokenCompanyId = Guid.NewGuid();
        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(false);
        _currentUser.SetupGet(user => user.CompanyId).Returns(tokenCompanyId);
        _currentUser.SetupGet(user => user.Role).Returns(UserRole.CompanyAdmin);
        _companies
            .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _deviceModels
            .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<DeviceModel, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        DeviceModel? added = null;
        _deviceModels
            .Setup(repository => repository.AddAsync(It.IsAny<DeviceModel>(), It.IsAny<CancellationToken>()))
            .Callback<DeviceModel, CancellationToken>((model, _) => added = model)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(new CreateDeviceModelRequest(
            Guid.NewGuid(),
            "Bosch",
            "BME680",
            "Temperature,Humidity",
            180));

        added.Should().NotBeNull();
        added!.CompanyId.Should().Be(tokenCompanyId);
        result.CompanyId.Should().Be(tokenCompanyId);
        result.Manufacturer.Should().Be("Bosch");
    }

    [Fact]
    public async Task CreateAsync_WhenSuperAdmin_UsesRequestCompanyId()
    {
        var requestCompanyId = Guid.NewGuid();
        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(true);
        _companies
            .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _deviceModels
            .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<DeviceModel, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _deviceModels
            .Setup(repository => repository.AddAsync(It.IsAny<DeviceModel>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(new CreateDeviceModelRequest(
            requestCompanyId,
            "Bosch",
            "BME680",
            "Temperature",
            90));

        result.CompanyId.Should().Be(requestCompanyId);
    }
}
