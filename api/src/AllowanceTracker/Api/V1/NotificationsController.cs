using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public NotificationsController(
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get user's notifications (paginated)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<NotificationListDto>> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] NotificationType? type = null)
    {
        if (pageSize > 50) pageSize = 50;
        if (page < 1) page = 1;

        var userId = _currentUserService.UserId;
        var notifications = await _notificationService.GetNotificationsAsync(userId, page, pageSize, unreadOnly, type);

        return Ok(notifications);
    }

    /// <summary>
    /// Get count of unread notifications
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount()
    {
        var userId = _currentUserService.UserId;
        var count = await _notificationService.GetUnreadCountAsync(userId);

        return Ok(new UnreadCountDto(count));
    }

    /// <summary>
    /// Get single notification detail
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<NotificationDto>> GetNotification(Guid id)
    {
        var userId = _currentUserService.UserId;
        var notification = await _notificationService.GetNotificationByIdAsync(id, userId);

        if (notification == null)
            return NotFound();

        return Ok(notification);
    }

    /// <summary>
    /// Mark single notification as read
    /// </summary>
    [HttpPatch("{id}/read")]
    public async Task<ActionResult<NotificationDto>> MarkAsRead(Guid id)
    {
        var userId = _currentUserService.UserId;
        var notification = await _notificationService.MarkAsReadAsync(id, userId);

        if (notification == null)
            return NotFound();

        return Ok(notification);
    }

    /// <summary>
    /// Mark multiple notifications as read
    /// </summary>
    [HttpPost("read")]
    public async Task<ActionResult<MarkReadResultDto>> MarkMultipleAsRead([FromBody] MarkNotificationsReadDto dto)
    {
        var userId = _currentUserService.UserId;
        var markedCount = await _notificationService.MarkMultipleAsReadAsync(userId, dto.NotificationIds);

        return Ok(new MarkReadResultDto(markedCount));
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNotification(Guid id)
    {
        var userId = _currentUserService.UserId;
        var deleted = await _notificationService.DeleteNotificationAsync(id, userId);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Delete all read notifications
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult<DeleteResultDto>> DeleteReadNotifications()
    {
        var userId = _currentUserService.UserId;
        var deletedCount = await _notificationService.DeleteReadNotificationsAsync(userId);

        return Ok(new DeleteResultDto(deletedCount));
    }

    /// <summary>
    /// Get user's notification preferences
    /// </summary>
    [HttpGet("preferences")]
    public async Task<ActionResult<NotificationPreferencesDto>> GetPreferences()
    {
        var userId = _currentUserService.UserId;
        var preferences = await _notificationService.GetPreferencesAsync(userId);

        return Ok(preferences);
    }

    /// <summary>
    /// Update notification preferences
    /// </summary>
    [HttpPut("preferences")]
    public async Task<ActionResult<NotificationPreferencesDto>> UpdatePreferences([FromBody] UpdateNotificationPreferencesDto dto)
    {
        var userId = _currentUserService.UserId;
        var preferences = await _notificationService.UpdatePreferencesAsync(userId, dto);

        return Ok(preferences);
    }

    /// <summary>
    /// Update quiet hours settings
    /// </summary>
    [HttpPut("preferences/quiet-hours")]
    public async Task<ActionResult<NotificationPreferencesDto>> UpdateQuietHours([FromBody] UpdateQuietHoursDto dto)
    {
        var userId = _currentUserService.UserId;
        var preferences = await _notificationService.UpdateQuietHoursAsync(userId, dto);

        return Ok(preferences);
    }
}
