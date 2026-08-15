using Npgsql;
using Serilog.Core;
using Serilog.Events;
using Serilog.Debugging;

namespace IoTSensorMonitoring.Infrastructure.Logging;

public sealed class PostgreSqlErrorLogSink : ILogEventSink
{
    private const string CreateTableSql =
        """
        CREATE TABLE IF NOT EXISTS error_logs (
            id uuid PRIMARY KEY,
            raised_at timestamptz NOT NULL,
            level varchar(16) NOT NULL,
            application varchar(64) NULL,
            source_context varchar(256) NULL,
            message text NOT NULL,
            exception text NULL
        );
        """;

    private const string InsertSql =
        """
        INSERT INTO error_logs (id, raised_at, level, application, source_context, message, exception)
        VALUES (@id, @raised_at, @level, @application, @source_context, @message, @exception);
        """;

    private readonly string _connectionString;
    private readonly Lock _ensureLock = new();
    private bool _tableEnsured;

    public PostgreSqlErrorLogSink(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = 2,
            CommandTimeout = 2
        };

        _connectionString = builder.ConnectionString;
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Error)
        {
            return;
        }

        if (GetProperty(logEvent, "SourceContext") is "Serilog.AspNetCore.RequestLoggingMiddleware")
        {
            return;
        }

        try
        {
            EnsureTable();

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = InsertSql;
            command.Parameters.AddWithValue("id", Guid.NewGuid());
            command.Parameters.AddWithValue("raised_at", logEvent.Timestamp.UtcDateTime);
            command.Parameters.AddWithValue("level", logEvent.Level.ToString());
            command.Parameters.AddWithValue("application", GetProperty(logEvent, "Application"));
            command.Parameters.AddWithValue("source_context", GetProperty(logEvent, "SourceContext"));
            command.Parameters.AddWithValue("message", logEvent.RenderMessage());
            command.Parameters.AddWithValue("exception", (object?)logEvent.Exception?.ToString() ?? DBNull.Value);
            command.ExecuteNonQuery();
        }
        catch (Exception exception)
        {
            SelfLog.WriteLine("Failed to write error log to PostgreSQL: {0}", exception.Message);
        }
    }

    private void EnsureTable()
    {
        if (_tableEnsured)
        {
            return;
        }

        lock (_ensureLock)
        {
            if (_tableEnsured)
            {
                return;
            }

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = CreateTableSql;
            command.ExecuteNonQuery();
            _tableEnsured = true;
        }
    }

    private static object GetProperty(LogEvent logEvent, string propertyName)
    {
        if (logEvent.Properties.TryGetValue(propertyName, out var value))
        {
            return value.ToString().Trim('"');
        }

        return DBNull.Value;
    }
}
