using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/alert-rules")]
public class AlertRulesController : ControllerBase
{
    private readonly IAlertRuleService _alertRuleService;

    public AlertRulesController(IAlertRuleService alertRuleService)
    {
        _alertRuleService = alertRuleService;
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<AlertRuleDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _alertRuleService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<AlertRuleDto>> Create([FromBody] CreateAlertRuleRequest request, CancellationToken cancellationToken)
    {
        var created = await _alertRuleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<AlertRuleDto>> Update(Guid id, [FromBody] UpdateAlertRuleRequest request, CancellationToken cancellationToken)
        => Ok(await _alertRuleService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _alertRuleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
