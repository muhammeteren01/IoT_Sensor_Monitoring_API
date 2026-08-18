using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs;

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid? CompanyId,
    UserRole Role = UserRole.Operator);

public record UserDto(
    Guid Id,
    Guid? CompanyId,
    string FirstName,
    string LastName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt);
