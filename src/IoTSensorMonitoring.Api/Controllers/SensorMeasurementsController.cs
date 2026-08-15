using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sensor-measurements")]
public class SensorMeasurementsController : ControllerBase
{
    private readonly ISensorMeasurementService _measurementService;

    public SensorMeasurementsController(ISensorMeasurementService measurementService)
    {
        _measurementService = measurementService;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<SensorMeasurementDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _measurementService.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<SensorMeasurementDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _measurementService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<SensorMeasurementDto>> Create(
        [FromBody] CreateSensorMeasurementRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _measurementService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
