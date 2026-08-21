namespace IoTSensorMonitoring.Application.DTOs.Facilities;

public record UpdateFacilityRequest(string Name, string? City, string? Address, int FloorCount);
