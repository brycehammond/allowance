# Push Notifications System Specification

## Overview

The Push Notifications system provides real-time alerts to users across web (SignalR) and mobile (Firebase Cloud Messaging) platforms. It keeps parents and children informed about important events: balance changes, allowance deposits, goal progress, task reminders, approval requests, and budget warnings.

Key features:
- Multi-channel delivery (in-app, push via FCM, email)
- User-configurable notification preferences
- Device token management for mobile push
- Notification history/center
- Real-time web notifications via SignalR
- Firebase Cloud Messaging for iOS and Android

---

## Database Schema

### Notification Model

```csharp
public class Notification
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

public enum NotificationType
{
    // Balance & Transactions
    BalanceAlert = 1,
    LowBalanceWarning = 2,
    TransactionCreated = 3,

    // Allowance
    AllowanceDeposit = 10,
    AllowancePaused = 11,
    AllowanceResumed = 12,

    // Goals & Savings
    GoalProgress = 20,
    GoalMilestone = 21,
    GoalCompleted = 22,
    ParentMatchAdded = 23,

    // Tasks
    TaskAssigned = 30,
    TaskReminder = 31,
    TaskCompleted = 32,
    ApprovalRequired = 33,
    TaskApproved = 34,
    TaskRejected = 35,

    // Budget
    BudgetWarning = 40,
    BudgetExceeded = 41,

    // Achievements
    AchievementUnlocked = 50,
    StreakUpdate = 51,

    // Family
    FamilyInvite = 60,
    ChildAdded = 61,
    GiftReceived = 62,

    // System
    WeeklySummary = 70,
    MonthlySummary = 71,
    SystemAnnouncement = 99
}

public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Delivered = 3,
    Failed = 4,
    Expired = 5
}

public enum NotificationChannel
{
    InApp = 1,
    Push = 2,
    Email = 3,
    All = 99
}
```

### NotificationPreference Model

```csharp
public class NotificationPreference
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
```

### DeviceToken Model

```csharp
public class DeviceToken
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

public enum DevicePlatform
{
    iOS = 1,
    Android = 2,
    Web = 3
}
```

---

## DTOs

### Request DTOs

```csharp
// Register device for push notifications
public record RegisterDeviceDto(
    string Token,              // FCM registration token
    DevicePlatform Platform,
    string? DeviceName,
    string? AppVersion
);

// Update notification preferences
public record UpdateNotificationPreferencesDto(
    List<NotificationPreferenceItemDto> Preferences
);

public record NotificationPreferenceItemDto(
    NotificationType NotificationType,
    bool InAppEnabled,
    bool PushEnabled,
    bool EmailEnabled
);

// Update quiet hours
public record UpdateQuietHoursDto(
    bool Enabled,
    TimeOnly? StartTime,
    TimeOnly? EndTime
);

// Mark notifications as read
public record MarkNotificationsReadDto(
    List<Guid>? NotificationIds  // null = mark all as read
);

// Send test notification (admin/debug)
public record SendTestNotificationDto(
    Guid UserId,
    NotificationType Type,
    string Title,
    string Body
);
```

### Response DTOs

```csharp
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

public record NotificationPreferenceDto(
    NotificationType NotificationType,
    string TypeName,
    string Category,
    bool InAppEnabled,
    bool PushEnabled,
    bool EmailEnabled
);

public record NotificationPreferencesDto(
    List<NotificationPreferenceDto> Preferences,
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
```

---

## API Endpoints

### Notifications

#### GET /api/v1/notifications
Get user's notifications (paginated)

**Authorization**: Authenticated user
**Response**: `NotificationListDto`

**Query Parameters**:
- `page` (default: 1) - Page number
- `pageSize` (default: 20, max: 50) - Items per page
- `unreadOnly` (default: false) - Only return unread
- `type` (optional) - Filter by notification type

---

#### GET /api/v1/notifications/unread-count
Get count of unread notifications

**Authorization**: Authenticated user
**Response**: `UnreadCountDto`

---

#### GET /api/v1/notifications/{id}
Get single notification detail

**Authorization**: Notification owner
**Response**: `NotificationDto`

---

#### PATCH /api/v1/notifications/{id}/read
Mark single notification as read

**Authorization**: Notification owner
**Response**: `NotificationDto`

---

#### POST /api/v1/notifications/read
Mark multiple notifications as read

**Authorization**: Authenticated user
**Request Body**: `MarkNotificationsReadDto`
**Response**: `{ "markedCount": 5 }`

---

#### DELETE /api/v1/notifications/{id}
Delete a notification

**Authorization**: Notification owner
**Response**: 204 No Content

---

#### DELETE /api/v1/notifications
Delete all read notifications

**Authorization**: Authenticated user
**Response**: `{ "deletedCount": 10 }`

---

### Notification Preferences

#### GET /api/v1/notifications/preferences
Get user's notification preferences

**Authorization**: Authenticated user
**Response**: `NotificationPreferencesDto`

---

#### PUT /api/v1/notifications/preferences
Update notification preferences

**Authorization**: Authenticated user
**Request Body**: `UpdateNotificationPreferencesDto`
**Response**: `NotificationPreferencesDto`

---

#### PUT /api/v1/notifications/preferences/quiet-hours
Update quiet hours settings

**Authorization**: Authenticated user
**Request Body**: `UpdateQuietHoursDto`
**Response**: `NotificationPreferencesDto`

---

### Device Management

#### POST /api/v1/devices
Register device for push notifications (FCM token)

**Authorization**: Authenticated user
**Request Body**: `RegisterDeviceDto`
**Response**: `DeviceTokenDto`

**Business Rules**:
- Updates existing token if same device
- Deactivates old tokens for same platform
- Maximum 5 active devices per user

---

#### GET /api/v1/devices
Get user's registered devices

**Authorization**: Authenticated user
**Response**: `List<DeviceTokenDto>`

---

#### DELETE /api/v1/devices/{id}
Unregister device

**Authorization**: Device owner
**Response**: 204 No Content

---

#### DELETE /api/v1/devices/current
Unregister current device (logout)

**Authorization**: Authenticated user
**Request Header**: `X-Device-Token: <token>`
**Response**: 204 No Content

---

## Service Layer

### INotificationService

