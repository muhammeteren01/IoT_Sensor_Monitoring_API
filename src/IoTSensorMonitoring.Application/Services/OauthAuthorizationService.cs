using System.Security.Cryptography;
using System.Text;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Grafana;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Application.Settings;
using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;
using Microsoft.Extensions.Options;

namespace IoTSensorMonitoring.Application.Services;

public class OauthAuthorizationService : IOauthAuthorizationService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);

    private readonly IAuthCodeStore _authCodeStore;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IGrafanaTenantProvisioner _grafanaTenantProvisioner;
    private readonly GrafanaSettings _settings;

    public OauthAuthorizationService(
        IAuthCodeStore authCodeStore,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IGrafanaTenantProvisioner grafanaTenantProvisioner,
        IOptions<GrafanaSettings> settings)
    {
        _authCodeStore = authCodeStore;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _grafanaTenantProvisioner = grafanaTenantProvisioner;
        _settings = settings.Value;
    }

    public bool IsAllowedRedirectUri(string redirectUri) =>
        _settings.AllowedRedirectUris.Contains(redirectUri, StringComparer.Ordinal);

    public string CreateAuthorizationCode(Guid userId, string redirectUri, string? codeChallenge)
    {
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        _authCodeStore.Save(new AuthCodeEntry(
            code,
            userId,
            redirectUri,
            string.IsNullOrWhiteSpace(codeChallenge) ? null : codeChallenge,
            DateTime.UtcNow.Add(CodeLifetime)));
        return code;
    }

    public async Task<AuthResponse> ExchangeCodeAsync(
        string code,
        string redirectUri,
        string? codeVerifier,
        CancellationToken cancellationToken = default)
    {
        if (!_authCodeStore.TryTake(code, out var entry) || entry.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid or expired authorization code.");
        }

        if (!string.Equals(entry.RedirectUri, redirectUri, StringComparison.Ordinal))
        {
            throw new UnauthorizedException("Redirect URI mismatch.");
        }

        if (entry.CodeChallenge is not null && !MatchesPkce(entry.CodeChallenge, codeVerifier))
        {
            throw new UnauthorizedException("PKCE verification failed.");
        }

        var user = await _userRepository.GetByIdAsync(entry.UserId, cancellationToken)
            ?? throw new UnauthorizedException("User not found.");

        if (!user.IsActive)
        {
            throw new UnauthorizedException("User account is inactive.");
        }

        var token = _tokenService.CreateToken(user, out var expiresAt);
        return new AuthResponse(
            token,
            expiresAt,
            user.Id,
            user.CompanyId,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role);
    }

    public async Task<GrafanaUserInfoDto> GetUserInfoAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        if (!user.IsActive)
        {
            throw new UnauthorizedException("User account is inactive.");
        }

        if (user.Role == UserRole.SuperAdmin || !user.CompanyId.HasValue)
        {
            return Map(user, GrafanaSettings.MainOrgName, "GrafanaAdmin", [GrafanaSettings.MainOrgName]);
        }

        var company = await _unitOfWork.Companies.GetByIdAsync(user.CompanyId.Value, cancellationToken)
            ?? throw new ForbiddenException("User company was not found.");

        if (!company.IsActive)
        {
            throw new ForbiddenException("Company account is inactive.");
        }

        try
        {
            await _grafanaTenantProvisioner.EnsureUserAccessAsync(user, cancellationToken);
        }
        catch
        {
            // Login must succeed even if Grafana org/datasource sync is down.
        }

        var orgName = GrafanaOrgNaming.ForCompany(company);
        return Map(user, orgName, "Viewer", [orgName]);
    }

    private static GrafanaUserInfoDto Map(
        User user,
        string grafanaOrg,
        string role,
        IReadOnlyList<string> orgs) =>
        new(
            user.Id.ToString(),
            user.Email,
            $"{user.FirstName} {user.LastName}",
            user.Email,
            role,
            grafanaOrg,
            orgs);

    private static bool MatchesPkce(string challenge, string? verifier)
    {
        if (string.IsNullOrWhiteSpace(verifier))
        {
            return false;
        }

        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        var computed = Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computed),
            Encoding.ASCII.GetBytes(challenge));
    }
}
