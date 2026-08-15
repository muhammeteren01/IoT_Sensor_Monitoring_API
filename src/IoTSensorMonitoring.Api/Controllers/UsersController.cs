using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.CompanyAdmins)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
        => Ok(await _userService.GetAllAsync(companyId, cancellationToken));
}