```csharp
public interface INotificationService
{
    // Notification CRUD
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationInternalDto dto);
    Task<NotificationListDto> GetNotificationsAsync(Guid userId, int page, int pageSize, bool unreadOnly, NotificationType? type);
    Task<NotificationDto?> GetNotificationByIdAsync(Guid notificationId, Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<NotificationDto> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<int> MarkMultipleAsReadAsync(Guid userId, List<Guid>? notificationIds);
    Task DeleteNotificationAsync(Guid notificationId, Guid userId);
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
```

### IDeviceTokenService

```csharp
public interface IDeviceTokenService
{
    Task<DeviceTokenDto> RegisterDeviceAsync(Guid userId, RegisterDeviceDto dto);
    Task<List<DeviceTokenDto>> GetUserDevicesAsync(Guid userId);
    Task DeactivateDeviceAsync(Guid deviceId, Guid userId);
    Task DeactivateByTokenAsync(string token, Guid userId);
    Task<List<DeviceToken>> GetActiveDevicesAsync(Guid userId);
    Task UpdateLastUsedAsync(string token);
    Task CleanupStaleTokensAsync(int daysInactive = 90);
}
```

### IFirebasePushService

```csharp
public interface IFirebasePushService
{
    Task<bool> SendPushAsync(Guid userId, string title, string body, object? data = null);
    Task<bool> SendToDeviceAsync(string fcmToken, string title, string body, object? data = null);
    Task<int> SendToMultipleAsync(List<string> fcmTokens, string title, string body, object? data = null);
    Task<bool> SendToTopicAsync(string topic, string title, string body, object? data = null);
}
```

### ISignalRNotificationService

```csharp
public interface ISignalRNotificationService
{
    Task SendToUserAsync(Guid userId, NotificationDto notification);
    Task SendToFamilyAsync(Guid familyId, NotificationDto notification);
    Task SendUnreadCountAsync(Guid userId, int count);
}
```

---

## Firebase Cloud Messaging Integration

### FirebasePushService Implementation

```csharp
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

public class FirebasePushService : IFirebasePushService
{
    private readonly IDeviceTokenService _deviceTokenService;
    private readonly ILogger<FirebasePushService> _logger;
    private readonly FirebaseMessaging _messaging;

    public FirebasePushService(
        IDeviceTokenService deviceTokenService,
        ILogger<FirebasePushService> logger,
        IConfiguration configuration)
    {
        _deviceTokenService = deviceTokenService;
        _logger = logger;

        // Initialize Firebase Admin SDK (do once at startup)
        if (FirebaseApp.DefaultInstance == null)
        {
            var credentialPath = configuration["Firebase:CredentialPath"];
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(credentialPath)
            });
        }

        _messaging = FirebaseMessaging.DefaultInstance;
    }

    public async Task<bool> SendPushAsync(Guid userId, string title, string body, object? data = null)
    {
        var devices = await _deviceTokenService.GetActiveDevicesAsync(userId);
        if (!devices.Any())
        {
            _logger.LogDebug("No active devices for user {UserId}", userId);
            return false;
        }

        var tokens = devices.Select(d => d.Token).ToList();
        var successCount = await SendToMultipleAsync(tokens, title, body, data);

        return successCount > 0;
    }

    public async Task<bool> SendToDeviceAsync(string fcmToken, string title, string body, object? data = null)
    {
        try
        {
            var message = new Message
            {
                Token = fcmToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = ConvertToStringDictionary(data),
                // iOS specific
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Badge = 1,
                        Sound = "default"
                    }
                },
                // Android specific
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Sound = "default",
                        ClickAction = "OPEN_NOTIFICATION"
                    }
                }
            };

            var response = await _messaging.SendAsync(message);
            _logger.LogInformation("FCM message sent: {MessageId}", response);
            return true;
        }
        catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            _logger.LogWarning("FCM token is unregistered: {Token}", fcmToken);
            // Token is invalid, should be removed
            await _deviceTokenService.DeactivateByTokenAsync(fcmToken, Guid.Empty);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM message to {Token}", fcmToken);
            return false;
        }
    }

    public async Task<int> SendToMultipleAsync(List<string> fcmTokens, string title, string body, object? data = null)
    {
        if (!fcmTokens.Any()) return 0;

        try
        {
            var message = new MulticastMessage
            {
                Tokens = fcmTokens,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = ConvertToStringDictionary(data),
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Badge = 1,
                        Sound = "default"
                    }
                },
                Android = new AndroidConfig
                {
                    Priority = Priority.High
                }
            };

            var response = await _messaging.SendEachForMulticastAsync(message);

            // Handle failed tokens
            if (response.FailureCount > 0)
            {
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    if (!response.Responses[i].IsSuccess)
                    {
                        var error = response.Responses[i].Exception;
                        if (error?.MessagingErrorCode == MessagingErrorCode.Unregistered)
                        {
                            await _deviceTokenService.DeactivateByTokenAsync(fcmTokens[i], Guid.Empty);
                        }
                        _logger.LogWarning("FCM send failed for token {Token}: {Error}",
                            fcmTokens[i], error?.Message);
                    }
                }
            }

            _logger.LogInformation("FCM multicast: {Success} succeeded, {Failed} failed",
                response.SuccessCount, response.FailureCount);

            return response.SuccessCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM multicast message");
            return 0;
        }
    }

    public async Task<bool> SendToTopicAsync(string topic, string title, string body, object? data = null)
    {
        try
        {
            var message = new Message
            {
                Topic = topic,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = ConvertToStringDictionary(data)
            };

            var response = await _messaging.SendAsync(message);
            _logger.LogInformation("FCM topic message sent to {Topic}: {MessageId}", topic, response);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM topic message to {Topic}", topic);
            return false;
        }
    }

    private Dictionary<string, string>? ConvertToStringDictionary(object? data)
    {
        if (data == null) return null;

        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        return dict?.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? "");
    }
}
```

### Program.cs Configuration

```csharp
// Add Firebase services
builder.Services.AddSingleton<IFirebasePushService, FirebasePushService>();

// Initialize Firebase at startup
var firebaseCredPath = builder.Configuration["Firebase:CredentialPath"];
if (!string.IsNullOrEmpty(firebaseCredPath) && File.Exists(firebaseCredPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseCredPath)
    });
}
```

