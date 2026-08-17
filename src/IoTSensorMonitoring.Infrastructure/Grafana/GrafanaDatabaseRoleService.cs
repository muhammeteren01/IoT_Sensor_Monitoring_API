using IoTSensorMonitoring.Application.Grafana;
using IoTSensorMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IoTSensorMonitoring.Infrastructure.Grafana;

public interface IGrafanaDatabaseRoleService
{
    Task EnsureRoleAsync(Guid companyId, string password, CancellationToken cancellationToken = default);
    Task DropRoleAsync(Guid companyId, CancellationToken cancellationToken = default);
}

public sealed class GrafanaDatabaseRoleService : IGrafanaDatabaseRoleService
{
    private readonly AppDbContext _db;
    private readonly ILogger<GrafanaDatabaseRoleService> _logger;

    public GrafanaDatabaseRoleService(AppDbContext db, ILogger<GrafanaDatabaseRoleService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureRoleAsync(Guid companyId, string password, CancellationToken cancellationToken = default)
    {
        var roleName = GrafanaPostgresRole.RoleName(companyId);
        if (!GrafanaPostgresRole.IsManagedRoleName(roleName))
        {
            throw new InvalidOperationException($"Refusing to manage role '{roleName}'.");
        }

        await _db.Database.ExecuteSqlRawAsync(
            $"""
            DO $body$
            BEGIN
              IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{roleName}') THEN
                EXECUTE format('CREATE ROLE %I LOGIN PASSWORD %L IN ROLE grafana_reader', '{roleName}', '{password}');
              ELSE
                EXECUTE format('ALTER ROLE %I WITH LOGIN PASSWORD %L', '{roleName}', '{password}');
              END IF;
              EXECUTE format('ALTER ROLE %I SET app.company_id = %L', '{roleName}', '{companyId:N}');
            END
            $body$;
            """,
            cancellationToken);
    }

    public async Task DropRoleAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var roleName = GrafanaPostgresRole.RoleName(companyId);
        if (!GrafanaPostgresRole.IsManagedRoleName(roleName))
        {
            return;
        }

        try
        {
            await _db.Database.ExecuteSqlRawAsync(
                $"""
                DO $body$
                BEGIN
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{roleName}') THEN
                    EXECUTE format('DROP ROLE %I', '{roleName}');
                  END IF;
                END
                $body$;
                """,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not drop Grafana Postgres role {RoleName}", roleName);
        }
    }
}
