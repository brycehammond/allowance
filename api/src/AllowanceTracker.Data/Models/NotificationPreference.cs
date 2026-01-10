using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

public class NotificationPreference : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public NotificationType NotificationType { get; set; }

    // Channel preferences
    public bool InAppEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = false;

    // Schedule preferences
    public bool QuietHoursEnabled { get; set; } = false;
    public TimeOnly? QuietHoursStart { get; set; }
    public TimeOnly? QuietHoursEnd { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