---

## SignalR Hub

### NotificationHub

```csharp
[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Also join family group
            var familyId = await GetUserFamilyIdAsync(Guid.Parse(userId));
            if (familyId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"family_{familyId}");
            }
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Groups are automatically cleaned up
        await base.OnDisconnectedAsync(exception);
    }
}
```

### Client Events

```typescript
// Events sent from server to client
interface NotificationHubEvents {
    // New notification received
    "ReceiveNotification": (notification: NotificationDto) => void;

    // Unread count updated
    "UnreadCountChanged": (count: number) => void;

    // Notification marked as read (sync across devices)
    "NotificationRead": (notificationId: string) => void;

    // Real-time balance update
    "BalanceUpdated": (childId: string, newBalance: number) => void;
}
```

---

## Notification Templates

### Template Definitions

```csharp
public static class NotificationTemplates
{
    public static (string Title, string Body) GetTemplate(NotificationType type, Dictionary<string, string> data)
    {
        return type switch
        {
            // Balance & Transactions
            NotificationType.BalanceAlert =>
                ("Balance Alert", $"Your balance changed to {data["balance"]}"),
            NotificationType.LowBalanceWarning =>
                ("Low Balance Warning", $"Your balance is down to {data["balance"]}"),
            NotificationType.TransactionCreated =>
                ($"{data["type"]} - {data["amount"]}", data["description"]),

            // Allowance
            NotificationType.AllowanceDeposit =>
                ("Allowance Received!", $"Your weekly allowance of {data["amount"]} has been deposited. New balance: {data["balance"]}"),
            NotificationType.AllowancePaused =>
                ("Allowance Paused", $"Your allowance has been paused{(data.ContainsKey("reason") ? $": {data["reason"]}" : "")}"),
            NotificationType.AllowanceResumed =>
                ("Allowance Resumed", "Your allowance is back on track!"),

            // Goals
            NotificationType.GoalProgress =>
                ("Goal Progress", $"You're {data["percent"]}% of the way to {data["goalName"]}!"),
            NotificationType.GoalMilestone =>
                ("Milestone Reached!", $"You've hit {data["percent"]}% on your {data["goalName"]} goal!"),
            NotificationType.GoalCompleted =>
                ("Goal Achieved!", $"Congratulations! You saved enough for {data["goalName"]}!"),
            NotificationType.ParentMatchAdded =>
                ("Parent Match!", $"Your parent added {data["amount"]} to match your savings!"),

            // Tasks
            NotificationType.TaskAssigned =>
                ("New Task", $"You have a new task: {data["taskName"]} - {data["reward"]}"),
            NotificationType.TaskReminder =>
                ("Task Reminder", $"Don't forget: {data["taskName"]} is due {data["dueDate"]}"),
            NotificationType.ApprovalRequired =>
                ("Approval Needed", $"{data["childName"]} completed \"{data["taskName"]}\" - tap to review"),
            NotificationType.TaskApproved =>
                ("Task Approved!", $"\"{data["taskName"]}\" was approved! {data["reward"]} added to your balance."),
            NotificationType.TaskRejected =>
                ("Task Needs Work", $"\"{data["taskName"]}\" wasn't approved: {data["reason"]}"),

            // Budget
            NotificationType.BudgetWarning =>
                ("Budget Warning", $"You've spent {data["percent"]}% of your {data["category"]} budget"),
            NotificationType.BudgetExceeded =>
                ("Budget Exceeded", $"You've exceeded your {data["category"]} budget by {data["amount"]}"),

            // Achievements
            NotificationType.AchievementUnlocked =>
                ("Achievement Unlocked!", $"You earned the \"{data["badgeName"]}\" badge!"),
            NotificationType.StreakUpdate =>
                ("Streak Update", $"You're on a {data["count"]}-week savings streak!"),

            // Family
            NotificationType.GiftReceived =>
                ("Gift Received!", $"{data["giverName"]} sent you {data["amount"]}!"),

            // Summaries
            NotificationType.WeeklySummary =>
                ("Your Weekly Summary", $"This week: +{data["income"]} earned, -{data["spending"]} spent"),

            _ => ("Notification", "You have a new notification")
        };
    }
}
```

---

## iOS Implementation

### Firebase Setup in iOS

Add to `Podfile` or Swift Package Manager:
```ruby
pod 'FirebaseMessaging'
```

### AppDelegate Integration

```swift
import UIKit
import FirebaseCore
import FirebaseMessaging
import UserNotifications

class AppDelegate: NSObject, UIApplicationDelegate, UNUserNotificationCenterDelegate, MessagingDelegate {

    func application(
        _ application: UIApplication,
        didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]? = nil
    ) -> Bool {
        // Configure Firebase
        FirebaseApp.configure()

        // Set delegates
        UNUserNotificationCenter.current().delegate = self
        Messaging.messaging().delegate = self

        // Request notification permissions
        requestNotificationPermissions()

        return true
    }

    private func requestNotificationPermissions() {
        UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .badge, .sound]) { granted, error in
            if granted {
                DispatchQueue.main.async {
                    UIApplication.shared.registerForRemoteNotifications()
                }
            }
        }
    }

    // MARK: - APNs Token Registration

    func application(
        _ application: UIApplication,
        didRegisterForRemoteNotificationsWithDeviceToken deviceToken: Data
    ) {
        // Pass APNs token to Firebase
        Messaging.messaging().apnsToken = deviceToken
    }

    func application(
        _ application: UIApplication,
        didFailToRegisterForRemoteNotificationsWithError error: Error
    ) {
        print("Failed to register for remote notifications: \(error)")
    }

    // MARK: - Firebase Messaging Delegate

    func messaging(_ messaging: Messaging, didReceiveRegistrationToken fcmToken: String?) {
        guard let fcmToken = fcmToken else { return }

        print("FCM Token: \(fcmToken)")

        // Send FCM token to your backend
        Task {
            await registerDeviceToken(fcmToken)
        }
    }

    private func registerDeviceToken(_ token: String) async {
        let viewModel = NotificationViewModel()
        await viewModel.registerDeviceToken(token)
    }

    // MARK: - Notification Handling

    // Handle notification when app is in foreground
    func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        willPresent notification: UNNotification
    ) async -> UNNotificationPresentationOptions {
        return [.banner, .badge, .sound]
    }

    // Handle notification tap
    func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        didReceive response: UNNotificationResponse
    ) async {
        let userInfo = response.notification.request.content.userInfo

        // Handle deep linking based on notification data
        if let entityType = userInfo["relatedEntityType"] as? String,
           let entityId = userInfo["relatedEntityId"] as? String {
            await handleDeepLink(entityType: entityType, entityId: entityId)
        }
    }

    private func handleDeepLink(entityType: String, entityId: String) async {
        // Navigate to appropriate screen based on entity type
        switch entityType {
        case "Transaction":
            // Navigate to transaction detail
            break
        case "Task":
            // Navigate to task detail
            break
        case "Goal":
            // Navigate to goal detail
            break
        default:
            break
        }
    }
}
```

