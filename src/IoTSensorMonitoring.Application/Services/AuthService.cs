using FluentValidation;
using IoTSensorMonitoring.Application.Abstractions;
using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.Common.Exceptions;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces;
using IoTSensorMonitoring.Application.Interfaces.Repositories;
using IoTSensorMonitoring.Application.Interfaces.Services;
using IoTSensorMonitoring.Domain.Entities;

namespace IoTSensorMonitoring.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        ITokenService tokenService,
        ICurrentUser currentUser,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _currentUser = currentUser;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_registerValidator, request, cancellationToken);

        var companyId = TenantGuard.ResolveUserCompanyId(_currentUser, request.Role, request.CompanyId);

        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("This email is already registered.");
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

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_loginValidator, request, cancellationToken);

        var user = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Email or password is incorrect.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("User account is inactive.");
        }

        return CreateAuthResponse(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = _tokenService.CreateToken(user, out var expiresAt);

        return new AuthResponse(
            token,
            expiresAt,
            user.Id,
            user.CompanyId,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role);
    }
}
