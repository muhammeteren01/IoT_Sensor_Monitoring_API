using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.Simulation;

public static class SupportedMetricsParser
{
    public static IReadOnlySet<SensorMetric> Parse(string? supportedMetrics)
    {
        var metrics = new HashSet<SensorMetric>();
        if (string.IsNullOrWhiteSpace(supportedMetrics))
        {
            return metrics;
        }

        foreach (var part in supportedMetrics.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<SensorMetric>(part, ignoreCase: true, out var metric))
            {
                metrics.Add(metric);
            }
        }

        return metrics;
    }
}