### Models

```swift
import Foundation

struct NotificationModel: Codable, Identifiable {
    let id: UUID
    let type: NotificationType
    let typeName: String
    let title: String
    let body: String
    let data: String?
    let isRead: Bool
    let readAt: Date?
    let createdAt: Date
    let relatedEntityType: String?
    let relatedEntityId: UUID?
    let timeAgo: String
}

struct NotificationListResponse: Codable {
    let notifications: [NotificationModel]
    let unreadCount: Int
    let totalCount: Int
    let hasMore: Bool
}

struct NotificationPreferences: Codable {
    let preferences: [NotificationPreferenceItem]
    let quietHoursEnabled: Bool
    let quietHoursStart: String?
    let quietHoursEnd: String?
}

struct NotificationPreferenceItem: Codable, Identifiable {
    var id: Int { notificationType }
    let notificationType: Int
    let typeName: String
    let category: String
    var inAppEnabled: Bool
    var pushEnabled: Bool
    var emailEnabled: Bool
}

enum NotificationType: Int, Codable, CaseIterable {
    case balanceAlert = 1
    case lowBalanceWarning = 2
    case transactionCreated = 3
    case allowanceDeposit = 10
    case goalProgress = 20
    case goalMilestone = 21
    case goalCompleted = 22
    case taskAssigned = 30
    case taskReminder = 31
    case approvalRequired = 33
    case taskApproved = 34
    case budgetWarning = 40
    case achievementUnlocked = 50
    case giftReceived = 62
    case weeklySummary = 70
}
```

### ViewModel

```swift
import Foundation
import FirebaseMessaging

@Observable
@MainActor
final class NotificationViewModel {
    var notifications: [NotificationModel] = []
    var unreadCount: Int = 0
    var isLoading = false
    var errorMessage: String?
    var hasMore = true
    var preferences: NotificationPreferences?

    private var currentPage = 1
    private let pageSize = 20
    private let apiService: APIServiceProtocol

    init(apiService: APIServiceProtocol = APIService()) {
        self.apiService = apiService
    }

    func loadNotifications(refresh: Bool = false) async {
        if refresh {
            currentPage = 1
            notifications = []
            hasMore = true
        }

        guard hasMore, !isLoading else { return }
        isLoading = true
        errorMessage = nil

        do {
            let response: NotificationListResponse = try await apiService.get(
                endpoint: "/api/v1/notifications",
                queryParams: ["page": "\(currentPage)", "pageSize": "\(pageSize)"]
            )

            if refresh {
                notifications = response.notifications
            } else {
                notifications.append(contentsOf: response.notifications)
            }

            unreadCount = response.unreadCount
            hasMore = response.hasMore
            currentPage += 1
        } catch {
            errorMessage = error.localizedDescription
        }

        isLoading = false
    }

    func markAsRead(_ notification: NotificationModel) async {
        guard !notification.isRead else { return }

        do {
            let _: NotificationModel = try await apiService.patch(
                endpoint: "/api/v1/notifications/\(notification.id)/read"
            )

            if let index = notifications.firstIndex(where: { $0.id == notification.id }) {
                // Create updated notification
                let updated = NotificationModel(
                    id: notification.id,
                    type: notification.type,
                    typeName: notification.typeName,
                    title: notification.title,
                    body: notification.body,
                    data: notification.data,
                    isRead: true,
                    readAt: Date(),
                    createdAt: notification.createdAt,
                    relatedEntityType: notification.relatedEntityType,
                    relatedEntityId: notification.relatedEntityId,
                    timeAgo: notification.timeAgo
                )
                notifications[index] = updated
                unreadCount = max(0, unreadCount - 1)
            }
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func markAllAsRead() async {
        do {
            let _: [String: Int] = try await apiService.post(
                endpoint: "/api/v1/notifications/read",
                body: MarkNotificationsReadRequest(notificationIds: nil)
            )

            // Update local state
            notifications = notifications.map { notification in
                NotificationModel(
                    id: notification.id,
                    type: notification.type,
                    typeName: notification.typeName,
                    title: notification.title,
                    body: notification.body,
                    data: notification.data,
                    isRead: true,
                    readAt: Date(),
                    createdAt: notification.createdAt,
                    relatedEntityType: notification.relatedEntityType,
                    relatedEntityId: notification.relatedEntityId,
                    timeAgo: notification.timeAgo
                )
            }
            unreadCount = 0
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func loadPreferences() async {
        do {
            preferences = try await apiService.get(endpoint: "/api/v1/notifications/preferences")
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func registerDeviceToken(_ token: String) async {
        do {
            let _: DeviceTokenResponse = try await apiService.post(
                endpoint: "/api/v1/devices",
                body: RegisterDeviceRequest(
                    token: token,
                    platform: 1, // iOS
                    deviceName: UIDevice.current.name,
                    appVersion: Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String
                )
            )
        } catch {
            print("Failed to register device token: \(error)")
        }
    }

    func refreshFCMToken() {
        Messaging.messaging().token { [weak self] token, error in
            if let token = token {
                Task {
                    await self?.registerDeviceToken(token)
                }
            }
        }
    }
}

struct RegisterDeviceRequest: Codable {
    let token: String
    let platform: Int
    let deviceName: String?
    let appVersion: String?
}

struct MarkNotificationsReadRequest: Codable {
    let notificationIds: [UUID]?
}

struct DeviceTokenResponse: Codable {
    let id: UUID
    let platform: Int
    let deviceName: String?
    let isActive: Bool
}
```

