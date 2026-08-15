using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/maintenance-logs")]
public class MaintenanceLogsController : ControllerBase
{
    private readonly IMaintenanceLogService _maintenanceLogService;

    public MaintenanceLogsController(IMaintenanceLogService maintenanceLogService)
    {
        _maintenanceLogService = maintenanceLogService;
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<MaintenanceLogDto>> Create(
        [FromBody] CreateMaintenanceLogRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _maintenanceLogService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBySensorId), new { sensorId = created.SensorId }, created);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<MaintenanceLogDto>>> GetBySensorId(
        [FromQuery] Guid sensorId,
        CancellationToken cancellationToken)
        => Ok(await _maintenanceLogService.GetBySensorIdAsync(sensorId, cancellationToken));
}
