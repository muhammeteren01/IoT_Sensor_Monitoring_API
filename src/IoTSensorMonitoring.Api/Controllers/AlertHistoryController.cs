using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/alert-history")]
public class AlertHistoryController : ControllerBase
{
    private readonly IAlertHistoryService _alertHistoryService;

    public AlertHistoryController(IAlertHistoryService alertHistoryService)
    {
        _alertHistoryService = alertHistoryService;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<AlertHistoryDto>>> List(
        [FromQuery] Guid? sensorId,
        [FromQuery] bool? isResolved,
        CancellationToken cancellationToken)
        => Ok(await _alertHistoryService.ListAsync(sensorId, isResolved, cancellationToken));

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<AlertHistoryDto>> Resolve(Guid id, CancellationToken cancellationToken)
        => Ok(await _alertHistoryService.ResolveAsync(id, cancellationToken));
}
