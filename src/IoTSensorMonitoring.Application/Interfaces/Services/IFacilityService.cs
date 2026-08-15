using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Interfaces.Services;

public interface IFacilityService
{
    Task<FacilityDto> CreateAsync(CreateFacilityRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FacilityDto>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<FacilityDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FacilityDto> UpdateAsync(Guid id, UpdateFacilityRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
