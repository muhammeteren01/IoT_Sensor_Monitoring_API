using FluentValidation;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Services;

public class DeviceModelService : IDeviceModelService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateDeviceModelRequest> _createValidator;
    private readonly IValidator<UpdateDeviceModelRequest> _updateValidator;

    public DeviceModelService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateDeviceModelRequest> createValidator,
        IValidator<UpdateDeviceModelRequest> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<DeviceModelDto> CreateAsync(CreateDeviceModelRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        var companyId = TenantGuard.ResolveCompanyId(_currentUser, request.CompanyId);
        await EnsureCompanyExistsAsync(companyId, cancellationToken);

        var exists = await _unitOfWork.DeviceModels.AnyAsync(
            model => model.CompanyId == companyId
                     && model.Manufacturer == request.Manufacturer
                     && model.ModelNumber == request.ModelNumber,
            cancellationToken);

        if (exists)
        {
            throw new ConflictException($"Device model '{request.Manufacturer} {request.ModelNumber}' already exists for this company.");
        }

        var deviceModel = new DeviceModel
        {
            CompanyId = companyId,
            Manufacturer = request.Manufacturer,
            ModelNumber = request.ModelNumber,
            SupportedMetrics = request.SupportedMetrics,
            CalibrationPeriodDays = request.CalibrationPeriodDays
        };

        await _unitOfWork.DeviceModels.AddAsync(deviceModel, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(deviceModel);
    }

    public async Task<IReadOnlyList<DeviceModelDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var models = await _unitOfWork.DeviceModels.GetAllAsync(cancellationToken);
        return models.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<DeviceModelDto>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        await EnsureCompanyExistsAsync(companyId, cancellationToken);
        var models = await _unitOfWork.DeviceModels.FindAsync(
            model => model.CompanyId == companyId,
            cancellationToken);
        return models.Select(Map).ToList();
    }

    public async Task<DeviceModelDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Map(await GetRequiredAsync(id, cancellationToken));
    }

    public async Task<DeviceModelDto> UpdateAsync(Guid id, UpdateDeviceModelRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_updateValidator, request, cancellationToken);

        var deviceModel = await GetRequiredAsync(id, cancellationToken);

        var duplicate = await _unitOfWork.DeviceModels.AnyAsync(
            model => model.Id != id
                     && model.CompanyId == deviceModel.CompanyId
                     && model.Manufacturer == request.Manufacturer
                     && model.ModelNumber == request.ModelNumber,
            cancellationToken);

        if (duplicate)
        {
            throw new ConflictException($"Device model '{request.Manufacturer} {request.ModelNumber}' already exists for this company.");
        }

        deviceModel.Manufacturer = request.Manufacturer;
        deviceModel.ModelNumber = request.ModelNumber;
        deviceModel.SupportedMetrics = request.SupportedMetrics;
        deviceModel.CalibrationPeriodDays = request.CalibrationPeriodDays;

        _unitOfWork.DeviceModels.Update(deviceModel);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(deviceModel);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deviceModel = await GetRequiredAsync(id, cancellationToken);
        var inUse = await _unitOfWork.Sensors.AnyAsync(sensor => sensor.DeviceModelId == id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException("Device model cannot be deleted while sensors are using it.");
        }

        _unitOfWork.DeviceModels.Remove(deviceModel);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCompanyExistsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Companies.AnyAsync(company => company.Id == companyId, cancellationToken))
        {
            throw new NotFoundException(nameof(Company), companyId);
        }
    }

    private async Task<DeviceModel> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _unitOfWork.DeviceModels.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(DeviceModel), id);
    }

    private static DeviceModelDto Map(DeviceModel deviceModel) =>
        new(
            deviceModel.Id,
            deviceModel.CompanyId,
            deviceModel.Manufacturer,
            deviceModel.ModelNumber,
            deviceModel.SupportedMetrics,
            deviceModel.CalibrationPeriodDays);
}