### Views

```swift
import SwiftUI

struct NotificationCenterView: View {
    @State private var viewModel = NotificationViewModel()

    var body: some View {
        NavigationStack {
            Group {
                if viewModel.isLoading && viewModel.notifications.isEmpty {
                    ProgressView("Loading...")
                } else if viewModel.notifications.isEmpty {
                    EmptyNotificationsView()
                } else {
                    notificationList
                }
            }
            .navigationTitle("Notifications")
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    if viewModel.unreadCount > 0 {
                        Button("Mark All Read") {
                            Task { await viewModel.markAllAsRead() }
                        }
                    }
                }
            }
            .refreshable {
                await viewModel.loadNotifications(refresh: true)
            }
        }
        .task {
            await viewModel.loadNotifications()
        }
    }

    private var notificationList: some View {
        List {
            ForEach(viewModel.notifications) { notification in
                NotificationRow(notification: notification)
                    .onTapGesture {
                        Task { await viewModel.markAsRead(notification) }
                        // Handle navigation based on relatedEntityType
                    }
                    .onAppear {
                        // Load more when reaching end
                        if notification.id == viewModel.notifications.last?.id {
                            Task { await viewModel.loadNotifications() }
                        }
                    }
            }

            if viewModel.isLoading {
                HStack {
                    Spacer()
                    ProgressView()
                    Spacer()
                }
            }
        }
        .listStyle(.plain)
    }
}

struct NotificationRow: View {
    let notification: NotificationModel

    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            NotificationIcon(type: notification.type)

            VStack(alignment: .leading, spacing: 4) {
                HStack {
                    Text(notification.title)
                        .font(.headline)
                        .fontWeight(notification.isRead ? .regular : .semibold)

                    Spacer()

                    Text(notification.timeAgo)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Text(notification.body)
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .lineLimit(2)
            }

            if !notification.isRead {
                Circle()
                    .fill(.blue)
                    .frame(width: 8, height: 8)
            }
        }
        .padding(.vertical, 4)
        .opacity(notification.isRead ? 0.7 : 1.0)
    }
}

struct NotificationIcon: View {
    let type: NotificationType

    var body: some View {
        Image(systemName: iconName)
            .font(.title2)
            .foregroundStyle(iconColor)
            .frame(width: 40, height: 40)
            .background(iconColor.opacity(0.1))
            .clipShape(Circle())
    }

    private var iconName: String {
        switch type {
        case .allowanceDeposit: return "dollarsign.circle.fill"
        case .balanceAlert, .lowBalanceWarning: return "exclamationmark.triangle.fill"
        case .goalProgress, .goalMilestone, .goalCompleted: return "target"
        case .taskAssigned, .taskReminder: return "checklist"
        case .approvalRequired: return "clock.badge.questionmark"
        case .taskApproved: return "checkmark.circle.fill"
        case .budgetWarning: return "chart.pie.fill"
        case .achievementUnlocked: return "medal.fill"
        case .giftReceived: return "gift.fill"
        case .weeklySummary: return "calendar"
        default: return "bell.fill"
        }
    }

    private var iconColor: Color {
        switch type {
        case .allowanceDeposit, .taskApproved, .goalCompleted: return .green
        case .balanceAlert, .lowBalanceWarning, .budgetWarning: return .orange
        case .goalProgress, .goalMilestone: return .blue
        case .achievementUnlocked: return .purple
        case .giftReceived: return .pink
        case .approvalRequired: return .yellow
        default: return .gray
        }
    }
}

struct NotificationPreferencesView: View {
    @State private var viewModel = NotificationViewModel()

    var body: some View {
        List {
            if let preferences = viewModel.preferences {
                ForEach(groupedPreferences(preferences.preferences), id: \.0) { category, items in
                    Section(category) {
                        ForEach(items) { pref in
                            NotificationPreferenceRow(preference: pref)
                        }
                    }
                }

                Section("Quiet Hours") {
                    Toggle("Enable Quiet Hours", isOn: .constant(preferences.quietHoursEnabled))
                    // Time pickers for start/end
                }
            }
        }
        .navigationTitle("Notification Settings")
        .task {
            await viewModel.loadPreferences()
        }
    }

    private func groupedPreferences(_ prefs: [NotificationPreferenceItem]) -> [(String, [NotificationPreferenceItem])] {
        Dictionary(grouping: prefs, by: { $0.category })
            .sorted { $0.key < $1.key }
    }
}

struct NotificationPreferenceRow: View {
    let preference: NotificationPreferenceItem

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(preference.typeName)
                .font(.headline)

            HStack(spacing: 16) {
                Toggle("In-App", isOn: .constant(preference.inAppEnabled))
                Toggle("Push", isOn: .constant(preference.pushEnabled))
                Toggle("Email", isOn: .constant(preference.emailEnabled))
            }
            .font(.caption)
        }
        .padding(.vertical, 4)
    }
}

struct EmptyNotificationsView: View {
    var body: some View {
        ContentUnavailableView(
            "No Notifications",
            systemImage: "bell.slash",
            description: Text("You're all caught up!")
        )
    }
}
```

---

## React Implementation

### Types

```typescript
// types/notifications.ts
export interface Notification {
  id: string;
  type: NotificationType;
  typeName: string;
  title: string;
  body: string;
  data?: string;
  isRead: boolean;
  readAt?: string;
  createdAt: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  timeAgo: string;
}

export interface NotificationListResponse {
  notifications: Notification[];
  unreadCount: number;
  totalCount: number;
  hasMore: boolean;
}

export interface NotificationPreferences {
  preferences: NotificationPreferenceItem[];
  quietHoursEnabled: boolean;
  quietHoursStart?: string;
  quietHoursEnd?: string;
}

export interface NotificationPreferenceItem {
  notificationType: NotificationType;
  typeName: string;
  category: string;
  inAppEnabled: boolean;
  pushEnabled: boolean;
  emailEnabled: boolean;
}

export enum NotificationType {
  BalanceAlert = 1,
  LowBalanceWarning = 2,
  TransactionCreated = 3,
  AllowanceDeposit = 10,
  GoalProgress = 20,
  GoalMilestone = 21,
  GoalCompleted = 22,
  TaskAssigned = 30,
  ApprovalRequired = 33,
  TaskApproved = 34,
  BudgetWarning = 40,
  AchievementUnlocked = 50,
  GiftReceived = 62,
  WeeklySummary = 70,
}
```

