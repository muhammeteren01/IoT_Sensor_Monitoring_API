using FluentValidation;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Services;

public class ZoneService : IZoneService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateZoneRequest> _createValidator;
    private readonly IValidator<UpdateZoneRequest> _updateValidator;

    public ZoneService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateZoneRequest> createValidator,
        IValidator<UpdateZoneRequest> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ZoneDto> CreateAsync(CreateZoneRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        var facility = await GetFacilityRequiredAsync(request.FacilityId, cancellationToken);
        TenantGuard.EnsureCompanyAccess(_currentUser, facility.CompanyId);

        var zone = new Zone
        {
            FacilityId = request.FacilityId,
            Name = request.Name,
            FloorLevel = request.FloorLevel
        };

        await _unitOfWork.Zones.AddAsync(zone, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(zone);
    }

    public async Task<IReadOnlyList<ZoneDto>> GetByFacilityIdAsync(Guid facilityId, CancellationToken cancellationToken = default)
    {
        await EnsureFacilityExistsAsync(facilityId, cancellationToken);
        var zones = await _unitOfWork.Zones.GetByFacilityIdAsync(facilityId, cancellationToken);
        return zones.Select(Map).ToList();
    }

    public async Task<ZoneDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Map(await GetRequiredAsync(id, cancellationToken));
    }

    public async Task<ZoneDto> UpdateAsync(Guid id, UpdateZoneRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_updateValidator, request, cancellationToken);

        var zone = await GetRequiredAsync(id, cancellationToken);
        zone.Name = request.Name;
        zone.FloorLevel = request.FloorLevel;

        _unitOfWork.Zones.Update(zone);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(zone);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var zone = await GetRequiredAsync(id, cancellationToken);
        var sensors = await _unitOfWork.Sensors.GetByZoneIdAsync(id, cancellationToken);
        if (sensors.Count > 0)
        {
            throw new ConflictException("Zone cannot be deleted while it still has sensors.");
        }

        _unitOfWork.Zones.Remove(zone);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureFacilityExistsAsync(Guid facilityId, CancellationToken cancellationToken)
    {
        await GetFacilityRequiredAsync(facilityId, cancellationToken);
    }

    private async Task<Facility> GetFacilityRequiredAsync(Guid facilityId, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Facilities.GetByIdAsync(facilityId, cancellationToken)
            ?? throw new NotFoundException(nameof(Facility), facilityId);
    }

    private async Task<Zone> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Zones.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Zone), id);
    }

    private static ZoneDto Map(Zone zone) =>
        new(zone.Id, zone.FacilityId, zone.Name, zone.FloorLevel);
}
