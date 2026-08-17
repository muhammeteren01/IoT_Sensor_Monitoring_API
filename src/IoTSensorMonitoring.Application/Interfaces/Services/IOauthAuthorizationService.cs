using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Interfaces.Services;

public interface IOauthAuthorizationService
{
    bool IsAllowedRedirectUri(string redirectUri);
    string CreateAuthorizationCode(Guid userId, string redirectUri, string? codeChallenge);
    Task<AuthResponse> ExchangeCodeAsync(
        string code,
        string redirectUri,
        string? codeVerifier,
        CancellationToken cancellationToken = default);
    Task<GrafanaUserInfoDto> GetUserInfoAsync(Guid userId, CancellationToken cancellationToken = default);
}

public record GrafanaUserInfoDto(
    string Sub,
    string Email,
    string Name,
    string Login,
    string Role,
    string GrafanaOrg,
    IReadOnlyList<string> Orgs);
