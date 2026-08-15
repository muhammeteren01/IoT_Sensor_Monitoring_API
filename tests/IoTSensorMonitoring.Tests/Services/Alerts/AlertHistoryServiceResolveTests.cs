using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Domain.Entities;
using FluentAssertions;
using Moq;

namespace IoTSensorMonitoring.Tests.Services.Alerts;

public class AlertHistoryServiceResolveTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAlertHistoryRepository> _histories = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly AlertHistoryService _sut;

    public AlertHistoryServiceResolveTests()
    {
        _unitOfWork.SetupGet(unit => unit.AlertHistories).Returns(_histories.Object);
        _sut = new AlertHistoryService(_unitOfWork.Object, _currentUser.Object);
    }

    [Fact]
    public async Task ResolveAsync_WhenMissing_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _histories
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AlertHistory?)null);

        var act = async () => await _sut.ResolveAsync(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ResolveAsync_WhenAlreadyResolved_ThrowsConflictException()
    {
        var history = CreateHistory(isResolved: true);
        _histories
            .Setup(repository => repository.GetByIdAsync(history.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var act = async () => await _sut.ResolveAsync(history.Id);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Alert is already resolved.");
    }

    [Fact]
    public async Task ResolveAsync_WhenSuccessful_SetsResolvedByUserIdFromToken()
    {
        var userId = Guid.NewGuid();
        var history = CreateHistory(isResolved: false);
        _currentUser.SetupGet(user => user.UserId).Returns(userId);
        _histories
            .Setup(repository => repository.GetByIdAsync(history.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.ResolveAsync(history.Id);

        history.IsResolved.Should().BeTrue();
        history.ResolvedByUserId.Should().Be(userId);
        history.ResolvedAt.Should().NotBeNull();
        result.ResolvedByUserId.Should().Be(userId);
        result.Status.Should().Be("Resolved");
        _histories.Verify(repository => repository.Update(history), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WhenUserIdMissing_ThrowsForbiddenException()
    {
        var history = CreateHistory(isResolved: false);
        _currentUser.SetupGet(user => user.UserId).Returns((Guid?)null);
        _histories
            .Setup(repository => repository.GetByIdAsync(history.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var act = async () => await _sut.ResolveAsync(history.Id);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("User identity was not found.");
    }

    private static AlertHistory CreateHistory(bool isResolved) =>
        new()
        {
            AlertRuleId = Guid.NewGuid(),
            SensorId = Guid.NewGuid(),
            TriggeredValue = 42,
            Message = "High temperature",
            IsResolved = isResolved
        };
}
