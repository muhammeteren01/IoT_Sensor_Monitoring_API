namespace IoTSensorMonitoring.Application.Settings;

public class WorkerSettings
{
    public const string SectionName = "WorkerSettings";

    public int IntervalSeconds { get; set; } = 10;
    public int CalibrationWarningDays { get; set; } = 7;
}
