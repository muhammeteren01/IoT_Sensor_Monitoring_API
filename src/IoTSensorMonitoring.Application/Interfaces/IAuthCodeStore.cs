namespace IoTSensorMonitoring.Application.Interfaces;

public sealed record AuthCodeEntry(
    string Code,
    Guid UserId,
    string RedirectUri,
    string? CodeChallenge,
    DateTime ExpiresAt);

public interface IAuthCodeStore
{
    void Save(AuthCodeEntry entry);
    bool TryTake(string code, out AuthCodeEntry entry);
}
