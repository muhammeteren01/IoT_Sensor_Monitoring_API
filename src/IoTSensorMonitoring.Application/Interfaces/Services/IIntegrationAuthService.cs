namespace IoTSensorMonitoring.Application.Interfaces.Services;

public interface IIntegrationAuthService
{
    Task<ClientCredentialsTokenResponse> IssueTokenAsync(
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default);
}
