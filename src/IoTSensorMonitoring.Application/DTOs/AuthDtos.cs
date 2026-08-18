using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs;

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    Guid UserId,
    Guid? CompanyId,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role);
