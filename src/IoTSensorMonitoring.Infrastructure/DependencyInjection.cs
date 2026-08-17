using IoTSensorMonitoring.Infrastructure.Persistence;
using IoTSensorMonitoring.Infrastructure.Grafana;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IoTSensorMonitoring.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(2, TimeSpan.FromSeconds(1), null)));

        services.Configure<GrafanaSettings>(configuration.GetSection(GrafanaSettings.SectionName));
        services.AddHttpClient<IGrafanaAdminClient, GrafanaAdminClient>();
        services.AddScoped<IGrafanaDatabaseRoleService, GrafanaDatabaseRoleService>();
        services.AddScoped<IGrafanaTenantProvisioner, GrafanaTenantProvisioner>();

        return services;
    }
}
