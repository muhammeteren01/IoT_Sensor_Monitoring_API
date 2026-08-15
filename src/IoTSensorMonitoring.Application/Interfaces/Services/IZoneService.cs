using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Interfaces.Services;

public interface IZoneService
{
    Task<ZoneDto> CreateAsync(CreateZoneRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ZoneDto>> GetByFacilityIdAsync(Guid facilityId, CancellationToken cancellationToken = default);
    Task<ZoneDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ZoneDto> UpdateAsync(Guid id, UpdateZoneRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
