using System.Linq.Expressions;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Application.Services;
using IoTSensorMonitoring.Domain.Entities;
using FluentAssertions;
using Moq;

namespace IoTSensorMonitoring.Tests.Services.Alerts;

public class AlertHistoryServiceListTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAlertHistoryRepository> _histories = new();
    private readonly Mock<ISensorRepository> _sensors = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly AlertHistoryService _sut;

    public AlertHistoryServiceListTests()
    {
        _unitOfWork.SetupGet(unit => unit.AlertHistories).Returns(_histories.Object);
        _unitOfWork.SetupGet(unit => unit.Sensors).Returns(_sensors.Object);
        _sut = new AlertHistoryService(_unitOfWork.Object, _currentUser.Object);
    }

    [Fact]
    public async Task ListAsync_WhenOpenAlert_StatusIsOpen()
    {
        var history = new AlertHistory
        {
            AlertRuleId = Guid.NewGuid(),
            SensorId = Guid.NewGuid(),
            TriggeredValue = 24.1m,
            Message = "Sıcaklık 24.1 > 23.7",
            IsResolved = false
        };
        _histories
            .Setup(repository => repository.ListAsync(null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AlertHistory>());
        _histories
            .Setup(repository => repository.ListAsync(null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { history });

        var result = await _sut.ListAsync(null, false);

        result.Should().ContainSingle();
        result[0].IsResolved.Should().BeFalse();
        result[0].Status.Should().Be("Open");
    }

    [Fact]
    public async Task ListAsync_WhenSensorIdGiven_ChecksSensorExists()
    {
        var sensorId = Guid.NewGuid();
        _sensors
            .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<Sensor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _histories
            .Setup(repository => repository.ListAsync(sensorId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AlertHistory>());

        var result = await _sut.ListAsync(sensorId, null);

        result.Should().BeEmpty();
        _sensors.Verify(
            repository => repository.AnyAsync(It.IsAny<Expression<Func<Sensor, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
