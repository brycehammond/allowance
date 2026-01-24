import Foundation
import SwiftUI

/// ViewModel for notification management
@Observable
@MainActor
final class NotificationViewModel {

    // MARK: - Observable Properties

    private(set) var notifications: [NotificationDto] = []
    private(set) var unreadCount: Int = 0
    private(set) var isLoading = false
    private(set) var isLoadingMore = false
    private(set) var hasMore = true
    var errorMessage: String?

    // MARK: - Private Properties

    private let apiService: APIServiceProtocol
    private var currentPage = 1
    private let pageSize = 20

    // MARK: - Initialization

    init(apiService: APIServiceProtocol = ServiceProvider.apiService) {
        self.apiService = apiService
    }

    // MARK: - Public Methods

    /// Load initial notifications
    func loadNotifications() async {
        // Clear previous errors
        errorMessage = nil

        // Set loading state
        isLoading = true
        defer { isLoading = false }

        // Reset pagination
        currentPage = 1

        do {
            let response = try await apiService.getNotifications(
                page: currentPage,
                pageSize: pageSize,
                unreadOnly: false,
                type: nil
            )

            notifications = response.notifications
            unreadCount = response.unreadCount
            hasMore = response.hasMore
            currentPage = 2

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load notifications. Please try again."
        }
    }

    /// Load more notifications (pagination)
    func loadMoreNotifications() async {
        guard !isLoadingMore && hasMore else { return }

        isLoadingMore = true
        defer { isLoadingMore = false }

        do {
            let response = try await apiService.getNotifications(
                page: currentPage,
                pageSize: pageSize,
                unreadOnly: false,
                type: nil
            )

            notifications.append(contentsOf: response.notifications)
            unreadCount = response.unreadCount
            hasMore = response.hasMore
            currentPage += 1

        } catch {
            // Silently fail for load more
        }
    }

    /// Refresh unread count only
    func refreshUnreadCount() async {
        do {
            unreadCount = try await apiService.getUnreadCount()
        } catch {
            // Silently fail
        }
    }

    /// Mark a notification as read
    /// - Parameter notification: Notification to mark as read
    func markAsRead(_ notification: NotificationDto) async {
        guard !notification.isRead else { return }

        do {
            let updated = try await apiService.markNotificationAsRead(id: notification.id)

            // Update local state
            if let index = notifications.firstIndex(where: { $0.id == notification.id }) {
                notifications[index] = updated
            }
            unreadCount = max(0, unreadCount - 1)

        } catch {
            // Silently fail
        }
    }

    /// Mark all notifications as read
    func markAllAsRead() async {
        let request = MarkNotificationsReadRequest(notificationIds: nil)

        do {
            _ = try await apiService.markMultipleAsRead(request)

            // Update local state
            notifications = notifications.map { notification in
                NotificationDto(
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
            // Silently fail
        }
    }

    /// Delete a notification
    /// - Parameter notification: Notification to delete
    func delete(_ notification: NotificationDto) async {
        do {
            try await apiService.deleteNotification(id: notification.id)

            // Update local state
            notifications.removeAll { $0.id == notification.id }
            if !notification.isRead {
                unreadCount = max(0, unreadCount - 1)
            }

        } catch {
            // Silently fail
        }
    }

    /// Refresh all notification data
    func refresh() async {
        await loadNotifications()
    }

    /// Clear error message
    func clearError() {
        errorMessage = nil
    }

    // MARK: - Push Notification Methods

    /// Request push notification permission
    func requestPushNotificationPermission() async -> Bool {
        await PushNotificationService.shared.requestAuthorization()
    }

    /// Register device token with backend
    func registerDeviceToken() async {
        // Get current token from PushNotificationService
        if let fcmToken = PushNotificationService.shared.fcmToken {
            await PushNotificationService.shared.registerDeviceToken(fcmToken)
        } else if let apnsToken = PushNotificationService.shared.apnsToken {
            let tokenString = apnsToken.map { String(format: "%02.2hhx", $0) }.joined()
            await PushNotificationService.shared.registerDeviceToken(tokenString)
        }
    }

    /// Refresh FCM/APNs token
    func refreshFCMToken() async {
        await PushNotificationService.shared.refreshToken()
    }

    /// Load notification preferences
    func loadPreferences() async -> NotificationPreferences? {
        do {
            return try await apiService.getNotificationPreferences()
        } catch {
            print("Failed to load notification preferences: \(error)")
            return nil
        }
    }

    /// Update notification preferences
    func updatePreferences(_ request: UpdateNotificationPreferencesRequest) async -> Bool {
        do {
            _ = try await apiService.updateNotificationPreferences(request)
            return true
        } catch {
            print("Failed to update notification preferences: \(error)")
            return false
        }
    }

    /// Update quiet hours
    func updateQuietHours(_ request: UpdateQuietHoursRequest) async -> Bool {
        do {
            _ = try await apiService.updateQuietHours(request)
            return true
        } catch {
            print("Failed to update quiet hours: \(error)")
            return false
        }
    }

    /// Update badge count based on unread notifications
    func updateBadgeCount() async {
        await PushNotificationService.shared.setBadge(unreadCount)
    }

    /// Clear notification badge
    func clearBadge() async {
        await PushNotificationService.shared.clearBadge()
    }

    // MARK: - Computed Properties

    /// Notifications that are unread
    var unreadNotifications: [NotificationDto] {
        notifications.filter { !$0.isRead }
    }

    /// Whether there are any unread notifications
    var hasUnread: Bool {
        unreadCount > 0
    }
}
