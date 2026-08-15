using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentValidation;
using IoTSensorMonitoring.Api;
using IoTSensorMonitoring.Api.Data;
using IoTSensorMonitoring.Application.DependencyResolvers;
using IoTSensorMonitoring.Application.Validations.Auth;
using IoTSensorMonitoring.Infrastructure;
using IoTSensorMonitoring.Infrastructure.DependencyResolvers;
using IoTSensorMonitoring.Infrastructure.Logging;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(theme: ColoredConsoleTheme.Instance, outputTemplate: SerilogConfigurator.OutputTemplate)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
    {
        containerBuilder.RegisterModule(new AutofacInfrastructureModule());
        containerBuilder.RegisterModule(new AutofacApplicationModule());
    });

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ConfigureIoTLogging(context.Configuration, "Api")
            .ReadFrom.Services(services));

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddGlobalExceptionHandling();
    builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddSwaggerDocumentation();

    var app = builder.Build();

    app.UseApiRequestLogging();
    app.UseExceptionHandler();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IoT Sensor Monitoring API v1");
        options.RoutePrefix = string.Empty;
    });

    if (!string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase))
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

    await DatabaseInitializer.MigrateAsync(app.Services);
    await DbSeeder.SeedSuperAdminAsync(app.Services);

    Log.Information("API starting. Environment={Environment}", app.Environment.EnvironmentName);
    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
