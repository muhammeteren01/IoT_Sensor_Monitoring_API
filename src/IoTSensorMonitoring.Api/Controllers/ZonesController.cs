using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/zones")]
public class ZonesController : ControllerBase
{
    private readonly IZoneService _zoneService;
    private readonly ISensorService _sensorService;

    public ZonesController(IZoneService zoneService, ISensorService sensorService)
    {
        _zoneService = zoneService;
        _sensorService = sensorService;
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<ZoneDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _zoneService.GetByIdAsync(id, cancellationToken));

    [HttpGet("{id:guid}/sensors")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<SensorDto>>> GetSensors(Guid id, CancellationToken cancellationToken)
        => Ok(await _sensorService.GetByZoneIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<ZoneDto>> Create([FromBody] CreateZoneRequest request, CancellationToken cancellationToken)
    {
        var created = await _zoneService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<ZoneDto>> Update(Guid id, [FromBody] UpdateZoneRequest request, CancellationToken cancellationToken)
        => Ok(await _zoneService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _zoneService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
