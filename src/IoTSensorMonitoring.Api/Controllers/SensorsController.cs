using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sensors")]
public class SensorsController : ControllerBase
{
    private readonly ISensorService _sensorService;
    private readonly ISensorMeasurementService _measurementService;
    private readonly IAlertRuleService _alertRuleService;
    private readonly IMaintenanceLogService _maintenanceLogService;

    public SensorsController(
        ISensorService sensorService,
        ISensorMeasurementService measurementService,
        IAlertRuleService alertRuleService,
        IMaintenanceLogService maintenanceLogService)
    {
        _sensorService = sensorService;
        _measurementService = measurementService;
        _alertRuleService = alertRuleService;
        _maintenanceLogService = maintenanceLogService;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.SensorReaders)]
    public async Task<ActionResult<IReadOnlyList<SensorDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _sensorService.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.SensorReaders)]
    public async Task<ActionResult<SensorDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _sensorService.GetByIdAsync(id, cancellationToken));

    [HttpGet("{id:guid}/latest")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<SensorMeasurementDto>> GetLatest(Guid id, CancellationToken cancellationToken)
        => Ok(await _measurementService.GetLatestBySensorIdAsync(id, cancellationToken));

    [HttpGet("{id:guid}/measurements")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<SensorMeasurementDto>>> GetMeasurements(
        Guid id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
        => Ok(await _measurementService.GetBySensorIdAsync(id, from, to, cancellationToken));

    [HttpGet("{id:guid}/statistics")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<SensorStatisticsDto>> GetStatistics(
        Guid id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
        => Ok(await _measurementService.GetStatisticsAsync(id, from, to, cancellationToken));

    [HttpGet("{id:guid}/alert-rules")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<AlertRuleDto>>> GetAlertRules(Guid id, CancellationToken cancellationToken)
        => Ok(await _alertRuleService.GetBySensorIdAsync(id, cancellationToken));

    [HttpGet("{id:guid}/maintenance-logs")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<MaintenanceLogDto>>> GetMaintenanceLogs(Guid id, CancellationToken cancellationToken)
        => Ok(await _maintenanceLogService.GetBySensorIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<SensorDto>> Create([FromBody] CreateSensorRequest request, CancellationToken cancellationToken)
    {
        var created = await _sensorService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<SensorDto>> Update(Guid id, [FromBody] UpdateSensorRequest request, CancellationToken cancellationToken)
        => Ok(await _sensorService.UpdateAsync(id, request, cancellationToken));

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<SensorDto>> SetStatus(Guid id, [FromBody] SetSensorStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _sensorService.SetStatusAsync(id, request.Status, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sensorService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
