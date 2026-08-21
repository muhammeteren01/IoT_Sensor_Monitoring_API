using System.Text.Json.Serialization;

namespace IoTSensorMonitoring.Worker.Integration.Contracts;

public record ClientCredentialsTokenContract(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);
