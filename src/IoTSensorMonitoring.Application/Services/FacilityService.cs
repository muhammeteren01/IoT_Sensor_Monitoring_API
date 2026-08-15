using FluentValidation;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Services;

public class FacilityService : IFacilityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateFacilityRequest> _createValidator;
    private readonly IValidator<UpdateFacilityRequest> _updateValidator;

    public FacilityService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateFacilityRequest> createValidator,
        IValidator<UpdateFacilityRequest> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<FacilityDto> CreateAsync(CreateFacilityRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        var companyId = TenantGuard.ResolveCompanyId(_currentUser, request.CompanyId);
        await EnsureCompanyExistsAsync(companyId, cancellationToken);

        var facility = new Facility
        {
            CompanyId = companyId,
            Name = request.Name,
            City = request.City,
            Address = request.Address,
            FloorCount = request.FloorCount
        };

        await _unitOfWork.Facilities.AddAsync(facility, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(facility);
    }

    public async Task<IReadOnlyList<FacilityDto>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        await EnsureCompanyExistsAsync(companyId, cancellationToken);
        var facilities = await _unitOfWork.Facilities.GetByCompanyIdAsync(companyId, cancellationToken);
        return facilities.Select(Map).ToList();
    }

    public async Task<FacilityDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Map(await GetRequiredAsync(id, cancellationToken));
    }

    public async Task<FacilityDto> UpdateAsync(Guid id, UpdateFacilityRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_updateValidator, request, cancellationToken);

        var facility = await GetRequiredAsync(id, cancellationToken);
        facility.Name = request.Name;
        facility.City = request.City;
        facility.Address = request.Address;
        facility.FloorCount = request.FloorCount;

        _unitOfWork.Facilities.Update(facility);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(facility);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var facility = await GetRequiredAsync(id, cancellationToken);
        var zones = await _unitOfWork.Zones.GetByFacilityIdAsync(id, cancellationToken);

        foreach (var zone in zones)
        {
            var sensors = await _unitOfWork.Sensors.GetByZoneIdAsync(zone.Id, cancellationToken);
            if (sensors.Count > 0)
            {
                throw new ConflictException("Facility cannot be deleted while it still has sensors.");
            }
        }

        _unitOfWork.Facilities.Remove(facility);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCompanyExistsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Companies.AnyAsync(company => company.Id == companyId, cancellationToken))
        {
            throw new NotFoundException(nameof(Company), companyId);
        }
    }

    private async Task<Facility> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Facilities.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Facility), id);
    }

    private static FacilityDto Map(Facility facility) =>
        new(facility.Id, facility.CompanyId, facility.Name, facility.City, facility.Address, facility.FloorCount);
}
