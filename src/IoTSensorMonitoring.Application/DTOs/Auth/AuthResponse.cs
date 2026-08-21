using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs.Auth;

public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    Guid UserId,
    Guid? CompanyId,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role);
