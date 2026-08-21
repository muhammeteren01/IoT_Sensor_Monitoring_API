using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using IoTSensorMonitoring.Worker.Integration.Contracts;
using IoTSensorMonitoring.Worker.Settings;
using Microsoft.Extensions.Options;

namespace IoTSensorMonitoring.Worker.Integration.Api;

public interface IIntegrationApiClient
{
    Task<bool> IsApiReachableAsync(CancellationToken cancellationToken = default);

    Task<string> GetAccessTokenAsync(
        IntegrationClientSettings client,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SyncSensorContract>> GetSensorsAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    Task PostMeasurementAsync(
        string accessToken,
        CreateMeasurementContract measurement,
        CancellationToken cancellationToken = default);
}

public sealed class IntegrationApiClient : IIntegrationApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;
    private readonly IntegrationTokenCache _tokenCache;
    private readonly ILogger<IntegrationApiClient> _logger;

    public IntegrationApiClient(
        HttpClient httpClient,
        IntegrationTokenCache tokenCache,
        IOptions<IntegrationSettings> settings,
        ILogger<IntegrationApiClient> logger)
    {
        _httpClient = httpClient;
        _tokenCache = tokenCache;
        _logger = logger;

        var baseUrl = settings.Value.ApiBaseUrl?.TrimEnd('/') ?? "http://localhost:8080";
        _httpClient.BaseAddress = new Uri(baseUrl + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<bool> IsApiReachableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug(exception, "API health check failed");
            return false;
        }
    }

    public async Task<string> GetAccessTokenAsync(
        IntegrationClientSettings client,
        CancellationToken cancellationToken = default)
    {
        if (_tokenCache.TryGet(client.ClientId, out var cached))
        {
            return cached;
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = client.ClientId,
            ["client_secret"] = client.ClientSecret
        });

        using var response = await _httpClient.PostAsync("oauth/token", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Token request failed for {ClientId}. Status={StatusCode}, Body={Body}",
                client.ClientId,
                (int)response.StatusCode,
                body);
            response.EnsureSuccessStatusCode();
        }

        var token = JsonSerializer.Deserialize<ClientCredentialsTokenContract>(body, JsonOptions)
            ?? throw new InvalidOperationException("Token response was empty.");

        _tokenCache.Set(client.ClientId, token.AccessToken, TimeSpan.FromSeconds(Math.Max(60, token.ExpiresIn - 60)));
        return token.AccessToken;
    }

    public async Task<IReadOnlyList<SyncSensorContract>> GetSensorsAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/sensors");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Sensor sync failed. Status={StatusCode}, Body={Body}",
                (int)response.StatusCode,
                body);
            response.EnsureSuccessStatusCode();
        }

        var sensors = JsonSerializer.Deserialize<List<SyncSensorContract>>(body, JsonOptions) ?? [];
        return sensors;
    }

    public async Task PostMeasurementAsync(
        string accessToken,
        CreateMeasurementContract measurement,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/sensor-measurements");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(measurement, options: JsonOptions);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "Measurement flush failed for sensor {SensorId}. Status={StatusCode}, Body={Body}",
            measurement.SensorId,
            (int)response.StatusCode,
            body);
        response.EnsureSuccessStatusCode();
    }
}

public sealed class IntegrationTokenCache
{
    private readonly object _lock = new();
    private readonly Dictionary<string, (string Token, DateTime ExpiresAtUtc)> _tokens = new(StringComparer.Ordinal);

    public bool TryGet(string clientId, out string token)
    {
        lock (_lock)
        {
            if (_tokens.TryGetValue(clientId, out var entry) && entry.ExpiresAtUtc > DateTime.UtcNow)
            {
                token = entry.Token;
                return true;
            }

            token = string.Empty;
            return false;
        }
    }

    public void Set(string clientId, string token, TimeSpan lifetime)
    {
        lock (_lock)
        {
            _tokens[clientId] = (token, DateTime.UtcNow.Add(lifetime));
        }
    }

    public void Invalidate(string clientId)
    {
        lock (_lock)
        {
            _tokens.Remove(clientId);
        }
    }
}
