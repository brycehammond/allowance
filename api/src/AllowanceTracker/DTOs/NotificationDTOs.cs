using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

// Request DTOs

/// <summary>
/// Register device for push notifications
/// </summary>
public record RegisterDeviceDto(
    string Token,              // FCM registration token
    DevicePlatform Platform,
    string? DeviceName,
    string? AppVersion
);

/// <summary>
/// Update notification preferences
/// </summary>
public record UpdateNotificationPreferencesDto(
    List<NotificationPreferenceItemDto> Preferences
);

public record NotificationPreferenceItemDto(
    NotificationType NotificationType,
    bool InAppEnabled,
    bool PushEnabled,
    bool EmailEnabled
);

/// <summary>
/// Update quiet hours
/// </summary>
public record UpdateQuietHoursDto(
    bool Enabled,
    TimeOnly? StartTime,
    TimeOnly? EndTime
);

/// <summary>
/// Mark notifications as read
/// </summary>
public record MarkNotificationsReadDto(
    List<Guid>? NotificationIds  // null = mark all as read
);

/// <summary>
/// Internal DTO for creating notifications
/// </summary>
public record CreateNotificationInternalDto(
    Guid UserId,
    NotificationType Type,
    string Title,
    string Body,
    string? Data = null,
    Guid? RelatedEntityId = null,
    string? RelatedEntityType = null,
    NotificationChannel Channel = NotificationChannel.All
);

// Response DTOs

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string TypeName,
    string Title,
    string Body,
    string? Data,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    string TimeAgo
);

public record NotificationListDto(
    List<NotificationDto> Notifications,
    int UnreadCount,
    int TotalCount,
    bool HasMore
);

public record NotificationPreferenceResponseDto(
    NotificationType NotificationType,
    string TypeName,
    string Category,
    bool InAppEnabled,
    bool PushEnabled,
    bool EmailEnabled
);

public record NotificationPreferencesDto(
    List<NotificationPreferenceResponseDto> Preferences,
    bool QuietHoursEnabled,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd
);

public record DeviceTokenDto(
    Guid Id,
    DevicePlatform Platform,
    string? DeviceName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastUsedAt
);

public record UnreadCountDto(
    int Count
);

public record MarkReadResultDto(
    int MarkedCount
);

public record DeleteResultDto(
    int DeletedCount
);
