namespace IoTSensorMonitoring.Application.DTOs;

public record FacilityDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string? City,
    string? Address,
    int FloorCount);

public record CreateFacilityRequest(Guid CompanyId, string Name, string? City, string? Address, int FloorCount = 1);

public record UpdateFacilityRequest(string Name, string? City, string? Address, int FloorCount);
