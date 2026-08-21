namespace IoTSensorMonitoring.Application.DTOs.Measurements;

public record CreateSensorMeasurementRequest(
    Guid SensorId,
    decimal? Temperature,
    decimal? Humidity,
    decimal? Pressure,
    decimal? BatteryLevel,
    int? SignalStrength,
    DateTime? MeasurementDate);
