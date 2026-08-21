namespace IoTSensorMonitoring.Application.DTOs.Facilities;

public record FacilityDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string? City,
    string? Address,
    int FloorCount);
