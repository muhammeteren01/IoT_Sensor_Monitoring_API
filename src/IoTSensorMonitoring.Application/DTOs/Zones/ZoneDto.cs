namespace IoTSensorMonitoring.Application.DTOs.Zones;

public record ZoneDto(
    Guid Id,
    Guid FacilityId,
    string Name,
    int FloorLevel);
