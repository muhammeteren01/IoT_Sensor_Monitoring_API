using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Interfaces.Services;

public interface IDeviceModelService
{
    Task<DeviceModelDto> CreateAsync(CreateDeviceModelRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceModelDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceModelDto>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<DeviceModelDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DeviceModelDto> UpdateAsync(Guid id, UpdateDeviceModelRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
