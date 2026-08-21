using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs.Users;

public record UserDto(
    Guid Id,
    Guid? CompanyId,
    string FirstName,
    string LastName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt);
