using System.Linq.Expressions;
using IoTSensorMonitoring.Application.Validations.Facilities;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Moq;

namespace IoTSensorMonitoring.Tests.Services.Facilities;

public class FacilityServiceCreateTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<Company>> _companies = new();
    private readonly Mock<IFacilityRepository> _facilities = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly FacilityService _sut;

    public FacilityServiceCreateTests()
    {
        _unitOfWork.SetupGet(unit => unit.Companies).Returns(_companies.Object);
        _unitOfWork.SetupGet(unit => unit.Facilities).Returns(_facilities.Object);
        _sut = new FacilityService(
            _unitOfWork.Object,
            _currentUser.Object,
            new CreateFacilityRequestValidator(),
            new UpdateFacilityRequestValidator());
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

        Facility? added = null;
        _facilities
            .Setup(repository => repository.AddAsync(It.IsAny<Facility>(), It.IsAny<CancellationToken>()))
            .Callback<Facility, CancellationToken>((facility, _) => added = facility)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(new CreateFacilityRequest(Guid.NewGuid(), "Plant", "Istanbul", null));

        added.Should().NotBeNull();
        added!.CompanyId.Should().Be(tokenCompanyId);
        result.CompanyId.Should().Be(tokenCompanyId);
        result.Name.Should().Be("Plant");
        result.FloorCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_WhenSuperAdmin_UsesRequestCompanyId()
    {
        var requestCompanyId = Guid.NewGuid();
        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(true);
        _companies
            .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _facilities
            .Setup(repository => repository.AddAsync(It.IsAny<Facility>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(new CreateFacilityRequest(requestCompanyId, "Plant", null, null));

        result.CompanyId.Should().Be(requestCompanyId);
    }

    [Fact]
    public async Task CreateAsync_WhenFloorCountProvided_MapsFloorCount()
    {
        var requestCompanyId = Guid.NewGuid();
        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(true);
        _companies
            .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _facilities
            .Setup(repository => repository.AddAsync(It.IsAny<Facility>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(new CreateFacilityRequest(requestCompanyId, "Plant", null, null, 4));

        result.FloorCount.Should().Be(4);
    }

    [Fact]
    public async Task CreateAsync_WhenFloorCountInvalid_ThrowsValidationException()
    {
        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(true);

        var act = async () => await _sut.CreateAsync(
            new CreateFacilityRequest(Guid.NewGuid(), "Plant", null, null, 0));

        await act.Should().ThrowAsync<ValidationException>();
        _facilities.Verify(
            repository => repository.AddAsync(It.IsAny<Facility>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCompanyMissing_ThrowsNotFoundException()
    {
        var companyId = Guid.NewGuid();
        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(true);
        _companies
            .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = async () => await _sut.CreateAsync(new CreateFacilityRequest(companyId, "Plant", null, null));

        await act.Should().ThrowAsync<NotFoundException>();
        _facilities.Verify(
            repository => repository.AddAsync(It.IsAny<Facility>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSuperAdminMissingCompanyId_ThrowsValidationException()
    {
        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(true);

        var act = async () => await _sut.CreateAsync(new CreateFacilityRequest(Guid.Empty, "Plant", null, null));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("SuperAdmin requires CompanyId.");
    }
}
