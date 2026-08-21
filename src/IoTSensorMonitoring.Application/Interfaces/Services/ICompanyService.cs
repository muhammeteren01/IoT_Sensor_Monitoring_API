using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Interfaces.Services;

public interface ICompanyService
{
    Task<CompanyCreatedDto> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CompanyDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CompanyDto> UpdateAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
