using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/companies")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly IFacilityService _facilityService;
    private readonly IUserService _userService;

    public CompaniesController(
        ICompanyService companyService,
        IFacilityService facilityService,
        IUserService userService)
    {
        _companyService = companyService;
        _facilityService = facilityService;
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<CompanyDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _companyService.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<CompanyDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _companyService.GetByIdAsync(id, cancellationToken));

    [HttpGet("{id:guid}/facilities")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<FacilityDto>>> GetFacilities(Guid id, CancellationToken cancellationToken)
        => Ok(await _facilityService.GetByCompanyIdAsync(id, cancellationToken));

    [HttpGet("{id:guid}/users")]
    [Authorize(Roles = AppRoles.CompanyAdmins)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsers(Guid id, CancellationToken cancellationToken)
        => Ok(await _userService.GetAllAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = AppRoles.SuperAdminOnly)]
    public async Task<ActionResult<CompanyDto>> Create([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var created = await _companyService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.SuperAdminOnly)]
    public async Task<ActionResult<CompanyDto>> Update(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken cancellationToken)
        => Ok(await _companyService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.SuperAdminOnly)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _companyService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
