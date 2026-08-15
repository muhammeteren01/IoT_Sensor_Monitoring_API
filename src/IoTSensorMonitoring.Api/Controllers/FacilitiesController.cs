using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/facilities")]
public class FacilitiesController : ControllerBase
{
    private readonly IFacilityService _facilityService;
    private readonly IZoneService _zoneService;

    public FacilitiesController(IFacilityService facilityService, IZoneService zoneService)
    {
        _facilityService = facilityService;
        _zoneService = zoneService;
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<FacilityDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _facilityService.GetByIdAsync(id, cancellationToken));

    [HttpGet("{id:guid}/zones")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<ZoneDto>>> GetZones(Guid id, CancellationToken cancellationToken)
        => Ok(await _zoneService.GetByFacilityIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<FacilityDto>> Create([FromBody] CreateFacilityRequest request, CancellationToken cancellationToken)
    {
        var created = await _facilityService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<FacilityDto>> Update(Guid id, [FromBody] UpdateFacilityRequest request, CancellationToken cancellationToken)
        => Ok(await _facilityService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _facilityService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