### Hooks

```typescript
// hooks/useNotifications.ts
import { useState, useEffect, useCallback } from 'react';
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';
import { api } from '../services/api';
import { Notification, NotificationListResponse } from '../types/notifications';

export function useNotifications() {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [page, setPage] = useState(1);
  const [connection, setConnection] = useState<HubConnection | null>(null);

  // Setup SignalR connection
  useEffect(() => {
    const newConnection = new HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => localStorage.getItem('token') || '',
      })
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);

    return () => {
      newConnection.stop();
    };
  }, []);

  // Start connection and subscribe to events
  useEffect(() => {
    if (!connection) return;

    connection.start().then(() => {
      connection.on('ReceiveNotification', (notification: Notification) => {
        setNotifications((prev) => [notification, ...prev]);
        setUnreadCount((prev) => prev + 1);
      });

      connection.on('UnreadCountChanged', (count: number) => {
        setUnreadCount(count);
      });

      connection.on('NotificationRead', (notificationId: string) => {
        setNotifications((prev) =>
          prev.map((n) =>
            n.id === notificationId ? { ...n, isRead: true } : n
          )
        );
      });
    });

    return () => {
      connection.off('ReceiveNotification');
      connection.off('UnreadCountChanged');
      connection.off('NotificationRead');
    };
  }, [connection]);

  const loadNotifications = useCallback(async (refresh = false) => {
    if (isLoading || (!hasMore && !refresh)) return;

    setIsLoading(true);
    const currentPage = refresh ? 1 : page;

    try {
      const response = await api.get<NotificationListResponse>(
        `/api/v1/notifications?page=${currentPage}&pageSize=20`
      );

      if (refresh) {
        setNotifications(response.notifications);
      } else {
        setNotifications((prev) => [...prev, ...response.notifications]);
      }

      setUnreadCount(response.unreadCount);
      setHasMore(response.hasMore);
      setPage(currentPage + 1);
    } catch (error) {
      console.error('Failed to load notifications:', error);
    } finally {
      setIsLoading(false);
    }
  }, [isLoading, hasMore, page]);

  const markAsRead = useCallback(async (notificationId: string) => {
    try {
      await api.patch(`/api/v1/notifications/${notificationId}/read`);
      setNotifications((prev) =>
        prev.map((n) =>
          n.id === notificationId ? { ...n, isRead: true } : n
        )
      );
      setUnreadCount((prev) => Math.max(0, prev - 1));
    } catch (error) {
      console.error('Failed to mark notification as read:', error);
    }
  }, []);

  const markAllAsRead = useCallback(async () => {
    try {
      await api.post('/api/v1/notifications/read', { notificationIds: null });
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
      setUnreadCount(0);
    } catch (error) {
      console.error('Failed to mark all as read:', error);
    }
  }, []);

  return {
    notifications,
    unreadCount,
    isLoading,
    hasMore,
    loadNotifications,
    markAsRead,
    markAllAsRead,
  };
}
```

### Components

```tsx
// components/NotificationCenter.tsx
import React, { useEffect } from 'react';
import { useNotifications } from '../hooks/useNotifications';
import { Notification, NotificationType } from '../types/notifications';
import { Bell, Check, Gift, Target, DollarSign, AlertTriangle } from 'lucide-react';

export function NotificationCenter() {
  const {
    notifications,
    unreadCount,
    isLoading,
    hasMore,
    loadNotifications,
    markAsRead,
    markAllAsRead,
  } = useNotifications();

  useEffect(() => {
    loadNotifications(true);
  }, []);

  return (
    <div className="w-96 max-h-[500px] bg-white rounded-lg shadow-xl border overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b">
        <h3 className="font-semibold text-gray-900">
          Notifications
          {unreadCount > 0 && (
            <span className="ml-2 bg-blue-500 text-white text-xs px-2 py-0.5 rounded-full">
              {unreadCount}
            </span>
          )}
        </h3>
        {unreadCount > 0 && (
          <button
            onClick={markAllAsRead}
            className="text-sm text-blue-600 hover:text-blue-800"
          >
            Mark all read
          </button>
        )}
      </div>

      {/* Notification List */}
      <div className="overflow-y-auto max-h-[400px]">
        {notifications.length === 0 && !isLoading ? (
          <div className="p-8 text-center text-gray-500">
            <Bell className="w-12 h-12 mx-auto mb-2 opacity-50" />
            <p>No notifications yet</p>
          </div>
        ) : (
          notifications.map((notification) => (
            <NotificationItem
              key={notification.id}
              notification={notification}
              onRead={() => markAsRead(notification.id)}
            />
          ))
        )}

        {isLoading && (
          <div className="p-4 text-center">
            <div className="animate-spin w-6 h-6 border-2 border-blue-500 border-t-transparent rounded-full mx-auto" />
          </div>
        )}

        {hasMore && !isLoading && (
          <button
            onClick={() => loadNotifications()}
            className="w-full p-3 text-sm text-blue-600 hover:bg-gray-50"
          >
            Load more
          </button>
        )}
      </div>
    </div>
  );
}

interface NotificationItemProps {
  notification: Notification;
  onRead: () => void;
}

function NotificationItem({ notification, onRead }: NotificationItemProps) {
  const Icon = getNotificationIcon(notification.type);
  const iconColor = getNotificationColor(notification.type);

  return (
    <div
      className={`p-4 border-b hover:bg-gray-50 cursor-pointer transition-colors ${
        notification.isRead ? 'opacity-60' : ''
      }`}
      onClick={onRead}
    >
      <div className="flex gap-3">
        <div
          className={`w-10 h-10 rounded-full flex items-center justify-center ${iconColor}`}
        >
          <Icon className="w-5 h-5" />
        </div>

        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between gap-2">
            <p
              className={`text-sm ${
                notification.isRead ? 'text-gray-600' : 'text-gray-900 font-medium'
              }`}
            >
              {notification.title}
            </p>
            {!notification.isRead && (
              <div className="w-2 h-2 bg-blue-500 rounded-full flex-shrink-0 mt-1.5" />
            )}
          </div>
          <p className="text-sm text-gray-500 mt-0.5 line-clamp-2">
            {notification.body}
          </p>
          <p className="text-xs text-gray-400 mt-1">{notification.timeAgo}</p>
        </div>
      </div>
    </div>
  );
}

function getNotificationIcon(type: NotificationType) {
  switch (type) {
    case NotificationType.AllowanceDeposit:
    case NotificationType.TransactionCreated:
      return DollarSign;
    case NotificationType.GoalProgress:
    case NotificationType.GoalMilestone:
    case NotificationType.GoalCompleted:
      return Target;
    case NotificationType.TaskApproved:
      return Check;
    case NotificationType.GiftReceived:
      return Gift;
    case NotificationType.BalanceAlert:
    case NotificationType.BudgetWarning:
      return AlertTriangle;
    default:
      return Bell;
  }
}

function getNotificationColor(type: NotificationType): string {
  switch (type) {
    case NotificationType.AllowanceDeposit:
    case NotificationType.TaskApproved:
    case NotificationType.GoalCompleted:
      return 'bg-green-100 text-green-600';
    case NotificationType.BalanceAlert:
    case NotificationType.BudgetWarning:
      return 'bg-orange-100 text-orange-600';
    case NotificationType.GiftReceived:
      return 'bg-pink-100 text-pink-600';
    case NotificationType.AchievementUnlocked:
      return 'bg-purple-100 text-purple-600';
    default:
      return 'bg-blue-100 text-blue-600';
  }
}
```

