using IoTSensorMonitoring.Domain.Enums;
using IoTSensorMonitoring.Worker.Integration.Contracts;

namespace IoTSensorMonitoring.Worker.Integration.Store;

public sealed class QueuedMeasurement
{
    public long Id { get; init; }
    public required string ClientId { get; init; }
    public Guid SensorId { get; init; }
    public decimal? Temperature { get; init; }
    public decimal? Humidity { get; init; }
    public decimal? Pressure { get; init; }
    public decimal? BatteryLevel { get; init; }
    public int? SignalStrength { get; init; }
    public DateTime MeasurementDate { get; init; }
    public int AttemptCount { get; init; }
}

public sealed class LocalSensorSnapshot
{
    public Guid SensorId { get; init; }
    public required string Name { get; init; }
    public SensorStatus Status { get; init; }
    public required string SupportedMetrics { get; init; }
    public Guid DeviceModelId { get; init; }
    public DateTime? LastCalibrationDate { get; init; }
    public int? CalibrationPeriodDays { get; init; }
}

public sealed class LocalPreviousMeasurement
{
    public decimal? Temperature { get; init; }
    public decimal? Humidity { get; init; }
    public decimal? Pressure { get; init; }
    public decimal? BatteryLevel { get; init; }
    public int? SignalStrength { get; init; }
}

public interface ILocalQueueStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task ReplaceSensorsAsync(
        string clientId,
        IReadOnlyList<SyncSensorContract> sensors,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalSensorSnapshot>> GetActiveSensorsAsync(
        string clientId,
        CancellationToken cancellationToken = default);

    Task<DateTime?> GetLastSensorSyncAsync(
        string clientId,
        CancellationToken cancellationToken = default);

    Task SetLastSensorSyncAsync(
        string clientId,
        DateTime syncedAtUtc,
        CancellationToken cancellationToken = default);

    Task EnqueueMeasurementAsync(
        string clientId,
        CreateMeasurementContract measurement,
        CancellationToken cancellationToken = default);

    Task SaveLastMeasurementAsync(
        string clientId,
        CreateMeasurementContract measurement,
        CancellationToken cancellationToken = default);

    Task<LocalPreviousMeasurement?> GetLastMeasurementAsync(
        string clientId,
        Guid sensorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QueuedMeasurement>> DequeueBatchAsync(
        string clientId,
        int batchSize,
        int maxAttempts,
        CancellationToken cancellationToken = default);

    Task MarkFlushAttemptAsync(
        long id,
        string? error,
        CancellationToken cancellationToken = default);

    Task DeleteQueuedAsync(
        IReadOnlyList<long> ids,
        CancellationToken cancellationToken = default);

    Task<int> DeleteExhaustedAsync(
        string clientId,
        int maxAttempts,
        CancellationToken cancellationToken = default);

    Task<int> GetQueueCountAsync(
        string clientId,
        CancellationToken cancellationToken = default);
}
