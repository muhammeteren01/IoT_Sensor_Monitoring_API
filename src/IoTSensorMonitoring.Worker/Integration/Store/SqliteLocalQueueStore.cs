using System.Globalization;
using IoTSensorMonitoring.Domain.Enums;
using IoTSensorMonitoring.Worker.Integration.Contracts;
using IoTSensorMonitoring.Worker.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace IoTSensorMonitoring.Worker.Integration.Store;

public sealed class SqliteLocalQueueStore : ILocalQueueStore
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;

    public SqliteLocalQueueStore(IOptions<IntegrationSettings> settings)
    {
        var path = settings.Value.LocalStorePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = "data/worker-queue.db";
        }

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS synced_sensors (
                    client_id TEXT NOT NULL,
                    sensor_id TEXT NOT NULL,
                    name TEXT NOT NULL,
                    status TEXT NOT NULL,
                    supported_metrics TEXT NOT NULL,
                    device_model_id TEXT NOT NULL,
                    last_calibration_date TEXT NULL,
                    calibration_period_days INTEGER NULL,
                    synced_at TEXT NOT NULL,
                    PRIMARY KEY (client_id, sensor_id)
                );
                """, cancellationToken);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS last_measurements (
                    client_id TEXT NOT NULL,
                    sensor_id TEXT NOT NULL,
                    temperature TEXT NULL,
                    humidity TEXT NULL,
                    pressure TEXT NULL,
                    battery_level TEXT NULL,
                    signal_strength INTEGER NULL,
                    PRIMARY KEY (client_id, sensor_id)
                );
                """, cancellationToken);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS measurement_queue (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    client_id TEXT NOT NULL,
                    sensor_id TEXT NOT NULL,
                    temperature TEXT NULL,
                    humidity TEXT NULL,
                    pressure TEXT NULL,
                    battery_level TEXT NULL,
                    signal_strength INTEGER NULL,
                    measurement_date TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    attempt_count INTEGER NOT NULL DEFAULT 0,
                    last_attempt_at TEXT NULL,
                    last_error TEXT NULL
                );
                """, cancellationToken);

            await EnsureColumnAsync(connection, "measurement_queue", "attempt_count", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
            await EnsureColumnAsync(connection, "measurement_queue", "last_attempt_at", "TEXT NULL", cancellationToken);
            await EnsureColumnAsync(connection, "measurement_queue", "last_error", "TEXT NULL", cancellationToken);

            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS sync_state (
                    client_id TEXT NOT NULL PRIMARY KEY,
                    last_sensor_sync_at TEXT NULL
                );
                """, cancellationToken);

            await ExecuteAsync(connection, """
                CREATE INDEX IF NOT EXISTS ix_measurement_queue_client
                ON measurement_queue (client_id, id);
                """, cancellationToken);

            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReplaceSensorsAsync(
        string clientId,
        IReadOnlyList<SyncSensorContract> sensors,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

            await using (var delete = connection.CreateCommand())
            {
                delete.Transaction = tx;
                delete.CommandText = "DELETE FROM synced_sensors WHERE client_id = $clientId;";
                delete.Parameters.AddWithValue("$clientId", clientId);
                await delete.ExecuteNonQueryAsync(cancellationToken);
            }

            var syncedAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            foreach (var sensor in sensors)
            {
                await using var insert = connection.CreateCommand();
                insert.Transaction = tx;
                insert.CommandText = """
                    INSERT INTO synced_sensors (
                        client_id, sensor_id, name, status, supported_metrics,
                        device_model_id, last_calibration_date, calibration_period_days, synced_at)
                    VALUES (
                        $clientId, $sensorId, $name, $status, $supportedMetrics,
                        $deviceModelId, $lastCalibrationDate, $calibrationPeriodDays, $syncedAt);
                    """;
                insert.Parameters.AddWithValue("$clientId", clientId);
                insert.Parameters.AddWithValue("$sensorId", sensor.Id.ToString("D"));
                insert.Parameters.AddWithValue("$name", sensor.Name);
                insert.Parameters.AddWithValue("$status", sensor.Status.ToString());
                insert.Parameters.AddWithValue("$supportedMetrics", sensor.SupportedMetrics ?? string.Empty);
                insert.Parameters.AddWithValue("$deviceModelId", sensor.DeviceModelId.ToString("D"));
                insert.Parameters.AddWithValue(
                    "$lastCalibrationDate",
                    (object?)sensor.LastCalibrationDate?.ToString("O", CultureInfo.InvariantCulture) ?? DBNull.Value);
                insert.Parameters.AddWithValue(
                    "$calibrationPeriodDays",
                    (object?)sensor.CalibrationPeriodDays ?? DBNull.Value);
                insert.Parameters.AddWithValue("$syncedAt", syncedAt);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<LocalSensorSnapshot>> GetActiveSensorsAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT sensor_id, name, status, supported_metrics, device_model_id,
                       last_calibration_date, calibration_period_days
                FROM synced_sensors
                WHERE client_id = $clientId AND status = $active
                ORDER BY name;
                """;
            command.Parameters.AddWithValue("$clientId", clientId);
            command.Parameters.AddWithValue("$active", SensorStatus.Active.ToString());

            var list = new List<LocalSensorSnapshot>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new LocalSensorSnapshot
                {
                    SensorId = Guid.Parse(reader.GetString(0)),
                    Name = reader.GetString(1),
                    Status = Enum.Parse<SensorStatus>(reader.GetString(2)),
                    SupportedMetrics = reader.GetString(3),
                    DeviceModelId = Guid.Parse(reader.GetString(4)),
                    LastCalibrationDate = reader.IsDBNull(5)
                        ? null
                        : DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    CalibrationPeriodDays = reader.IsDBNull(6) ? null : reader.GetInt32(6)
                });
            }

            return list;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DateTime?> GetLastSensorSyncAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT last_sensor_sync_at FROM sync_state WHERE client_id = $clientId;
                """;
            command.Parameters.AddWithValue("$clientId", clientId);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            if (value is null or DBNull)
            {
                return null;
            }

            return DateTime.Parse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetLastSensorSyncAsync(
        string clientId,
        DateTime syncedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO sync_state (client_id, last_sensor_sync_at)
                VALUES ($clientId, $syncedAt)
                ON CONFLICT(client_id) DO UPDATE SET last_sensor_sync_at = excluded.last_sensor_sync_at;
                """;
            command.Parameters.AddWithValue("$clientId", clientId);
            command.Parameters.AddWithValue(
                "$syncedAt",
                syncedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task EnqueueMeasurementAsync(
        string clientId,
        CreateMeasurementContract measurement,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO measurement_queue (
                    client_id, sensor_id, temperature, humidity, pressure,
                    battery_level, signal_strength, measurement_date, created_at)
                VALUES (
                    $clientId, $sensorId, $temperature, $humidity, $pressure,
                    $batteryLevel, $signalStrength, $measurementDate, $createdAt);
                """;
            BindMeasurement(command, clientId, measurement);
            command.Parameters.AddWithValue(
                "$createdAt",
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveLastMeasurementAsync(
        string clientId,
        CreateMeasurementContract measurement,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO last_measurements (
                    client_id, sensor_id, temperature, humidity, pressure, battery_level, signal_strength)
                VALUES (
                    $clientId, $sensorId, $temperature, $humidity, $pressure, $batteryLevel, $signalStrength)
                ON CONFLICT(client_id, sensor_id) DO UPDATE SET
                    temperature = excluded.temperature,
                    humidity = excluded.humidity,
                    pressure = excluded.pressure,
                    battery_level = excluded.battery_level,
                    signal_strength = excluded.signal_strength;
                """;
            BindMeasurementCore(command, clientId, measurement);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LocalPreviousMeasurement?> GetLastMeasurementAsync(
        string clientId,
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT temperature, humidity, pressure, battery_level, signal_strength
                FROM last_measurements
                WHERE client_id = $clientId AND sensor_id = $sensorId;
                """;
            command.Parameters.AddWithValue("$clientId", clientId);
            command.Parameters.AddWithValue("$sensorId", sensorId.ToString("D"));

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new LocalPreviousMeasurement
            {
                Temperature = ReadDecimal(reader, 0),
                Humidity = ReadDecimal(reader, 1),
                Pressure = ReadDecimal(reader, 2),
                BatteryLevel = ReadDecimal(reader, 3),
                SignalStrength = reader.IsDBNull(4) ? null : reader.GetInt32(4)
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<QueuedMeasurement>> DequeueBatchAsync(
        string clientId,
        int batchSize,
        int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var list = new List<QueuedMeasurement>();
        var corrupt = new List<(long Id, int AttemptCount, string Error)>();
        var limit = Math.Max(1, maxAttempts);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT id, client_id, sensor_id, temperature, humidity, pressure,
                       battery_level, signal_strength, measurement_date, attempt_count
                FROM measurement_queue
                WHERE client_id = $clientId AND attempt_count < $maxAttempts
                ORDER BY id
                LIMIT $limit;
                """;
            command.Parameters.AddWithValue("$clientId", clientId);
            command.Parameters.AddWithValue("$maxAttempts", limit);
            command.Parameters.AddWithValue("$limit", Math.Max(1, batchSize));

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetInt64(0);
                var attemptCount = reader.IsDBNull(9) ? 0 : reader.GetInt32(9);
                var sensorIdRaw = reader.GetString(2);
                if (!Guid.TryParse(sensorIdRaw, out var sensorId))
                {
                    corrupt.Add((id, attemptCount, $"Invalid sensor_id: '{sensorIdRaw}'"));
                    continue;
                }

                var measurementDateRaw = reader.GetString(8);
                if (!DateTime.TryParse(
                        measurementDateRaw,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var measurementDate))
                {
                    corrupt.Add((id, attemptCount, $"Invalid measurement_date: '{measurementDateRaw}'"));
                    continue;
                }

                list.Add(new QueuedMeasurement
                {
                    Id = id,
                    ClientId = reader.GetString(1),
                    SensorId = sensorId,
                    Temperature = ReadDecimal(reader, 3),
                    Humidity = ReadDecimal(reader, 4),
                    Pressure = ReadDecimal(reader, 5),
                    BatteryLevel = ReadDecimal(reader, 6),
                    SignalStrength = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    MeasurementDate = measurementDate,
                    AttemptCount = attemptCount
                });
            }
        }
        finally
        {
            _gate.Release();
        }

        foreach (var item in corrupt)
        {
            await MarkFlushAttemptAsync(item.Id, item.Error, cancellationToken);
            if (item.AttemptCount + 1 >= limit)
            {
                await DeleteQueuedAsync([item.Id], cancellationToken);
            }
        }

        return list;
    }

    public async Task MarkFlushAttemptAsync(
        long id,
        string? error,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE measurement_queue
                SET attempt_count = attempt_count + 1,
                    last_attempt_at = $attemptedAt,
                    last_error = $error
                WHERE id = $id;
                """;
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue(
                "$attemptedAt",
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue(
                "$error",
                string.IsNullOrWhiteSpace(error) ? DBNull.Value : error.Length > 500 ? error[..500] : error);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> DeleteExhaustedAsync(
        string clientId,
        int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                DELETE FROM measurement_queue
                WHERE client_id = $clientId AND attempt_count >= $maxAttempts;
                """;
            command.Parameters.AddWithValue("$clientId", clientId);
            command.Parameters.AddWithValue("$maxAttempts", Math.Max(1, maxAttempts));
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteQueuedAsync(
        IReadOnlyList<long> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return;
        }

        await EnsureInitializedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

            foreach (var id in ids)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = tx;
                command.CommandText = "DELETE FROM measurement_queue WHERE id = $id;";
                command.Parameters.AddWithValue("$id", id);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> GetQueueCountAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM measurement_queue WHERE client_id = $clientId;";
            command.Parameters.AddWithValue("$clientId", clientId);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            await InitializeAsync(cancellationToken);
        }
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureColumnAsync(
        SqliteConnection connection,
        string tableName,
        string columnName,
        string columnSql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await reader.DisposeAsync();
        await ExecuteAsync(connection, $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnSql};", cancellationToken);
    }

    private static void BindMeasurement(
        SqliteCommand command,
        string clientId,
        CreateMeasurementContract measurement)
    {
        BindMeasurementCore(command, clientId, measurement);
        command.Parameters.AddWithValue(
            "$measurementDate",
            (measurement.MeasurementDate ?? DateTime.UtcNow).ToString("O", CultureInfo.InvariantCulture));
    }

    private static void BindMeasurementCore(
        SqliteCommand command,
        string clientId,
        CreateMeasurementContract measurement)
    {
        command.Parameters.AddWithValue("$clientId", clientId);
        command.Parameters.AddWithValue("$sensorId", measurement.SensorId.ToString("D"));
        command.Parameters.AddWithValue("$temperature", ToDb(measurement.Temperature));
        command.Parameters.AddWithValue("$humidity", ToDb(measurement.Humidity));
        command.Parameters.AddWithValue("$pressure", ToDb(measurement.Pressure));
        command.Parameters.AddWithValue("$batteryLevel", ToDb(measurement.BatteryLevel));
        command.Parameters.AddWithValue(
            "$signalStrength",
            (object?)measurement.SignalStrength ?? DBNull.Value);
    }

    private static object ToDb(decimal? value) =>
        value is null
            ? DBNull.Value
            : value.Value.ToString(CultureInfo.InvariantCulture);

    private static decimal? ReadDecimal(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return decimal.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture);
    }
}
