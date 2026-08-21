namespace IoTSensorMonitoring.Worker.Settings;

public class WorkerExecutionSettings
{
    public const string SectionName = "WorkerExecution";

    public WorkerExecutionMode Mode { get; set; } = WorkerExecutionMode.DirectDb;
}
