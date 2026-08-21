using IoTSensorMonitoring.Application.Simulation;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Worker.Integration.Api;
using IoTSensorMonitoring.Worker.Integration.Contracts;
using IoTSensorMonitoring.Worker.Integration.Store;
using IoTSensorMonitoring.Worker.Settings;
using Microsoft.Extensions.Options;

namespace IoTSensorMonitoring.Worker.Integration;

public interface IIntegrationPipeline
{
    Task RunCycleAsync(CancellationToken cancellationToken = default);
}

public sealed class IntegrationPipeline : IIntegrationPipeline
{
    private readonly ILocalQueueStore _store;
    private readonly IIntegrationApiClient _api;
    private readonly IntegrationTokenCache _tokenCache;
    private readonly MeasurementGenerator _generator;
    private readonly IntegrationSettings _settings;
    private readonly ILogger<IntegrationPipeline> _logger;

    public IntegrationPipeline(
        ILocalQueueStore store,
        IIntegrationApiClient api,
        IntegrationTokenCache tokenCache,
        MeasurementGenerator generator,
        IOptions<IntegrationSettings> settings,
        ILogger<IntegrationPipeline> logger)
    {
        _store = store;
        _api = api;
        _tokenCache = tokenCache;
        _generator = generator;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task RunCycleAsync(CancellationToken cancellationToken = default)
    {
        var clients = _settings.Clients
            .Where(client => !string.IsNullOrWhiteSpace(client.ClientId) && !string.IsNullOrWhiteSpace(client.ClientSecret))
            .ToList();

        if (clients.Count == 0)
        {
            _logger.LogWarning("Integration cycle skipped; no clients configured.");
            return;
        }

        var apiOnline = await _api.IsApiReachableAsync(cancellationToken);
        if (!apiOnline)
        {
            _logger.LogWarning("API health check failed; producing measurements into local queue only.");
        }

        foreach (var client in clients)
        {
            try
            {
                await RunClientCycleAsync(client, apiOnline, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Integration cycle failed for client {ClientId}", client.ClientId);
            }
        }
    }

    private async Task RunClientCycleAsync(
        IntegrationClientSettings client,
        bool apiOnline,
        CancellationToken cancellationToken)
    {
        if (apiOnline)
        {
            await TrySyncSensorsAsync(client, cancellationToken);
        }

        await ProduceMeasurementsAsync(client, cancellationToken);

        if (apiOnline)
        {
            await FlushQueueAsync(client, cancellationToken);
        }
        else
        {
            var pending = await _store.GetQueueCountAsync(client.ClientId, cancellationToken);
            _logger.LogInformation(
                "Flush skipped for {ClientId}; API offline. QueueCount={QueueCount}",
                client.ClientId,
                pending);
        }
    }

    private async Task TrySyncSensorsAsync(IntegrationClientSettings client, CancellationToken cancellationToken)
    {
        var syncInterval = TimeSpan.FromSeconds(Math.Max(30, _settings.SensorSyncIntervalSeconds));
        var lastSync = await _store.GetLastSensorSyncAsync(client.ClientId, cancellationToken);
        if (lastSync.HasValue && DateTime.UtcNow - lastSync.Value < syncInterval)
        {
            return;
        }

        try
        {
            var token = await _api.GetAccessTokenAsync(client, cancellationToken);
            var sensors = await _api.GetSensorsAsync(token, cancellationToken);
            await _store.ReplaceSensorsAsync(client.ClientId, sensors, cancellationToken);
            await _store.SetLastSensorSyncAsync(client.ClientId, DateTime.UtcNow, cancellationToken);

            _logger.LogInformation(
                "Sensor sync completed. ClientId={ClientId}, Count={Count}",
                client.ClientId,
                sensors.Count);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Sensor sync failed for {ClientId}; continuing with local cache.",
                client.ClientId);
        }
    }

    private async Task ProduceMeasurementsAsync(IntegrationClientSettings client, CancellationToken cancellationToken)
    {
        var sensors = await _store.GetActiveSensorsAsync(client.ClientId, cancellationToken);
        if (sensors.Count == 0)
        {
            _logger.LogInformation(
                "No active local sensors for {ClientId}; waiting for successful sync.",
                client.ClientId);
            return;
        }

        var warningDays = Math.Max(0, _settings.CalibrationWarningDays);

        foreach (var sensor in sensors)
        {
            var metrics = SupportedMetricsParser.Parse(sensor.SupportedMetrics);
            if (metrics.Count == 0)
            {
                _logger.LogWarning(
                    "Sensor {SensorId} ({SensorName}) skipped; SupportedMetrics empty: {SupportedMetrics}",
                    sensor.SensorId,
                    sensor.Name,
                    sensor.SupportedMetrics);
                continue;
            }

            WarnIfCalibrationDue(sensor, warningDays);

            var previousLocal = await _store.GetLastMeasurementAsync(client.ClientId, sensor.SensorId, cancellationToken);
            SensorMeasurement? previous = previousLocal is null
                ? null
                : new SensorMeasurement
                {
                    SensorId = sensor.SensorId,
                    Temperature = previousLocal.Temperature,
                    Humidity = previousLocal.Humidity,
                    Pressure = previousLocal.Pressure,
                    BatteryLevel = previousLocal.BatteryLevel,
                    SignalStrength = previousLocal.SignalStrength
                };

            var generated = _generator.Next(sensor.SensorId, previous, metrics);
            var contract = new CreateMeasurementContract(
                generated.SensorId,
                generated.Temperature,
                generated.Humidity,
                generated.Pressure,
                generated.BatteryLevel,
                generated.SignalStrength,
                generated.MeasurementDate);

            await _store.EnqueueMeasurementAsync(client.ClientId, contract, cancellationToken);
            await _store.SaveLastMeasurementAsync(client.ClientId, contract, cancellationToken);

            _logger.LogInformation(
                "Queued measurement. ClientId={ClientId}, SensorId={SensorId}, SensorName={SensorName}, MeasurementDate={MeasurementDate}, Temperature={Temperature}, Humidity={Humidity}",
                client.ClientId,
                sensor.SensorId,
                sensor.Name,
                contract.MeasurementDate,
                contract.Temperature,
                contract.Humidity);
        }
    }

    private async Task FlushQueueAsync(IntegrationClientSettings client, CancellationToken cancellationToken)
    {
        var batchSize = Math.Max(1, _settings.FlushBatchSize);
        var maxAttempts = Math.Max(1, _settings.MaxFlushAttempts);

        var exhausted = await _store.DeleteExhaustedAsync(client.ClientId, maxAttempts, cancellationToken);
        if (exhausted > 0)
        {
            _logger.LogWarning(
                "Discarded {Count} exhausted queue rows for {ClientId} (attempt_count >= {MaxAttempts}).",
                exhausted,
                client.ClientId,
                maxAttempts);
        }

        var pending = await _store.GetQueueCountAsync(client.ClientId, cancellationToken);
        if (pending == 0)
        {
            return;
        }

        string token;
        try
        {
            token = await _api.GetAccessTokenAsync(client, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Flush skipped for {ClientId}; token failed. QueueCount={QueueCount}",
                client.ClientId,
                pending);
            return;
        }

        var batch = await _store.DequeueBatchAsync(client.ClientId, batchSize, maxAttempts, cancellationToken);
        if (batch.Count == 0)
        {
            var leftover = await _store.DeleteExhaustedAsync(client.ClientId, maxAttempts, cancellationToken);
            if (leftover == 0)
            {
                _logger.LogWarning(
                    "Queue has {QueueCount} rows for {ClientId} but none are under MaxFlushAttempts={MaxAttempts}.",
                    pending,
                    client.ClientId,
                    maxAttempts);
            }

            return;
        }

        var flushedIds = new List<long>();
        var discardedIds = new List<long>();
        foreach (var item in batch)
        {
            var contract = new CreateMeasurementContract(
                item.SensorId,
                item.Temperature,
                item.Humidity,
                item.Pressure,
                item.BatteryLevel,
                item.SignalStrength,
                item.MeasurementDate);

            try
            {
                await _api.PostMeasurementAsync(token, contract, cancellationToken);
                flushedIds.Add(item.Id);
            }
            catch (HttpRequestException exception) when (IsUnauthorized(exception))
            {
                _tokenCache.Invalidate(client.ClientId);
                await _store.MarkFlushAttemptAsync(item.Id, exception.Message, cancellationToken);
                _logger.LogWarning(
                    exception,
                    "Flush unauthorized for {ClientId}; token invalidated. Remaining queue kept.",
                    client.ClientId);
                break;
            }
            catch (Exception exception)
            {
                var nextAttempt = item.AttemptCount + 1;
                await _store.MarkFlushAttemptAsync(item.Id, exception.Message, cancellationToken);
                _logger.LogWarning(
                    exception,
                    "Flush failed for {ClientId} queue id {QueueId} sensor {SensorId} (attempt {Attempt}/{MaxAttempts}). Skipping item, continuing batch.",
                    client.ClientId,
                    item.Id,
                    item.SensorId,
                    nextAttempt,
                    maxAttempts);

                if (nextAttempt >= maxAttempts)
                {
                    discardedIds.Add(item.Id);
                    _logger.LogWarning(
                        "Queue item discarded after max attempts. ClientId={ClientId}, QueueId={QueueId}, SensorId={SensorId}",
                        client.ClientId,
                        item.Id,
                        item.SensorId);
                }
            }
        }

        if (flushedIds.Count > 0)
        {
            await _store.DeleteQueuedAsync(flushedIds, cancellationToken);
        }

        if (discardedIds.Count > 0)
        {
            await _store.DeleteQueuedAsync(discardedIds, cancellationToken);
        }

        if (flushedIds.Count > 0 || discardedIds.Count > 0)
        {
            var remaining = await _store.GetQueueCountAsync(client.ClientId, cancellationToken);
            _logger.LogInformation(
                "Flush completed. ClientId={ClientId}, Flushed={Flushed}, Discarded={Discarded}, Remaining={Remaining}",
                client.ClientId,
                flushedIds.Count,
                discardedIds.Count,
                remaining);
        }
    }

    private void WarnIfCalibrationDue(LocalSensorSnapshot sensor, int warningDays)
    {
        if (!sensor.CalibrationPeriodDays.HasValue || !sensor.LastCalibrationDate.HasValue)
        {
            return;
        }

        var dueDate = sensor.LastCalibrationDate.Value.AddDays(sensor.CalibrationPeriodDays.Value);
        var daysLeft = (dueDate.Date - DateTime.UtcNow.Date).TotalDays;
        if (daysLeft < 0)
        {
            _logger.LogWarning(
                "Calibration overdue. SensorId={SensorId}, SensorName={SensorName}, DueDate={DueDate}",
                sensor.SensorId,
                sensor.Name,
                dueDate);
        }
        else if (daysLeft <= warningDays)
        {
            _logger.LogWarning(
                "Calibration due soon. SensorId={SensorId}, SensorName={SensorName}, DaysLeft={DaysLeft}",
                sensor.SensorId,
                sensor.Name,
                daysLeft);
        }
    }

    private static bool IsUnauthorized(HttpRequestException exception) =>
        exception.StatusCode == System.Net.HttpStatusCode.Unauthorized
        || exception.Message.Contains("401", StringComparison.Ordinal);
}
