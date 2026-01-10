using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

public class DeviceToken : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public string Token { get; set; } = string.Empty;  // FCM registration token
    public DevicePlatform Platform { get; set; }
    public string? DeviceName { get; set; }
    public string? AppVersion { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
}
