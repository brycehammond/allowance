using AllowanceTracker.DTOs;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/devices")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly IDeviceTokenService _deviceTokenService;
    private readonly ICurrentUserService _currentUserService;

    public DevicesController(
        IDeviceTokenService deviceTokenService,
        ICurrentUserService currentUserService)
    {
        _deviceTokenService = deviceTokenService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Register device for push notifications (FCM token)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DeviceTokenDto>> RegisterDevice([FromBody] RegisterDeviceDto dto)
    {
        var userId = _currentUserService.UserId;
        var device = await _deviceTokenService.RegisterDeviceAsync(userId, dto);

        return Ok(device);
    }

    /// <summary>
    /// Get user's registered devices
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<DeviceTokenDto>>> GetDevices()
    {
        var userId = _currentUserService.UserId;
        var devices = await _deviceTokenService.GetUserDevicesAsync(userId);

        return Ok(devices);
    }

    /// <summary>
    /// Unregister device by ID
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> UnregisterDevice(Guid id)
    {
        var userId = _currentUserService.UserId;
        await _deviceTokenService.DeactivateDeviceAsync(id, userId);

        return NoContent();
    }

    /// <summary>
    /// Unregister current device (logout)
    /// </summary>
    [HttpDelete("current")]
    public async Task<ActionResult> UnregisterCurrentDevice([FromHeader(Name = "X-Device-Token")] string? deviceToken)
    {
        if (string.IsNullOrEmpty(deviceToken))
        {
            return BadRequest(new { error = "X-Device-Token header is required" });
        }

        var userId = _currentUserService.UserId;
        await _deviceTokenService.DeactivateByTokenAsync(deviceToken, userId);

        return NoContent();
    }
}
