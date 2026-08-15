namespace IoTSensorMonitoring.Application.DTOs;

public record ZoneDto(
    Guid Id,
    Guid FacilityId,
    string Name,
    int FloorLevel);

public record CreateZoneRequest(Guid FacilityId, string Name, int FloorLevel);

public record UpdateZoneRequest(string Name, int FloorLevel);
