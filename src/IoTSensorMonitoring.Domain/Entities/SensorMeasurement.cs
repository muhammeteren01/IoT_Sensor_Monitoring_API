using IoTSensorMonitoring.Domain.Common;

namespace IoTSensorMonitoring.Domain.Entities;

public class SensorMeasurement : BaseEntity
{
    public Guid SensorId { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? Humidity { get; set; }
    public decimal? Pressure { get; set; }
    public decimal? BatteryLevel { get; set; }
    public int? SignalStrength { get; set; }
    public DateTime MeasurementDate { get; set; } = DateTime.UtcNow;

    public Sensor Sensor { get; set; } = null!;
}
