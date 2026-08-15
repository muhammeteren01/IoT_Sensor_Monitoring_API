using FluentValidation;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Services;

public class CompanyService : ICompanyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCompanyRequest> _createValidator;
    private readonly IValidator<UpdateCompanyRequest> _updateValidator;

    public CompanyService(
        IUnitOfWork unitOfWork,
        IValidator<CreateCompanyRequest> createValidator,
        IValidator<UpdateCompanyRequest> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<CompanyDto> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        var company = new Company
        {
            Name = request.Name,
            ContactEmail = request.ContactEmail,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Companies.AddAsync(company, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(company);
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

        return Map(company);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await GetRequiredAsync(id, cancellationToken);

        if (await _unitOfWork.Users.AnyAsync(user => user.CompanyId == id, cancellationToken))
        {
            throw new ConflictException("Company cannot be deleted while it still has users.");
        }

        if (await _unitOfWork.Sensors.ExistsInCompanyAsync(id, cancellationToken))
        {
            throw new ConflictException("Company cannot be deleted while it still has sensors.");
        }

        _unitOfWork.Companies.Remove(company);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Company> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Companies.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), id);
    }

    private static CompanyDto Map(Company company) =>
        new(company.Id, company.Name, company.ContactEmail, company.IsActive, company.CreatedAt);
}
