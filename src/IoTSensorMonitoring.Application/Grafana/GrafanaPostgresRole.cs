using System.Security.Cryptography;
using System.Text;

namespace IoTSensorMonitoring.Application.Grafana;

public static class GrafanaPostgresRole
{
    public static string RoleName(Guid companyId) => $"g_c_{companyId:N}";

    public static string Password(Guid companyId, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        var hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes(companyId.ToString("N")));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool IsManagedRoleName(string roleName) =>
        roleName.StartsWith("g_c_", StringComparison.Ordinal)
        && roleName.Length == 36
        && roleName[4..].All(static ch => char.IsAsciiHexDigit(ch));
}
