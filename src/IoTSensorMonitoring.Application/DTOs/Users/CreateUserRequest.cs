using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.DTOs.Users;

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid? CompanyId,
    UserRole Role = UserRole.Operator);
