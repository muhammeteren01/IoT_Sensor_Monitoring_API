using Autofac;
using Autofac.Extensions.DependencyInjection;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.DependencyResolvers;
using IoTSensorMonitoring.Application.Settings;
using IoTSensorMonitoring.Application.Simulation;
using IoTSensorMonitoring.Infrastructure;
using IoTSensorMonitoring.Infrastructure.DependencyResolvers;
using IoTSensorMonitoring.Infrastructure.Identity;
using IoTSensorMonitoring.Infrastructure.Logging;
using IoTSensorMonitoring.Worker.Extensions;
using IoTSensorMonitoring.Worker.Settings;
using Microsoft.Extensions.Options;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(theme: ColoredConsoleTheme.Instance, outputTemplate: SerilogConfigurator.OutputTemplate)
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    var executionMode = WorkerHostExtensions.ResolveExecutionMode(builder.Configuration);

    if (executionMode == WorkerExecutionMode.DirectDb)
    {
        builder.ConfigureContainer(new AutofacServiceProviderFactory(), containerBuilder =>
        {
            containerBuilder.RegisterModule(new AutofacInfrastructureModule());
            containerBuilder.RegisterModule(new AutofacApplicationModule());
        });

        builder.Services.Configure<WorkerSettings>(builder.Configuration.GetSection(WorkerSettings.SectionName));
        builder.Services.AddSingleton<ICurrentUser, SystemCurrentUser>();
        builder.Services.AddInfrastructure(builder.Configuration);
    }
    else
    {
        builder.Services.AddSingleton<MeasurementGenerator>();
    }

    builder.Services.AddSerilog((services, configuration) =>
        configuration
            .ConfigureIoTLogging(builder.Configuration, "Worker")
            .ReadFrom.Services(services));

    builder.Services.AddWorkerExecution(builder.Configuration);

    var host = builder.Build();

    Log.Information("Worker execution mode selected: {Mode}", executionMode);
    Log.Information("Worker Service host starting");
    host.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Worker Service terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
