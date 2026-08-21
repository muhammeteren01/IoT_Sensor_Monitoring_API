using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Services;

namespace IoTSensorMonitoring.Application.Services;

public class IntegrationAuthService : IIntegrationAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public IntegrationAuthService(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<ClientCredentialsTokenResponse> IssueTokenAsync(
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new UnauthorizedException("Invalid client credentials.");
        }

        var client = await _unitOfWork.IntegrationClients.GetByClientIdAsync(clientId.Trim(), cancellationToken);
        if (client is null || !client.IsActive)
        {
            throw new UnauthorizedException("Invalid client credentials.");
        }

        if (client.Company is null || !client.Company.IsActive)
        {
            throw new UnauthorizedException("Company account is inactive.");
        }

        if (!_passwordService.VerifyPassword(clientSecret, client.ClientSecretHash))
        {
            throw new UnauthorizedException("Invalid client credentials.");
        }

        var token = _tokenService.CreateClientCredentialsToken(client, out var expiresAt);
        var expiresIn = Math.Max(60, (int)(expiresAt - DateTime.UtcNow).TotalSeconds);
        return new ClientCredentialsTokenResponse(token, "Bearer", expiresIn);
    }
}
