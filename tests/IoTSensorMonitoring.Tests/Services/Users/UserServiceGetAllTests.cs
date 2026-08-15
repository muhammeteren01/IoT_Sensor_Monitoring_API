using System.Linq.Expressions;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using FluentAssertions;
using Moq;

namespace IoTSensorMonitoring.Tests.Services.Users;

public class UserServiceGetAllTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<Company>> _companies = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly UserService _sut;

    public UserServiceGetAllTests()
    {
        _unitOfWork.SetupGet(unit => unit.Companies).Returns(_companies.Object);
        _unitOfWork.SetupGet(unit => unit.Users).Returns(_users.Object);
        _sut = new UserService(_unitOfWork.Object, _currentUser.Object);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoCompanyFilter_ReturnsMappedUsers()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            FirstName = "Ali",
            LastName = "Veli",
            Email = "ali@test.com",
            PasswordHash = "hash",
            Role = UserRole.Operator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _users
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { user });

        var result = await _sut.GetAllAsync(null);

        result.Should().ContainSingle();
        result[0].Email.Should().Be("ali@test.com");
        result[0].CompanyId.Should().Be(user.CompanyId);
    }

    [Fact]
    public async Task GetAllAsync_WhenCompanyAdminAccessesOtherCompany_ThrowsForbiddenException()
    {
        var ownCompany = Guid.NewGuid();
        var otherCompany = Guid.NewGuid();
        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(false);
        _currentUser.SetupGet(user => user.CompanyId).Returns(ownCompany);

        var act = async () => await _sut.GetAllAsync(otherCompany);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetAllAsync_WhenCompanyMissing_ThrowsNotFoundException()
    {
        var companyId = Guid.NewGuid();
        _currentUser.SetupGet(user => user.IsSuperAdmin).Returns(true);
        _companies
            .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = async () => await _sut.GetAllAsync(companyId);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
