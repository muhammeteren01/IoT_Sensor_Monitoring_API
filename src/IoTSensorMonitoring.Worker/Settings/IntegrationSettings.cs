namespace IoTSensorMonitoring.Worker.Settings;

public class IntegrationSettings
{
    public const string SectionName = "IntegrationSettings";

    public string ApiBaseUrl { get; set; } = "http://localhost:8080";

    public string LocalStorePath { get; set; } = "data/worker-queue.db";

    /// <summary>Ölçüm üretim döngüsü (DirectDb WorkerSettings.IntervalSeconds ile aynı rol).</summary>
    public int IntervalSeconds { get; set; } = 10;

    /// <summary>Kalibrasyon yaklaşınca uyarı penceresi (gün).</summary>
    public int CalibrationWarningDays { get; set; } = 7;

    public int SensorSyncIntervalSeconds { get; set; } = 300;

    public int FlushBatchSize { get; set; } = 200;

    /// <summary>Başarısız flush sonrası en fazla kaç kez yeniden denensin.</summary>
    public int MaxFlushAttempts { get; set; } = 10;

    public List<IntegrationClientSettings> Clients { get; set; } = [];
}

public class IntegrationClientSettings
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;
}
