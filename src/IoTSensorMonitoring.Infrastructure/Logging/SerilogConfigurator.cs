using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Configuration;

namespace IoTSensorMonitoring.Infrastructure.Logging;

public static class SerilogConfigurator
{
    public const string OutputTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    public static LoggerConfiguration ConfigureIoTLogging(
        this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        string applicationName)
    {
        var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        loggerConfiguration
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Connection", LogEventLevel.Fatal)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Query", LogEventLevel.Fatal)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", applicationName)
            .WriteTo.Console(
                theme: ColoredConsoleTheme.Instance,
                outputTemplate: OutputTemplate)
            .WriteTo.File(
                path: Path.Combine(logsDirectory, $"{applicationName.ToLowerInvariant()}-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: OutputTemplate);

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            loggerConfiguration.WriteTo.Sink(
                new PostgreSqlErrorLogSink(connectionString),
                restrictedToMinimumLevel: LogEventLevel.Error);
        }

        return loggerConfiguration;
    }
}
