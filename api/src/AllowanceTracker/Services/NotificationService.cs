using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class NotificationService : INotificationService
{
    private readonly AllowanceContext _context;
    private readonly IFirebasePushService _firebasePushService;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AllowanceContext context,
        IFirebasePushService firebasePushService,
        ISignalRNotificationService signalRNotificationService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _firebasePushService = firebasePushService;
        _signalRNotificationService = signalRNotificationService;
        _logger = logger;
    }

    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationInternalDto dto)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            Type = dto.Type,
            Title = dto.Title,
            Body = dto.Body,
            Data = dto.Data,
            RelatedEntityId = dto.RelatedEntityId,
            RelatedEntityType = dto.RelatedEntityType,
            Channel = dto.Channel,
            Status = NotificationStatus.Pending,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return MapToDto(notification);
    }

    public async Task<NotificationListDto> GetNotificationsAsync(Guid userId, int page, int pageSize, bool unreadOnly, NotificationType? type)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        if (type.HasValue)
        {
            query = query.Where(n => n.Type == type.Value);
        }

        var totalCount = await query.CountAsync();
        var unreadCount = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new NotificationListDto(
            Notifications: notifications.Select(MapToDto).ToList(),
            UnreadCount: unreadCount,
            TotalCount: totalCount,
            HasMore: page * pageSize < totalCount
        );
    }

    public async Task<NotificationDto?> GetNotificationByIdAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        return notification == null ? null : MapToDto(notification);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    public async Task<NotificationDto?> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
        {
            return null;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(notification);
    }

    public async Task<int> MarkMultipleAsReadAsync(Guid userId, List<Guid>? notificationIds)
    {
        IQueryable<Notification> query = _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead);

        if (notificationIds != null && notificationIds.Any())
        {
            query = query.Where(n => notificationIds.Contains(n.Id));
        }

        var notifications = await query.ToListAsync();
        var now = DateTime.UtcNow;

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _context.SaveChangesAsync();

        return notifications.Count;
    }

    public async Task<bool> DeleteNotificationAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
        {
            return false;
        }

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> DeleteReadNotificationsAsync(Guid userId)
    {
        var readNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && n.IsRead)
            .ToListAsync();

        _context.Notifications.RemoveRange(readNotifications);
        await _context.SaveChangesAsync();

        return readNotifications.Count;
    }

    public async Task SendNotificationAsync(Guid userId, NotificationType type, string title, string body, object? data = null, Guid? relatedEntityId = null, string? relatedEntityType = null)
    {
        var dataJson = data != null ? System.Text.Json.JsonSerializer.Serialize(data) : null;

        // Create the notification DTO
        var dto = new CreateNotificationInternalDto(
            UserId: userId,
            Type: type,
            Title: title,
            Body: body,
            Data: dataJson,
            RelatedEntityId: relatedEntityId,
            RelatedEntityType: relatedEntityType,
            Channel: NotificationChannel.All
        );

        // Check if we should send in-app notification
        var shouldSendInApp = await ShouldSendAsync(userId, type, NotificationChannel.InApp);
        NotificationDto? notificationDto = null;

        if (shouldSendInApp)
        {
            notificationDto = await CreateNotificationAsync(dto);
            _logger.LogDebug("Created in-app notification {NotificationId} for user {UserId}", notificationDto.Id, userId);
        }

        // Send real-time notification via SignalR (if in-app notification was created)
        if (notificationDto != null)
        {
            try
            {
                await _signalRNotificationService.SendToUserAsync(userId, notificationDto);

                // Update unread count
                var unreadCount = await GetUnreadCountAsync(userId);
                await _signalRNotificationService.SendUnreadCountAsync(userId, unreadCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send SignalR notification to user {UserId}", userId);
            }
        }

        // Check if we should send push notification
        var shouldSendPush = await ShouldSendAsync(userId, type, NotificationChannel.Push);
        if (shouldSendPush)
        {
            try
            {
                var pushData = new Dictionary<string, string>
                {
                    ["notificationType"] = type.ToString(),
                    ["notificationId"] = notificationDto?.Id.ToString() ?? ""
                };

                if (relatedEntityId.HasValue)
                {
                    pushData["relatedEntityId"] = relatedEntityId.Value.ToString();
                }
                if (!string.IsNullOrEmpty(relatedEntityType))
                {
                    pushData["relatedEntityType"] = relatedEntityType;
                }

                var pushSent = await _firebasePushService.SendPushAsync(userId, title, body, pushData);
                if (pushSent)
                {
                    _logger.LogDebug("Sent push notification to user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send push notification to user {UserId}", userId);
            }
        }
    }

    public async Task SendBulkNotificationAsync(List<Guid> userIds, NotificationType type, string title, string body)
    {
        foreach (var userId in userIds)
        {
            await SendNotificationAsync(userId, type, title, body);
        }
    }

    public async Task SendFamilyNotificationAsync(Guid familyId, NotificationType type, string title, string body, Guid? excludeUserId = null)
    {
        var familyMemberIds = await _context.Users
            .Where(u => u.FamilyId == familyId)
            .Select(u => u.Id)
            .ToListAsync();

        if (excludeUserId.HasValue)
        {
            familyMemberIds = familyMemberIds.Where(id => id != excludeUserId.Value).ToList();
        }

        await SendBulkNotificationAsync(familyMemberIds, type, title, body);
    }

    public async Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId)
    {
        var preferences = await _context.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync();

        // Get quiet hours from any preference (they should all have the same values)
        var anyPref = preferences.FirstOrDefault();

        var allTypes = Enum.GetValues<NotificationType>();
        var preferenceResponses = allTypes.Select(type =>
        {
            var pref = preferences.FirstOrDefault(p => p.NotificationType == type);
            return new NotificationPreferenceResponseDto(
                NotificationType: type,
                TypeName: GetTypeName(type),
                Category: GetCategory(type),
                InAppEnabled: pref?.InAppEnabled ?? true,
                PushEnabled: pref?.PushEnabled ?? true,
                EmailEnabled: pref?.EmailEnabled ?? false
            );
        }).ToList();

        return new NotificationPreferencesDto(
            Preferences: preferenceResponses,
            QuietHoursEnabled: anyPref?.QuietHoursEnabled ?? false,
            QuietHoursStart: anyPref?.QuietHoursStart,
            QuietHoursEnd: anyPref?.QuietHoursEnd
        );
    }

    public async Task<NotificationPreferencesDto> UpdatePreferencesAsync(Guid userId, UpdateNotificationPreferencesDto dto)
    {
        var existingPrefs = await _context.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync();

        foreach (var prefItem in dto.Preferences)
        {
            var existing = existingPrefs.FirstOrDefault(p => p.NotificationType == prefItem.NotificationType);

            if (existing != null)
            {
                existing.InAppEnabled = prefItem.InAppEnabled;
                existing.PushEnabled = prefItem.PushEnabled;
                existing.EmailEnabled = prefItem.EmailEnabled;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newPref = new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NotificationType = prefItem.NotificationType,
                    InAppEnabled = prefItem.InAppEnabled,
                    PushEnabled = prefItem.PushEnabled,
                    EmailEnabled = prefItem.EmailEnabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.NotificationPreferences.Add(newPref);
            }
        }

        await _context.SaveChangesAsync();

        return await GetPreferencesAsync(userId);
    }

    public async Task<NotificationPreferencesDto> UpdateQuietHoursAsync(Guid userId, UpdateQuietHoursDto dto)
    {
        var preferences = await _context.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync();

        if (preferences.Any())
        {
            foreach (var pref in preferences)
            {
                pref.QuietHoursEnabled = dto.Enabled;
                pref.QuietHoursStart = dto.StartTime;
                pref.QuietHoursEnd = dto.EndTime;
                pref.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            // Create a default preference to store quiet hours
            var defaultPref = new NotificationPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                NotificationType = NotificationType.SystemAnnouncement,
                InAppEnabled = true,
                PushEnabled = true,
                EmailEnabled = false,
                QuietHoursEnabled = dto.Enabled,
                QuietHoursStart = dto.StartTime,
                QuietHoursEnd = dto.EndTime,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.NotificationPreferences.Add(defaultPref);
        }

        await _context.SaveChangesAsync();

        return await GetPreferencesAsync(userId);
    }

    public async Task<bool> ShouldSendAsync(Guid userId, NotificationType type, NotificationChannel channel)
    {
        var preference = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == type);

        if (preference == null)
        {
            // Default is to send
            return true;
        }

        // Check quiet hours
        if (preference.QuietHoursEnabled && preference.QuietHoursStart.HasValue && preference.QuietHoursEnd.HasValue)
        {
            var now = TimeOnly.FromDateTime(DateTime.UtcNow);
            var start = preference.QuietHoursStart.Value;
            var end = preference.QuietHoursEnd.Value;

            // Handle overnight quiet hours (e.g., 22:00 to 07:00)
            bool inQuietHours;
            if (start < end)
            {
                inQuietHours = now >= start && now <= end;
            }
            else
            {
                inQuietHours = now >= start || now <= end;
            }

            if (inQuietHours && channel == NotificationChannel.Push)
            {
                return false;
            }
        }

        return channel switch
        {
            NotificationChannel.InApp => preference.InAppEnabled,
            NotificationChannel.Push => preference.PushEnabled,
            NotificationChannel.Email => preference.EmailEnabled,
            NotificationChannel.All => preference.InAppEnabled || preference.PushEnabled || preference.EmailEnabled,
            _ => true
        };
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto(
            Id: notification.Id,
            Type: notification.Type,
            TypeName: GetTypeName(notification.Type),
            Title: notification.Title,
            Body: notification.Body,
            Data: notification.Data,
            IsRead: notification.IsRead,
            ReadAt: notification.ReadAt,
            CreatedAt: notification.CreatedAt,
            RelatedEntityType: notification.RelatedEntityType,
            RelatedEntityId: notification.RelatedEntityId,
            TimeAgo: GetTimeAgo(notification.CreatedAt)
        );
    }

    private static string GetTypeName(NotificationType type)
    {
        return type switch
        {
            NotificationType.BalanceAlert => "Balance Alert",
            NotificationType.LowBalanceWarning => "Low Balance Warning",
            NotificationType.TransactionCreated => "Transaction Created",
            NotificationType.AllowanceDeposit => "Allowance Deposit",
            NotificationType.AllowancePaused => "Allowance Paused",
            NotificationType.AllowanceResumed => "Allowance Resumed",
            NotificationType.GoalProgress => "Goal Progress",
            NotificationType.GoalMilestone => "Goal Milestone",
            NotificationType.GoalCompleted => "Goal Completed",
            NotificationType.ParentMatchAdded => "Parent Match Added",
            NotificationType.TaskAssigned => "Task Assigned",
            NotificationType.TaskReminder => "Task Reminder",
            NotificationType.TaskCompleted => "Task Completed",
            NotificationType.ApprovalRequired => "Approval Required",
            NotificationType.TaskApproved => "Task Approved",
            NotificationType.TaskRejected => "Task Rejected",
            NotificationType.BudgetWarning => "Budget Warning",
            NotificationType.BudgetExceeded => "Budget Exceeded",
            NotificationType.AchievementUnlocked => "Achievement Unlocked",
            NotificationType.StreakUpdate => "Streak Update",
            NotificationType.FamilyInvite => "Family Invite",
            NotificationType.ChildAdded => "Child Added",
            NotificationType.GiftReceived => "Gift Received",
            NotificationType.WeeklySummary => "Weekly Summary",
            NotificationType.MonthlySummary => "Monthly Summary",
            NotificationType.SystemAnnouncement => "System Announcement",
            _ => type.ToString()
        };
    }

    private static string GetCategory(NotificationType type)
    {
        return type switch
        {
            NotificationType.BalanceAlert or NotificationType.LowBalanceWarning or NotificationType.TransactionCreated => "Balance & Transactions",
            NotificationType.AllowanceDeposit or NotificationType.AllowancePaused or NotificationType.AllowanceResumed => "Allowance",
            NotificationType.GoalProgress or NotificationType.GoalMilestone or NotificationType.GoalCompleted or NotificationType.ParentMatchAdded => "Goals & Savings",
            NotificationType.TaskAssigned or NotificationType.TaskReminder or NotificationType.TaskCompleted or NotificationType.ApprovalRequired or NotificationType.TaskApproved or NotificationType.TaskRejected => "Tasks",
            NotificationType.BudgetWarning or NotificationType.BudgetExceeded => "Budget",
            NotificationType.AchievementUnlocked or NotificationType.StreakUpdate => "Achievements",
            NotificationType.FamilyInvite or NotificationType.ChildAdded or NotificationType.GiftReceived => "Family",
            NotificationType.WeeklySummary or NotificationType.MonthlySummary or NotificationType.SystemAnnouncement => "System",
            _ => "Other"
        };
    }

    private static string GetTimeAgo(DateTime createdAt)
    {
        var timeSpan = DateTime.UtcNow - createdAt;

        if (timeSpan.TotalSeconds < 60)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)}w ago";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)}mo ago";

        return $"{(int)(timeSpan.TotalDays / 365)}y ago";
    }
}
