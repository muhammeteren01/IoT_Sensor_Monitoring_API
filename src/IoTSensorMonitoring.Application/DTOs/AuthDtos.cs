using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid? CompanyId,
    UserRole Role = UserRole.Operator);

public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    Guid UserId,
    Guid? CompanyId,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role);