### Notification Bell Component

```tsx
// components/NotificationBell.tsx
import React, { useState, useRef, useEffect } from 'react';
import { Bell } from 'lucide-react';
import { useNotifications } from '../hooks/useNotifications';
import { NotificationCenter } from './NotificationCenter';

export function NotificationBell() {
  const [isOpen, setIsOpen] = useState(false);
  const { unreadCount } = useNotifications();
  const ref = useRef<HTMLDivElement>(null);

  // Close on click outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (ref.current && !ref.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="relative p-2 text-gray-600 hover:text-gray-900 hover:bg-gray-100 rounded-full"
      >
        <Bell className="w-6 h-6" />
        {unreadCount > 0 && (
          <span className="absolute -top-1 -right-1 w-5 h-5 bg-red-500 text-white text-xs rounded-full flex items-center justify-center">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 z-50">
          <NotificationCenter />
        </div>
      )}
    </div>
  );
}
```

---

## Testing Strategy

### Unit Tests (Service Layer) - 15 tests

```csharp
public class NotificationServiceTests
{
    [Fact]
    public async Task CreateNotification_SavesCorrectly()
    {
        // Arrange
        var dto = new CreateNotificationInternalDto(
            UserId: _testUserId,
            Type: NotificationType.AllowanceDeposit,
            Title: "Test",
            Body: "Test body"
        );

        // Act
        var result = await _service.CreateNotificationAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(NotificationType.AllowanceDeposit);
        result.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsCorrectCount()
    {
        // Arrange - Create 3 notifications, mark 1 as read
        await CreateNotifications(3);
        await _service.MarkAsReadAsync(_notifications[0].Id, _testUserId);

        // Act
        var count = await _service.GetUnreadCountAsync(_testUserId);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task MarkAsRead_UpdatesNotification()
    {
        // Arrange
        var notification = await CreateNotification();

        // Act
        var result = await _service.MarkAsReadAsync(notification.Id, _testUserId);

        // Assert
        result.IsRead.Should().BeTrue();
        result.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldSend_RespectsUserPreferences()
    {
        // Arrange - Disable push for balance alerts
        await DisablePushForType(NotificationType.BalanceAlert);

        // Act
        var shouldSend = await _service.ShouldSendAsync(
            _testUserId,
            NotificationType.BalanceAlert,
            NotificationChannel.Push
        );

        // Assert
        shouldSend.Should().BeFalse();
    }

    [Fact]
    public async Task SendNotification_UsesFirebase()
    {
        // Arrange
        _mockFirebaseService.Setup(x => x.SendPushAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(true);

        // Act
        await _service.SendNotificationAsync(
            _testUserId,
            NotificationType.AllowanceDeposit,
            "Allowance Received!",
            "Your weekly allowance of $10 has been deposited."
        );

        // Assert
        _mockFirebaseService.Verify(x => x.SendPushAsync(_testUserId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()), Times.Once);
    }

    // ... 10 more tests
}
```

### Firebase Push Service Tests

```csharp
public class FirebasePushServiceTests
{
    [Fact]
    public async Task SendPushAsync_SendsToAllUserDevices()
    {
        // Arrange
        await RegisterDevices(_testUserId, 2);

        // Act
        var result = await _service.SendPushAsync(_testUserId, "Test", "Body");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendToDeviceAsync_HandlesUnregisteredToken()
    {
        // Arrange
        var token = await RegisterDevice(_testUserId);
        _mockFirebaseMessaging.Setup(x => x.SendAsync(It.IsAny<Message>()))
            .ThrowsAsync(new FirebaseMessagingException(MessagingErrorCode.Unregistered, "Token expired"));

        // Act
        var result = await _service.SendToDeviceAsync(token.Token, "Test", "Body");

        // Assert
        result.Should().BeFalse();

        // Token should be deactivated
        var device = await _context.DeviceTokens.FirstAsync(d => d.Token == token.Token);
        device.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SendToMultipleAsync_ReturnsSuccessCount()
    {
        // Arrange
        var tokens = new List<string> { "token1", "token2", "token3" };
        _mockFirebaseMessaging.Setup(x => x.SendEachForMulticastAsync(It.IsAny<MulticastMessage>()))
            .ReturnsAsync(new BatchResponse(new[]
            {
                new SendResponse { IsSuccess = true },
                new SendResponse { IsSuccess = false },
                new SendResponse { IsSuccess = true }
            }));

        // Act
        var successCount = await _service.SendToMultipleAsync(tokens, "Test", "Body");

        // Assert
        successCount.Should().Be(2);
    }
}
```

### Controller Tests - 10 tests

