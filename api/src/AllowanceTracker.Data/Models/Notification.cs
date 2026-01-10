using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

public class Notification : IHasCreatedAt
{
    public Guid Id { get; set; }

    // Recipient
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    // Content
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Data { get; set; } // JSON payload for deep linking

    // Status
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    // Delivery
    public NotificationChannel Channel { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // Related entities (optional)
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; } // "Transaction", "Task", "Goal", etc.
}
