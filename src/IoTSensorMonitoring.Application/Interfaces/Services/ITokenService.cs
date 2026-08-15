using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Interfaces.Services;

public interface ITokenService
{
    string CreateToken(User user, out DateTime expiresAt);
}
