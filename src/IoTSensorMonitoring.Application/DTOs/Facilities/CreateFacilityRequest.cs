namespace IoTSensorMonitoring.Application.DTOs.Facilities;

public record CreateFacilityRequest(Guid CompanyId, string Name, string? City, string? Address, int FloorCount = 1);
