using Autofac;
using Autofac.Extensions.DependencyInjection;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.DependencyResolvers;
using IoTSensorMonitoring.Application.Settings;
using IoTSensorMonitoring.Infrastructure;
using IoTSensorMonitoring.Infrastructure.DependencyResolvers;
using IoTSensorMonitoring.Infrastructure.Identity;
using IoTSensorMonitoring.Infrastructure.Logging;
using IoTSensorMonitoring.Worker;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(theme: ColoredConsoleTheme.Instance, outputTemplate: SerilogConfigurator.OutputTemplate)
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.ConfigureContainer(new AutofacServiceProviderFactory(), containerBuilder =>
    {
        containerBuilder.RegisterModule(new AutofacInfrastructureModule());
        containerBuilder.RegisterModule(new AutofacApplicationModule());
    });

    builder.Services.AddSerilog((services, configuration) =>
        configuration
            .ConfigureIoTLogging(builder.Configuration, "Worker")
            .ReadFrom.Services(services));

    builder.Services.Configure<WorkerSettings>(builder.Configuration.GetSection(WorkerSettings.SectionName));
    builder.Services.AddSingleton<ICurrentUser, SystemCurrentUser>();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();
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
