using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.Simulation;

public sealed class MeasurementGenerator
{
    private readonly Random _random;

    public MeasurementGenerator(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public SensorMeasurement Next(Guid sensorId, SensorMeasurement? previous, IReadOnlySet<SensorMetric> metrics)
    {
        var measurement = new SensorMeasurement
        {
            SensorId = sensorId,
            MeasurementDate = DateTime.UtcNow
        };

        if (metrics.Contains(SensorMetric.Temperature))
        {
            measurement.Temperature = Walk(previous?.Temperature, 22m, 0.4m, 10m, 45m);
        }

        if (metrics.Contains(SensorMetric.Humidity))
        {
            measurement.Humidity = Walk(previous?.Humidity, 50m, 1.2m, 15m, 95m);
        }

        if (metrics.Contains(SensorMetric.Pressure))
        {
            measurement.Pressure = Walk(previous?.Pressure, 1013.25m, 0.6m, 980m, 1040m);
        }

        if (metrics.Contains(SensorMetric.BatteryLevel))
        {
            var last = previous?.BatteryLevel ?? 100m;
            var drain = (decimal)(_random.NextDouble() * 0.12 + 0.03);
            measurement.BatteryLevel = Clamp(last - drain, 0m, 100m);
        }

        if (metrics.Contains(SensorMetric.SignalStrength))
        {
            var last = previous?.SignalStrength ?? -55;
            var next = last + _random.Next(-4, 5);
            measurement.SignalStrength = Math.Clamp(next, -95, -25);
        }

        return measurement;
    }

    private decimal Walk(decimal? last, decimal seed, decimal step, decimal min, decimal max)
    {
        var baseline = last ?? seed;
        var delta = (decimal)((_random.NextDouble() * 2d) - 1d) * step;
        return Clamp(baseline + delta, min, max);
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return Math.Round(value, 2);
    }
}
