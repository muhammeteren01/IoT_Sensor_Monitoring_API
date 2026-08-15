namespace IoTSensorMonitoring.Application.DTOs;

public record SensorMeasurementDto(
    Guid Id,
    Guid SensorId,
    decimal? Temperature,
    decimal? Humidity,
    decimal? Pressure,
    decimal? BatteryLevel,
    int? SignalStrength,
    DateTime MeasurementDate);

public record CreateSensorMeasurementRequest(
    Guid SensorId,
    decimal? Temperature,
    decimal? Humidity,
    decimal? Pressure,
    decimal? BatteryLevel,
    int? SignalStrength,
    DateTime? MeasurementDate);

public record SensorStatisticsDto(
    Guid SensorId,
    DateTime? From,
    DateTime? To,
    int TotalCount,
    decimal? AverageTemperature,
    decimal? MinTemperature,
    decimal? MaxTemperature,
    decimal? AverageHumidity,
    decimal? MinHumidity,
    decimal? MaxHumidity,
    decimal? MinPressure,
    decimal? MaxPressure);
