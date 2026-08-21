namespace IoTSensorMonitoring.Worker.Integration.Contracts;

/// <summary>POST /api/sensor-measurements istek gövdesi — offline queue flush için.</summary>
public record CreateMeasurementContract(
    Guid SensorId,
    decimal? Temperature,
    decimal? Humidity,
    decimal? Pressure,
    decimal? BatteryLevel,
    int? SignalStrength,
    DateTime? MeasurementDate);
