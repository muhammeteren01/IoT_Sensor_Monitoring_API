using System.Security.Cryptography;

namespace IoTSensorMonitoring.Application.Services;

public static class IntegrationClientCredentials
{
    public static (string ClientId, string ClientSecret) Create(Guid companyId)
    {
        var companyPart = companyId.ToString("N")[..8];
        var randomPart = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
        var clientId = $"iot_{companyPart}_{randomPart}";
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return (clientId, secret);
    }
}
