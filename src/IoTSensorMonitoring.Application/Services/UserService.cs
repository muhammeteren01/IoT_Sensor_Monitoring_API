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

    public UserService(IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
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
