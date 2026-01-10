using AllowanceTracker.DTOs;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

public interface INotificationService
{
    // Notification CRUD
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationInternalDto dto);
    Task<NotificationListDto> GetNotificationsAsync(Guid userId, int page, int pageSize, bool unreadOnly, NotificationType? type);
    Task<NotificationDto?> GetNotificationByIdAsync(Guid notificationId, Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<NotificationDto?> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<int> MarkMultipleAsReadAsync(Guid userId, List<Guid>? notificationIds);
    Task<bool> DeleteNotificationAsync(Guid notificationId, Guid userId);
    Task<int> DeleteReadNotificationsAsync(Guid userId);

    // Sending
    Task SendNotificationAsync(Guid userId, NotificationType type, string title, string body, object? data = null, Guid? relatedEntityId = null, string? relatedEntityType = null);
    Task SendBulkNotificationAsync(List<Guid> userIds, NotificationType type, string title, string body);
    Task SendFamilyNotificationAsync(Guid familyId, NotificationType type, string title, string body, Guid? excludeUserId = null);

    // Preferences
    Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId);
    Task<NotificationPreferencesDto> UpdatePreferencesAsync(Guid userId, UpdateNotificationPreferencesDto dto);
    Task<NotificationPreferencesDto> UpdateQuietHoursAsync(Guid userId, UpdateQuietHoursDto dto);
    Task<bool> ShouldSendAsync(Guid userId, NotificationType type, NotificationChannel channel);
}
