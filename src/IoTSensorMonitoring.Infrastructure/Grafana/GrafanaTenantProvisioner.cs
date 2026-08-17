using IoTSensorMonitoring.Application.Grafana;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Settings;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IoTSensorMonitoring.Infrastructure.Grafana;

public sealed class GrafanaTenantProvisioner : IGrafanaTenantProvisioner
{
    private readonly IGrafanaAdminClient _grafana;
    private readonly IGrafanaDatabaseRoleService _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly GrafanaSettings _settings;
    private readonly ILogger<GrafanaTenantProvisioner> _logger;

    public GrafanaTenantProvisioner(
        IGrafanaAdminClient grafana,
        IGrafanaDatabaseRoleService roles,
        IUnitOfWork unitOfWork,
        IOptions<GrafanaSettings> settings,
        ILogger<GrafanaTenantProvisioner> logger)
    {
        _grafana = grafana;
        _roles = roles;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task ProvisionAllAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        var companies = await _unitOfWork.Companies.GetAllAsync(cancellationToken);
        foreach (var listed in companies.Where(company => company.IsActive))
        {
            var company = await _unitOfWork.Companies.GetByIdAsync(listed.Id, cancellationToken);
            if (company is null)
            {
                continue;
            }

            try
            {
                await ProvisionAsync(company, cancellationToken);
            }
            catch (GrafanaAdminAuthException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Grafana tenant provision failed for company {CompanyId} ({CompanyName})",
                    company.Id,
                    company.Name);
            }
        }

        var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
        foreach (var user in users.Where(item => item.IsActive))
        {
            await EnsureUserAccessAsync(user, cancellationToken);
        }
    }

    public async Task ProvisionAsync(Company company, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.RolePasswordSecret))
        {
            throw new InvalidOperationException("GrafanaSettings.RolePasswordSecret is required.");
        }

        var password = GrafanaPostgresRole.Password(company.Id, _settings.RolePasswordSecret);
        await _roles.EnsureRoleAsync(company.Id, password, cancellationToken);

        var orgId = await _grafana.EnsureOrganizationAsync(
            GrafanaOrgNaming.ForCompany(company),
            company.GrafanaOrgId,
            company.Id,
            cancellationToken);
        await _grafana.EnsurePostgresDatasourceAsync(
            orgId,
            GrafanaPostgresRole.RoleName(company.Id),
            password,
            cancellationToken);
        await _grafana.EnsureDashboardAsync(orgId, cancellationToken);

        if (company.GrafanaOrgId != orgId)
        {
            company.GrafanaOrgId = orgId;
            _unitOfWork.Companies.Update(company);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task EnsureUserAccessAsync(User user, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        try
        {
            if (user.Role == UserRole.SuperAdmin || !user.CompanyId.HasValue)
            {
                await _grafana.EnsureUserOrgMembershipAsync(user.Email, 1, "Admin", cancellationToken);
                return;
            }

            var company = await _unitOfWork.Companies.GetByIdAsync(user.CompanyId.Value, cancellationToken);
            if (company is null)
            {
                return;
            }

            await ProvisionAsync(company, cancellationToken);

            company = await _unitOfWork.Companies.GetByIdAsync(company.Id, cancellationToken);
            if (company?.GrafanaOrgId is not int orgId || orgId <= 1)
            {
                return;
            }

            await _grafana.EnsureUserOrgMembershipAsync(user.Email, orgId, "Viewer", cancellationToken);
            await _grafana.RemoveUserFromOtherOrgsAsync(user.Email, orgId, cancellationToken);
            await _grafana.SetUserActiveOrgAsync(user.Email, orgId, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Grafana user access sync deferred for {Email}", user.Email);
        }
    }

    public async Task DeprovisionAsync(Company company, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            await _roles.DropRoleAsync(company.Id, cancellationToken);
            return;
        }

        if (company.GrafanaOrgId is int orgId)
        {
            await _grafana.DeleteOrganizationAsync(orgId, cancellationToken);
        }

        await _roles.DropRoleAsync(company.Id, cancellationToken);
    }
}
