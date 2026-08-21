namespace IoTSensorMonitoring.Application.DTOs.Companies;

public record CompanyCreatedDto(
    Guid Id,
    string Name,
    string? ContactEmail,
    bool IsActive,
    DateTime CreatedAt,
    string ClientId,
    string ClientSecret);
