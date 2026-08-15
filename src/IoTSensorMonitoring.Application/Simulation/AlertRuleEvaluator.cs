using IoTSensorMonitoring.Domain.Entities;
using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.Simulation;

public static class AlertRuleEvaluator
{
    public static decimal? ReadValue(SensorMeasurement measurement, SensorMetric metric)
    {
        return metric switch
        {
            SensorMetric.Temperature => measurement.Temperature,
            SensorMetric.Humidity => measurement.Humidity,
            SensorMetric.Pressure => measurement.Pressure,
            SensorMetric.BatteryLevel => measurement.BatteryLevel,
            SensorMetric.SignalStrength => measurement.SignalStrength,
            _ => null
        };
    }

    public static bool IsTriggered(ComparisonOperator comparison, decimal value, decimal threshold)
    {
        return comparison switch
        {
            ComparisonOperator.GreaterThan => value > threshold,
            ComparisonOperator.LessThan => value < threshold,
            ComparisonOperator.GreaterOrEqual => value >= threshold,
            ComparisonOperator.LessOrEqual => value <= threshold,
            ComparisonOperator.Equal => value == threshold,
            _ => false
        };
    }

    public static string FormatMessage(SensorMetric metric, ComparisonOperator comparison, decimal threshold, decimal value)
    {
        var symbol = comparison switch
        {
            ComparisonOperator.GreaterThan => ">",
            ComparisonOperator.LessThan => "<",
            ComparisonOperator.GreaterOrEqual => ">=",
            ComparisonOperator.LessOrEqual => "<=",
            ComparisonOperator.Equal => "==",
            _ => comparison.ToString()
        };

        return $"{metric} {symbol} {threshold.ToString(System.Globalization.CultureInfo.InvariantCulture)} (value: {value.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
    }
}
