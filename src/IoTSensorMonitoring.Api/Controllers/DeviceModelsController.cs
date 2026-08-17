using IoTSensorMonitoring.Application.Authorization;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/device-models")]
public class DeviceModelsController : ControllerBase
{
    private readonly IDeviceModelService _deviceModelService;

    public DeviceModelsController(IDeviceModelService deviceModelService)
    {
        _deviceModelService = deviceModelService;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<IReadOnlyList<DeviceModelDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _deviceModelService.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<ActionResult<DeviceModelDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _deviceModelService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<DeviceModelDto>> Create([FromBody] CreateDeviceModelRequest request, CancellationToken cancellationToken)
    {
        var created = await _deviceModelService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<ActionResult<DeviceModelDto>> Update(Guid id, [FromBody] UpdateDeviceModelRequest request, CancellationToken cancellationToken)
        => Ok(await _deviceModelService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Writers)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _deviceModelService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
