using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using IoTSensorMonitoring.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IoTSensorMonitoring.Infrastructure.Grafana;

public interface IGrafanaAdminClient
{
    Task<int> EnsureOrganizationAsync(string name, int? existingOrgId, Guid companyId, CancellationToken cancellationToken = default);
    Task RenameOrganizationAsync(int orgId, string name, CancellationToken cancellationToken = default);
    Task DeleteOrganizationAsync(int orgId, CancellationToken cancellationToken = default);
    Task EnsurePostgresDatasourceAsync(
        int orgId,
        string dbUser,
        string dbPassword,
        CancellationToken cancellationToken = default);
    Task EnsureDashboardAsync(int orgId, CancellationToken cancellationToken = default);
    Task EnsureUserOrgMembershipAsync(
        string email,
        int orgId,
        string role,
        CancellationToken cancellationToken = default);
    Task RemoveUserFromOrgAsync(string email, int orgId, CancellationToken cancellationToken = default);
    Task SetUserActiveOrgAsync(string email, int orgId, CancellationToken cancellationToken = default);
    Task RemoveUserFromOtherOrgsAsync(string email, int keepOrgId, CancellationToken cancellationToken = default);
}

public sealed class GrafanaAdminClient : IGrafanaAdminClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly GrafanaSettings _settings;
    private readonly ILogger<GrafanaAdminClient> _logger;

    public GrafanaAdminClient(HttpClient http, IOptions<GrafanaSettings> settings, ILogger<GrafanaAdminClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;

        _http.BaseAddress = new Uri(_settings.InternalUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(15);
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.AdminUser}:{_settings.AdminPassword}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
    }

    public async Task<int> EnsureOrganizationAsync(string name, int? existingOrgId, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (existingOrgId is int orgId && orgId > 1)
        {
            var byId = await FindOrgNameAsync(orgId, cancellationToken);
            if (byId is not null)
            {
                if (!string.Equals(byId, name, StringComparison.Ordinal))
                {
                    await RenameOrganizationAsync(orgId, name, cancellationToken);
                }

                return orgId;
            }
        }

        var existing = await FindOrgIdByNameAsync(name, cancellationToken);
        if (existing.HasValue)
        {
            return existing.Value;
        }

        return await CreateOrgWithUniqueNameAsync(name, companyId, cancellationToken);
    }

    private async Task<int> CreateOrgWithUniqueNameAsync(string name, Guid companyId, CancellationToken cancellationToken)
    {
        foreach (var candidate in new[] { name, $"{name} ({companyId.ToString("N")[..8]})" })
        {
            var existing = await FindOrgIdByNameAsync(candidate, cancellationToken);
            if (existing.HasValue)
            {
                return existing.Value;
            }

            using var createRequest = new HttpRequestMessage(HttpMethod.Post, "api/orgs")
            {
                Content = JsonBody(new { name = candidate })
            };
            using var response = await _http.SendAsync(createRequest, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureAdminAuthorized(response, body);
            if (response.IsSuccessStatusCode)
            {
                var created = JsonSerializer.Deserialize<GrafanaOrgCreated>(body, JsonOptions);
                return created?.OrgId ?? throw new InvalidOperationException("Grafana did not return orgId.");
            }
        }

        throw new InvalidOperationException($"Grafana org create failed for company {companyId}.");
    }

    private async Task<int?> FindOrgIdByNameAsync(string name, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync("api/orgs", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureAdminAuthorized(response, body);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Grafana org list failed ({(int)response.StatusCode}): {body}");
        }

        var orgs = JsonSerializer.Deserialize<List<GrafanaOrg>>(body, JsonOptions) ?? [];
        return orgs.FirstOrDefault(org => string.Equals(org.Name, name, StringComparison.Ordinal))?.Id;
    }

    private async Task<string?> FindOrgNameAsync(int orgId, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync($"api/orgs/{orgId}", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureAdminAuthorized(response, body);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var org = JsonSerializer.Deserialize<GrafanaOrg>(body, JsonOptions);
        return org?.Name;
    }

    private static void EnsureAdminAuthorized(HttpResponseMessage response, string body)
    {
        if ((int)response.StatusCode is 401 or 403)
        {
            throw new GrafanaAdminAuthException($"Grafana admin API unauthorized ({(int)response.StatusCode}): {body}");
        }
    }

    public async Task RenameOrganizationAsync(int orgId, string name, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/orgs/{orgId}")
        {
            Content = JsonBody(new { name })
        };
        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("Grafana org rename failed for {OrgId}: {Status} {Body}", orgId, (int)response.StatusCode, body);
    }

    public async Task DeleteOrganizationAsync(int orgId, CancellationToken cancellationToken = default)
    {
        if (orgId <= 1)
        {
            return;
        }

        using var response = await _http.DeleteAsync($"api/orgs/{orgId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Grafana org delete failed for {OrgId}: {Status} {Body}", orgId, (int)response.StatusCode, body);
        }
    }

    public async Task EnsurePostgresDatasourceAsync(
        int orgId,
        string dbUser,
        string dbPassword,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            uid = GrafanaSettings.DatasourceUid,
            name = GrafanaSettings.DatasourceName,
            type = "postgres",
            access = "proxy",
            url = $"{_settings.PostgresHost}:{_settings.PostgresPort}",
            user = dbUser,
            isDefault = true,
            jsonData = new
            {
                database = _settings.PostgresDatabase,
                sslmode = "disable",
                postgresVersion = 1600,
                timescaledb = false
            },
            secureJsonData = new { password = dbPassword }
        };

        using var get = WithOrg(HttpMethod.Get, $"api/datasources/uid/{GrafanaSettings.DatasourceUid}", orgId);
        using var existing = await _http.SendAsync(get, cancellationToken);
        if (existing.IsSuccessStatusCode)
        {
            using var put = WithOrg(HttpMethod.Put, $"api/datasources/uid/{GrafanaSettings.DatasourceUid}", orgId);
            put.Content = JsonBody(payload);
            using var updated = await _http.SendAsync(put, cancellationToken);
            await EnsureSuccess(updated, "datasource update", cancellationToken);
            return;
        }

        using var post = WithOrg(HttpMethod.Post, "api/datasources", orgId);
        post.Content = JsonBody(payload);
        using var created = await _http.SendAsync(post, cancellationToken);
        await EnsureSuccess(created, "datasource create", cancellationToken);
    }

    public async Task EnsureDashboardAsync(int orgId, CancellationToken cancellationToken = default)
    {
        using var get = WithOrg(HttpMethod.Get, $"api/dashboards/uid/{GrafanaSettings.DashboardUid}", 1);
        using var sourceResponse = await _http.SendAsync(get, cancellationToken);
        var sourceBody = await sourceResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!sourceResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Grafana main dashboard '{GrafanaSettings.DashboardUid}' is not ready ({(int)sourceResponse.StatusCode}): {sourceBody}");
        }

        using var document = JsonDocument.Parse(sourceBody);
        if (!document.RootElement.TryGetProperty("dashboard", out var dashboardElement))
        {
            throw new InvalidOperationException("Grafana dashboard payload is missing dashboard.");
        }

        var dashboard = JsonNode.Parse(dashboardElement.GetRawText()) as JsonObject
            ?? throw new InvalidOperationException("Grafana dashboard JSON is invalid.");
        dashboard["id"] = null;
        dashboard["uid"] = GrafanaSettings.DashboardUid;

        var payload = new JsonObject
        {
            ["dashboard"] = dashboard,
            ["overwrite"] = true,
            ["message"] = "PulseGrid tenant sync"
        };

        using var post = WithOrg(HttpMethod.Post, "api/dashboards/db", orgId);
        post.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        using var saved = await _http.SendAsync(post, cancellationToken);
        await EnsureSuccess(saved, "dashboard upsert", cancellationToken);
    }

    public async Task EnsureUserOrgMembershipAsync(
        string email,
        int orgId,
        string role,
        CancellationToken cancellationToken = default)
    {
        using var post = WithOrg(HttpMethod.Post, $"api/orgs/{orgId}/users", orgId);
        post.Content = JsonBody(new { loginOrEmail = email, role });
        using var response = await _http.SendAsync(post, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if ((int)response.StatusCode is 409 or 412)
        {
            _logger.LogDebug("Grafana org membership already exists for {Email} in org {OrgId}", email, orgId);
            return;
        }

        if ((int)response.StatusCode == 404)
        {
            _logger.LogDebug("Grafana user {Email} not created yet; org membership deferred", email);
            return;
        }

        throw new InvalidOperationException($"Grafana org membership failed ({(int)response.StatusCode}): {body}");
    }

    public async Task RemoveUserFromOrgAsync(string email, int orgId, CancellationToken cancellationToken = default)
    {
        var userId = await LookupUserIdAsync(email, cancellationToken);
        if (userId is null)
        {
            return;
        }

        using var delete = WithOrg(HttpMethod.Delete, $"api/orgs/{orgId}/users/{userId}", orgId);
        using var response = await _http.SendAsync(delete, cancellationToken);
        if (!response.IsSuccessStatusCode && (int)response.StatusCode != 404)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Grafana org remove failed for {Email} from org {OrgId}: {Status} {Body}",
                email,
                orgId,
                (int)response.StatusCode,
                body);
        }
    }

    public async Task SetUserActiveOrgAsync(string email, int orgId, CancellationToken cancellationToken = default)
    {
        var userId = await LookupUserIdAsync(email, cancellationToken);
        if (userId is null)
        {
            _logger.LogDebug("Grafana user {Email} not found; active org deferred", email);
            return;
        }

        using var response = await _http.PostAsync($"api/users/{userId}/using/{orgId}", null, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureAdminAuthorized(response, body);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Grafana switch org failed ({(int)response.StatusCode}): {body}");
        }
    }

    public async Task RemoveUserFromOtherOrgsAsync(string email, int keepOrgId, CancellationToken cancellationToken = default)
    {
        var userId = await LookupUserIdAsync(email, cancellationToken);
        if (userId is null)
        {
            return;
        }

        using var response = await _http.GetAsync($"api/users/{userId}/orgs", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureAdminAuthorized(response, body);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Grafana user orgs lookup failed for {Email}: {Status} {Body}",
                email,
                (int)response.StatusCode,
                body);
            return;
        }

        var orgs = JsonSerializer.Deserialize<List<GrafanaUserOrg>>(body, JsonOptions) ?? [];
        foreach (var org in orgs.Where(item => item.OrgId != keepOrgId))
        {
            await RemoveUserFromOrgAsync(email, org.OrgId, cancellationToken);
        }
    }

    private async Task<int?> LookupUserIdAsync(string email, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(
            $"api/users/lookup?loginOrEmail={Uri.EscapeDataString(email)}",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var user = JsonSerializer.Deserialize<GrafanaUserLookup>(body, JsonOptions);
        return user?.Id;
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private static HttpRequestMessage WithOrg(HttpMethod method, string path, int orgId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation("X-Grafana-Org-Id", orgId.ToString());
        return request;
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, string action, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"Grafana {action} failed ({(int)response.StatusCode}): {body}");
    }

    private sealed record GrafanaOrgCreated(int OrgId);
    private sealed record GrafanaOrg(int Id, string Name);
    private sealed record GrafanaUserLookup(int Id);
    private sealed record GrafanaUserOrg(int OrgId);
}
