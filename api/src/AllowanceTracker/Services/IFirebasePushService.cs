namespace AllowanceTracker.Services;

public interface IFirebasePushService
{
    /// <summary>
    /// Send push notification to all active devices for a user
    /// </summary>
    Task<bool> SendPushAsync(Guid userId, string title, string body, object? data = null);

    /// <summary>
    /// Send push notification to a specific device by FCM token
    /// </summary>
    Task<bool> SendToDeviceAsync(string fcmToken, string title, string body, object? data = null);

    /// <summary>
    /// Send push notification to multiple devices
    /// </summary>
    Task<int> SendToMultipleAsync(List<string> fcmTokens, string title, string body, object? data = null);

    /// <summary>
    /// Send push notification to a topic (e.g., all users in a family)
    /// </summary>
    Task<bool> SendToTopicAsync(string topic, string title, string body, object? data = null);

    /// <summary>
    /// Check if Firebase is configured and available
    /// </summary>
    bool IsAvailable { get; }
}
