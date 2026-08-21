namespace IoTSensorMonitoring.Application.DTOs.Companies;

public record UpdateCompanyRequest(string Name, string? ContactEmail, bool IsActive);
