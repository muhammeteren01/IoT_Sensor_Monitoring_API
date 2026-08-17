using IoTSensorMonitoring.Application.Interfaces;

namespace IoTSensorMonitoring.Api.Services;

public sealed class MemoryAuthCodeStore : IAuthCodeStore
{
    private readonly Dictionary<string, AuthCodeEntry> _codes = [];
    private readonly object _gate = new();

    public void Save(AuthCodeEntry entry)
    {
        lock (_gate)
        {
            PruneExpired();
            _codes[entry.Code] = entry;
        }
    }

    public bool TryTake(string code, out AuthCodeEntry entry)
    {
        lock (_gate)
        {
            PruneExpired();
            if (_codes.Remove(code, out entry!))
            {
                return true;
            }

            entry = null!;
            return false;
        }
    }

    private void PruneExpired()
    {
        var now = DateTime.UtcNow;
        var expired = _codes.Where(pair => pair.Value.ExpiresAt < now).Select(pair => pair.Key).ToList();
        foreach (var key in expired)
        {
            _codes.Remove(key);
        }
    }
}
