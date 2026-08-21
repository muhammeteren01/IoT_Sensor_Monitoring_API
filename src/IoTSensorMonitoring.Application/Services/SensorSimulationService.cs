using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Settings;
using IoTSensorMonitoring.Application.Simulation;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IoTSensorMonitoring.Application.Services;

public class SensorSimulationService : ISensorSimulationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly WorkerSettings _settings;
    private readonly ILogger<SensorSimulationService> _logger;
    private readonly MeasurementGenerator _generator;

    public SensorSimulationService(
        IUnitOfWork unitOfWork,
        IOptions<WorkerSettings> settings,
        ILogger<SensorSimulationService> logger,
        MeasurementGenerator generator)
    {
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _logger = logger;
        _generator = generator;
    }

    public async Task RunCycleAsync(CancellationToken cancellationToken = default)
    {
        var sensors = await _unitOfWork.Sensors.GetActiveWithDeviceModelAsync(cancellationToken);
        if (sensors.Count == 0)
        {
            _logger.LogInformation("Simulation cycle skipped; no active sensors");
            return;
        }

        _logger.LogInformation("Simulation cycle started. ActiveSensors={Count}", sensors.Count);

        foreach (var sensor in sensors)
        {
            try
            {
                await SimulateSensorAsync(sensor, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Simulation failed for sensor {SensorId} ({SensorName})", sensor.Id, sensor.Name);
            }
        }
    }

    private async Task SimulateSensorAsync(Sensor sensor, CancellationToken cancellationToken)
    {
        var metrics = SupportedMetricsParser.Parse(sensor.DeviceModel.SupportedMetrics);
        if (metrics.Count == 0)
        {
            _logger.LogWarning(
                "Sensor {SensorId} ({SensorName}) skipped; SupportedMetrics is empty or invalid: {SupportedMetrics}",
                sensor.Id,
                sensor.Name,
                sensor.DeviceModel.SupportedMetrics);
            return;
        }

        var previous = await _unitOfWork.SensorMeasurements.GetLatestBySensorIdAsync(sensor.Id, cancellationToken);
        var effectivePrevious = await ResolvePreviousWithBatteryReplacementAsync(sensor, previous, metrics, cancellationToken);
        var measurement = _generator.Next(sensor.Id, effectivePrevious, metrics);
        measurement.MeasurementDate = measurement.MeasurementDate.ToUniversalTime();

        await _unitOfWork.SensorMeasurements.AddAsync(measurement, cancellationToken);

        var rules = await _unitOfWork.AlertRules.GetActiveBySensorIdAsync(sensor.Id, cancellationToken);
        if (rules.Count > 0)
        {
            var unresolved = await _unitOfWork.AlertHistories.GetUnresolvedBySensorIdAsync(sensor.Id, cancellationToken);
            var openRuleIds = unresolved.Select(history => history.AlertRuleId).ToHashSet();
            await EvaluateAlertsAsync(sensor, measurement, rules, openRuleIds, cancellationToken);
        }

        await WarnIfCalibrationDueAsync(sensor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Measurement recorded. SensorId={SensorId}, SensorName={SensorName}, Temperature={Temperature}, Humidity={Humidity}, Pressure={Pressure}, Battery={Battery}",
            sensor.Id,
            sensor.Name,
            measurement.Temperature,
            measurement.Humidity,
            measurement.Pressure,
            measurement.BatteryLevel);
    }

    private async Task EvaluateAlertsAsync(
        Sensor sensor,
        SensorMeasurement measurement,
        IReadOnlyList<AlertRule> rules,
        HashSet<Guid> openRuleIds,
        CancellationToken cancellationToken)
    {
        foreach (var rule in rules)
        {
            var value = AlertRuleEvaluator.ReadValue(measurement, rule.Metric);
            if (!value.HasValue)
            {
                continue;
            }

            if (!AlertRuleEvaluator.IsTriggered(rule.Operator, value.Value, rule.Threshold))
            {
                continue;
            }

            if (openRuleIds.Contains(rule.Id))
            {
                continue;
            }

            var message = AlertRuleEvaluator.FormatMessage(rule.Metric, rule.Operator, rule.Threshold, value.Value);
            await _unitOfWork.AlertHistories.AddAsync(
                new AlertHistory
                {
                    AlertRuleId = rule.Id,
                    SensorId = sensor.Id,
                    TriggeredValue = value.Value,
                    Message = message,
                    TriggeredAt = measurement.MeasurementDate,
                    IsResolved = false
                },
                cancellationToken);

            openRuleIds.Add(rule.Id);

            if (rule.Severity == Domain.Enums.AlertSeverity.Warning)
            {
                _logger.LogWarning(
                    "Alert triggered. SensorId={SensorId}, SensorName={SensorName}, Severity={Severity}, Message={Message}",
                    sensor.Id,
                    sensor.Name,
                    rule.Severity,
                    message);
            }
            else
            {
                _logger.LogError(
                    "Alert triggered. SensorId={SensorId}, SensorName={SensorName}, Severity={Severity}, Message={Message}",
                    sensor.Id,
                    sensor.Name,
                    rule.Severity,
                    message);
            }
        }
    }

    private async Task<SensorMeasurement?> ResolvePreviousWithBatteryReplacementAsync(
        Sensor sensor,
        SensorMeasurement? previous,
        IReadOnlySet<SensorMetric> metrics,
        CancellationToken cancellationToken)
    {
        if (!metrics.Contains(SensorMetric.BatteryLevel))
        {
            return previous;
        }

        var latestReplacement = await _unitOfWork.MaintenanceLogs.GetLatestBySensorIdAndActionTypeAsync(
            sensor.Id,
            MaintenanceActionType.BatteryReplacement,
            cancellationToken);

        if (latestReplacement is null)
        {
            return previous;
        }

        var hasResetMeasurement = await _unitOfWork.SensorMeasurements.HasFullBatteryMeasurementSinceAsync(
            sensor.Id,
            latestReplacement.PerformedAt,
            cancellationToken);

        if (hasResetMeasurement)
        {
            return previous;
        }

        return new SensorMeasurement
        {
            SensorId = sensor.Id,
            BatteryLevel = 100m,
            MeasurementDate = latestReplacement.PerformedAt,
            Temperature = previous?.Temperature,
            Humidity = previous?.Humidity,
            Pressure = previous?.Pressure,
            SignalStrength = previous?.SignalStrength
        };
    }

    private async Task WarnIfCalibrationDueAsync(Sensor sensor, CancellationToken cancellationToken)
    {
        var dueAt = await ResolveCalibrationDueDateAsync(sensor, cancellationToken);
        if (!dueAt.HasValue)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var warningFrom = dueAt.Value.AddDays(-_settings.CalibrationWarningDays);

        if (now >= dueAt.Value)
        {
            _logger.LogWarning(
                "Calibration overdue. SensorId={SensorId}, SensorName={SensorName}, DueAt={DueAt:u}",
                sensor.Id,
                sensor.Name,
                dueAt.Value);
        }
        else if (now >= warningFrom)
        {
            _logger.LogWarning(
                "Calibration due soon. SensorId={SensorId}, SensorName={SensorName}, DueAt={DueAt:u}",
                sensor.Id,
                sensor.Name,
                dueAt.Value);
        }
    }

    private async Task<DateTime?> ResolveCalibrationDueDateAsync(Sensor sensor, CancellationToken cancellationToken)
    {
        var latestCalibration = await _unitOfWork.MaintenanceLogs.GetLatestBySensorIdAndActionTypeAsync(
            sensor.Id,
            MaintenanceActionType.Calibration,
            cancellationToken);

        if (latestCalibration?.NextDueDate is DateTime nextDueDate)
        {
            return nextDueDate;
        }

        if (sensor.DeviceModel.CalibrationPeriodDays is not int periodDays || periodDays <= 0)
        {
            return null;
        }

        var reference = sensor.LastCalibrationDate ?? sensor.CreatedAt;
        return reference.AddDays(periodDays);
    }
}
