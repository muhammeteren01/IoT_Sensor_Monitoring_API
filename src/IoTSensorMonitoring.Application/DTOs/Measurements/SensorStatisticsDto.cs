namespace IoTSensorMonitoring.Application.DTOs.Measurements;

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
