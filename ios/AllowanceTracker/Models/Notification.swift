import Foundation

// MARK: - Enums

enum NotificationType: Int, Codable {
    // Balance & Transactions
    case balanceAlert = 1
    case lowBalanceWarning = 2
    case transactionCreated = 3
    // Allowance
    case allowanceDeposit = 10
    case allowancePaused = 11
    case allowanceResumed = 12
    // Goals & Savings
    case goalProgress = 20
    case goalMilestone = 21
    case goalCompleted = 22
    case parentMatchAdded = 23
    // Tasks
    case taskAssigned = 30
    case taskReminder = 31
    case taskCompleted = 32
    case approvalRequired = 33
    case taskApproved = 34
    case taskRejected = 35
    // Budget
    case budgetWarning = 40
    case budgetExceeded = 41
    // Achievements
    case achievementUnlocked = 50
    case streakUpdate = 51
    // Family
    case familyInvite = 60
    case childAdded = 61
    case giftReceived = 62
    // System
    case weeklySummary = 70
    case monthlySummary = 71
    case systemAnnouncement = 99

    var systemImage: String {
        switch self {
        case .balanceAlert, .lowBalanceWarning, .budgetWarning, .budgetExceeded:
            return "exclamationmark.triangle.fill"
        case .transactionCreated, .allowanceDeposit:
            return "dollarsign.circle.fill"
        case .allowancePaused:
            return "pause.circle.fill"
        case .allowanceResumed:
            return "play.circle.fill"
        case .goalProgress, .goalMilestone:
            return "target"
        case .goalCompleted:
            return "checkmark.circle.fill"
        case .parentMatchAdded, .giftReceived:
            return "gift.fill"
        case .taskAssigned, .taskReminder:
            return "checklist"
        case .taskCompleted, .taskApproved:
            return "checkmark.circle.fill"
        case .approvalRequired:
            return "clock.badge.exclamationmark.fill"
        case .taskRejected:
            return "xmark.circle.fill"
        case .achievementUnlocked:
            return "trophy.fill"
        case .streakUpdate:
            return "flame.fill"
        case .familyInvite, .childAdded:
            return "person.badge.plus"
        case .weeklySummary, .monthlySummary:
            return "calendar"
        case .systemAnnouncement:
            return "megaphone.fill"
        }
    }

    var colorName: String {
        switch self {
        case .allowanceDeposit, .transactionCreated, .goalCompleted, .taskCompleted, .taskApproved:
            return "green"
        case .balanceAlert, .lowBalanceWarning, .budgetWarning, .budgetExceeded:
            return "orange"
        case .taskRejected:
            return "red"
        case .goalProgress, .goalMilestone, .taskAssigned, .taskReminder, .familyInvite, .childAdded:
            return "blue"
        case .parentMatchAdded, .giftReceived, .achievementUnlocked:
            return "purple"
        case .streakUpdate:
            return "orange"
        case .approvalRequired:
            return "yellow"
        default:
            return "gray"
        }
    }
}

enum NotificationStatus: Int, Codable {
    case pending = 1
    case sent = 2
    case delivered = 3
    case failed = 4
    case expired = 5
}

enum NotificationChannel: Int, Codable {
    case inApp = 1
    case push = 2
    case email = 3
    case all = 99
}

enum DevicePlatform: Int, Codable {
    case iOS = 1
    case android = 2
    case web = 3
}

// MARK: - DTOs

struct NotificationDto: Codable, Identifiable {
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
    let relatedEntityId: String?
    let timeAgo: String
}

struct NotificationListResponse: Codable {
    let notifications: [NotificationDto]
    let unreadCount: Int
    let totalCount: Int
    let hasMore: Bool
}

struct NotificationPreferenceItem: Codable {
    let notificationType: NotificationType
    let typeName: String
    let category: String
    let inAppEnabled: Bool
    let pushEnabled: Bool
    let emailEnabled: Bool
}

struct NotificationPreferences: Codable {
    let preferences: [NotificationPreferenceItem]
    let quietHoursEnabled: Bool
    let quietHoursStart: String?
    let quietHoursEnd: String?
}

struct DeviceTokenDto: Codable, Identifiable {
    let id: UUID
    let platform: DevicePlatform
    let deviceName: String?
    let isActive: Bool
    let createdAt: Date
    let lastUsedAt: Date?
}

// MARK: - Request DTOs

struct RegisterDeviceRequest: Codable {
    let token: String
    let platform: DevicePlatform
    let deviceName: String?
    let appVersion: String?
}

struct MarkNotificationsReadRequest: Codable {
    let notificationIds: [UUID]?
}

struct UpdateNotificationPreferenceRequest: Codable {
    let notificationType: NotificationType
    let inAppEnabled: Bool
    let pushEnabled: Bool
    let emailEnabled: Bool
}

struct UpdateNotificationPreferencesRequest: Codable {
    let preferences: [UpdateNotificationPreferenceRequest]
}

struct UpdateQuietHoursRequest: Codable {
    let enabled: Bool
    let startTime: String?
    let endTime: String?
}
