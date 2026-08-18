using FluentValidation;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordService _passwordService;
    private readonly IValidator<CreateUserRequest> _createValidator;

    public UserService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IPasswordService passwordService,
        IValidator<CreateUserRequest> createValidator)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _passwordService = passwordService;
        _createValidator = createValidator;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            TenantGuard.EnsureCompanyAccess(_currentUser, companyId.Value);

            if (!await _unitOfWork.Companies.AnyAsync(company => company.Id == companyId.Value, cancellationToken))
            {
                throw new NotFoundException(nameof(Company), companyId.Value);
            }

            var byCompany = await _unitOfWork.Users.FindAsync(
                user => user.CompanyId == companyId.Value,
                cancellationToken);
            return byCompany.Select(Map).ToList();
        }

        var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
        return users.Select(Map).ToList();
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        var companyId = TenantGuard.ResolveUserCompanyId(_currentUser, request.Role, request.CompanyId);

        var existing = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("This email is already in use.");
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = _passwordService.HashPassword(request.Password),
            Role = request.Role,
            CompanyId = companyId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(user);
    }

    private static UserDto Map(User user) =>
        new(
            user.Id,
            user.CompanyId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role,
            user.IsActive,
            user.CreatedAt);
}
