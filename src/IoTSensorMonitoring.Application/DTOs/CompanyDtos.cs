namespace IoTSensorMonitoring.Application.DTOs;

public record CompanyDto(
    Guid Id,
    string Name,
    string? ContactEmail,
    bool IsActive,
    DateTime CreatedAt);

public record CreateCompanyRequest(string Name, string? ContactEmail);

public record UpdateCompanyRequest(string Name, string? ContactEmail, bool IsActive);
