using FluentValidation;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IoTSensorMonitoring.Application.Services;

public class CompanyService : ICompanyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGrafanaTenantProvisioner _grafanaTenantProvisioner;
    private readonly IPasswordService _passwordService;
    private readonly IValidator<CreateCompanyRequest> _createValidator;
    private readonly IValidator<UpdateCompanyRequest> _updateValidator;
    private readonly ILogger<CompanyService> _logger;

    public CompanyService(
        IUnitOfWork unitOfWork,
        IGrafanaTenantProvisioner grafanaTenantProvisioner,
        IPasswordService passwordService,
        IValidator<CreateCompanyRequest> createValidator,
        IValidator<UpdateCompanyRequest> updateValidator,
        ILogger<CompanyService> logger)
    {
        _unitOfWork = unitOfWork;
        _grafanaTenantProvisioner = grafanaTenantProvisioner;
        _passwordService = passwordService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<CompanyCreatedDto> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        var company = new Company
        {
            Name = request.Name,
            ContactEmail = request.ContactEmail,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var (clientId, clientSecret) = IntegrationClientCredentials.Create(company.Id);
        var integrationClient = new IntegrationClient
        {
            CompanyId = company.Id,
            ClientId = clientId,
            ClientSecretHash = _passwordService.HashPassword(clientSecret),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Companies.AddAsync(company, cancellationToken);
        await _unitOfWork.IntegrationClients.AddAsync(integrationClient, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await TryProvisionAsync(company, cancellationToken);

        return new CompanyCreatedDto(
            company.Id,
            company.Name,
            company.ContactEmail,
            company.IsActive,
            company.CreatedAt,
            clientId,
            clientSecret);
    }

    public async Task<IReadOnlyList<CompanyDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var companies = await _unitOfWork.Companies.GetAllAsync(cancellationToken);
        return companies.Select(Map).ToList();
    }

    public async Task<CompanyDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await GetRequiredAsync(id, cancellationToken);
        return Map(company);
    }

    public async Task<CompanyDto> UpdateAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_updateValidator, request, cancellationToken);

        var company = await GetRequiredAsync(id, cancellationToken);
        company.Name = request.Name;
        company.ContactEmail = request.ContactEmail;
        company.IsActive = request.IsActive;

        _unitOfWork.Companies.Update(company);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await TryProvisionAsync(company, cancellationToken);

        return Map(company);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await GetRequiredAsync(id, cancellationToken);

        if (await _unitOfWork.Users.AnyAsync(user => user.CompanyId == id, cancellationToken))
        {
            throw new ConflictException("Company cannot be deleted while it still has users.");
        }

        if (await _unitOfWork.DeviceModels.AnyAsync(model => model.CompanyId == id, cancellationToken))
        {
            throw new ConflictException("Company cannot be deleted while it still has device models.");
        }

        if (await _unitOfWork.Sensors.ExistsInCompanyAsync(id, cancellationToken))
        {
            throw new ConflictException("Company cannot be deleted while it still has sensors.");
        }

        await TryDeprovisionAsync(company, cancellationToken);

        _unitOfWork.Companies.Remove(company);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task TryProvisionAsync(Company company, CancellationToken cancellationToken)
    {
        try
        {
            await _grafanaTenantProvisioner.ProvisionAsync(company, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Grafana provision deferred for company {CompanyId} ({CompanyName})",
                company.Id,
                company.Name);
        }
    }

    private async Task TryDeprovisionAsync(Company company, CancellationToken cancellationToken)
    {
        try
        {
            await _grafanaTenantProvisioner.DeprovisionAsync(company, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Grafana deprovision failed for company {CompanyId} ({CompanyName})",
                company.Id,
                company.Name);
        }
    }

    private async Task<Company> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Companies.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), id);
    }

    private static CompanyDto Map(Company company) =>
        new(company.Id, company.Name, company.ContactEmail, company.IsActive, company.CreatedAt);
}
