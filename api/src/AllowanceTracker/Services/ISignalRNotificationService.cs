using AllowanceTracker.DTOs;

namespace AllowanceTracker.Services;

public interface ISignalRNotificationService
{
    Task SendToUserAsync(Guid userId, NotificationDto notification);
    Task SendToFamilyAsync(Guid familyId, NotificationDto notification);
    Task SendUnreadCountAsync(Guid userId, int count);
    Task SendBalanceUpdatedAsync(Guid familyId, Guid childId, decimal newBalance);
}
