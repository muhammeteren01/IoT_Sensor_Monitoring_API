namespace IoTSensorMonitoring.Application.DTOs.Auth;

public record ClientCredentialsTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn);
