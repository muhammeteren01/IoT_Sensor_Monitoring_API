using FluentValidation;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Services;

public class AlertRuleService : IAlertRuleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateAlertRuleRequest> _createValidator;
    private readonly IValidator<UpdateAlertRuleRequest> _updateValidator;

    public AlertRuleService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateAlertRuleRequest> createValidator,
        IValidator<UpdateAlertRuleRequest> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<AlertRuleDto> CreateAsync(CreateAlertRuleRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        await EnsureSensorTenantAsync(request.SensorId, cancellationToken);

        var rule = new AlertRule
        {
            SensorId = request.SensorId,
            Metric = request.Metric,
            Operator = request.Operator,
            Threshold = request.Threshold,
            Severity = request.Severity,
            IsActive = true
        };

        await _unitOfWork.AlertRules.AddAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(rule);
    }

    public async Task<IReadOnlyList<AlertRuleDto>> GetBySensorIdAsync(Guid sensorId, CancellationToken cancellationToken = default)
    {
        await EnsureSensorExistsAsync(sensorId, cancellationToken);
        var rules = await _unitOfWork.AlertRules.GetBySensorIdAsync(sensorId, cancellationToken);
        return rules.Select(Map).ToList();
    }

    public async Task<AlertRuleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Map(await GetRequiredAsync(id, cancellationToken));
    }

    public async Task<AlertRuleDto> UpdateAsync(Guid id, UpdateAlertRuleRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_updateValidator, request, cancellationToken);

        var rule = await GetRequiredAsync(id, cancellationToken);
        rule.Metric = request.Metric;
        rule.Operator = request.Operator;
        rule.Threshold = request.Threshold;
        rule.Severity = request.Severity;
        rule.IsActive = request.IsActive;

        _unitOfWork.AlertRules.Update(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(rule);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await GetRequiredAsync(id, cancellationToken);

        var histories = await _unitOfWork.AlertHistories.FindAsync(
            history => history.AlertRuleId == id,
            cancellationToken);

        foreach (var history in histories)
        {
            _unitOfWork.AlertHistories.Remove(history);
        }

        _unitOfWork.AlertRules.Remove(rule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSensorExistsAsync(Guid sensorId, CancellationToken cancellationToken)
    {
        await EnsureSensorTenantAsync(sensorId, cancellationToken);
    }

    private async Task EnsureSensorTenantAsync(Guid sensorId, CancellationToken cancellationToken)
    {
        var sensor = await _unitOfWork.Sensors.GetByIdWithDetailsAsync(sensorId, cancellationToken)
            ?? throw new NotFoundException(nameof(Sensor), sensorId);

        TenantGuard.EnsureCompanyAccess(_currentUser, sensor.Zone.Facility.CompanyId);
    }

    private async Task<AlertRule> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _unitOfWork.AlertRules.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(AlertRule), id);
    }

    private static AlertRuleDto Map(AlertRule rule) =>
        new(rule.Id, rule.SensorId, rule.Metric, rule.Operator, rule.Threshold, rule.Severity, rule.IsActive);
}
