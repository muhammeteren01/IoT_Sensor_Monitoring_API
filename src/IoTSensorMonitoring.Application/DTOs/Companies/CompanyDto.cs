namespace IoTSensorMonitoring.Application.DTOs.Companies;

public record CompanyDto(
    Guid Id,
    string Name,
    string? ContactEmail,
    bool IsActive,
    DateTime CreatedAt);
