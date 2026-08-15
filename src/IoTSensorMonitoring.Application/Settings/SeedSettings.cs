namespace IoTSensorMonitoring.Application.Settings;

public class SeedSettings
{
    public const string SectionName = "SeedSettings";

    public bool Enabled { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = "Super";
    public string LastName { get; set; } = "Admin";
}