```csharp
public class NotificationsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetNotifications_ReturnsOk_WithPaginatedList() { /* ... */ }

    [Fact]
    public async Task GetNotifications_RequiresAuthentication() { /* ... */ }

    [Fact]
    public async Task MarkAsRead_UpdatesNotification() { /* ... */ }

    [Fact]
    public async Task RegisterDevice_CreatesToken() { /* ... */ }

    [Fact]
    public async Task RegisterDevice_UpdatesExistingToken() { /* ... */ }

    // ... 5 more tests
}
```

### Integration Tests - 10 tests

```csharp
public class NotificationIntegrationTests
{
    [Fact]
    public async Task TransactionCreated_TriggersNotification() { /* ... */ }

    [Fact]
    public async Task AllowanceDeposit_NotifiesChild() { /* ... */ }

    [Fact]
    public async Task Firebase_SendsPushToDevice()
    {
        // Arrange
        var userId = await CreateTestUser();
        await RegisterDevice(userId, "test-fcm-token", DevicePlatform.iOS);

        // Act
        var sent = await _pushService.SendPushAsync(userId, "Test Title", "Test Body");

        // Assert
        sent.Should().BeTrue();
        _mockFirebaseMessaging.Verify(x => x.SendAsync(
            It.Is<Message>(m => m.Token == "test-fcm-token")
        ), Times.Once);
    }

    // ... 7 more tests
}
```

---

## Implementation Phases

### Phase 1: Database & Core Models (2 days)
- [ ] Create `Notification` model
- [ ] Create `NotificationPreference` model
- [ ] Create `DeviceToken` model
- [ ] Create enums (`NotificationType`, `NotificationStatus`, etc.)
- [ ] Add database migration
- [ ] Configure EF Core relationships and indexes
- [ ] Write model unit tests

### Phase 2: Notification Service (3 days)
- [ ] Write `INotificationService` tests
- [ ] Implement `NotificationService`
- [ ] Write `IDeviceTokenService` tests
- [ ] Implement `DeviceTokenService`
- [ ] Implement notification templates
- [ ] Add preference checking logic

### Phase 3: API Controllers (2 days)
- [ ] Write `NotificationsController` tests
- [ ] Implement `NotificationsController`
- [ ] Implement device registration endpoints
- [ ] Implement preference endpoints
- [ ] Register services in DI

### Phase 4: SignalR Integration (2 days)
- [ ] Create `NotificationHub`
- [ ] Implement `ISignalRNotificationService`
- [ ] Configure SignalR in Program.cs
- [ ] Add client connection handling
- [ ] Test real-time notifications

### Phase 5: Firebase Push Integration (3 days)
- [ ] Add Firebase Admin SDK package
- [ ] Configure Firebase credentials
- [ ] Implement `IFirebasePushService`
- [ ] Handle FCM token registration from iOS/Android
- [ ] Test push delivery
- [ ] Handle token invalidation

### Phase 6: Event Triggers (2 days)
- [ ] Add notification triggers to `TransactionService`
- [ ] Add notification triggers to `AllowanceService`
- [ ] Add notification triggers to `TaskService`
- [ ] Add notification triggers to `CategoryBudgetService`
- [ ] Test end-to-end flows

### Phase 7: Frontend Integration (3 days)
- [ ] Create React `NotificationCenter` component
- [ ] Implement `useNotifications` hook
- [ ] Setup SignalR client connection
- [ ] Add notification bell to header
- [ ] Create notification preferences page

### Phase 8: iOS Integration (3 days)
- [ ] Add Firebase SDK to iOS project
- [ ] Create `NotificationViewModel`
- [ ] Create `NotificationCenterView`
- [ ] Implement FCM registration in AppDelegate
- [ ] Create notification preferences UI
- [ ] Test push notifications on device

---

## Success Criteria

### Functional Requirements
- [ ] Users can view notification history
- [ ] Notifications marked as read persist across sessions
- [ ] Push notifications deliver to iOS devices via FCM within 5 seconds
- [ ] Web notifications appear in real-time via SignalR
- [ ] Users can customize notification preferences
- [ ] Quiet hours prevent notifications during specified times
- [ ] Device tokens are properly managed

### Performance Requirements
- [ ] Notification list loads in <200ms
- [ ] Unread count updates in real-time
- [ ] Push notification delivery <5 seconds
- [ ] SignalR reconnects automatically on disconnect

### Test Coverage
- [ ] >90% coverage on NotificationService
- [ ] >90% coverage on FirebasePushService
- [ ] >90% coverage on DeviceTokenService
- [ ] All API endpoints tested
- [ ] Integration tests for notification triggers

---

## Database Indexes

```csharp
modelBuilder.Entity<Notification>()
    .HasIndex(n => n.UserId);

modelBuilder.Entity<Notification>()
    .HasIndex(n => new { n.UserId, n.IsRead });

modelBuilder.Entity<Notification>()
    .HasIndex(n => n.CreatedAt);

modelBuilder.Entity<NotificationPreference>()
    .HasIndex(np => new { np.UserId, np.NotificationType })
    .IsUnique();

modelBuilder.Entity<DeviceToken>()
    .HasIndex(dt => dt.Token)
    .IsUnique();

modelBuilder.Entity<DeviceToken>()
    .HasIndex(dt => new { dt.UserId, dt.IsActive });
```

---

## Configuration

### appsettings.json

```json
{
  "Firebase": {
    "CredentialPath": "firebase-service-account.json",
    "ProjectId": "allowance-tracker"
  },
  "SignalR": {
    "KeepAliveInterval": "00:00:15",
    "ClientTimeoutInterval": "00:00:30"
  },
  "Notifications": {
    "MaxDevicesPerUser": 5,
    "NotificationExpiresDays": 30,
    "StaleTokenCleanupDays": 90
  }
}
```

### Firebase Service Account

Download from Firebase Console > Project Settings > Service Accounts > Generate new private key.

Place the JSON file at the path specified in `Firebase:CredentialPath`.

### NuGet Packages Required

```xml
<PackageReference Include="FirebaseAdmin" Version="2.4.0" />
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="1.1.0" />
```

---

This specification provides a complete foundation for implementing the Push Notifications system using Firebase Cloud Messaging, following TDD principles and maintaining consistency with the existing AllowanceTracker architecture.
