namespace IoTSensorMonitoring.Application.DTOs.Zones;

public record CreateZoneRequest(Guid FacilityId, string Name, int FloorLevel);
