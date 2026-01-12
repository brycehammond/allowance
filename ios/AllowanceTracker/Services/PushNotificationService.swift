import Foundation
import UserNotifications
import UIKit

/// Service for managing push notifications
/// Handles both Firebase Cloud Messaging (FCM) and native APNs
@MainActor
final class PushNotificationService: NSObject, ObservableObject {

    // MARK: - Singleton

    static let shared = PushNotificationService()

    // MARK: - Published Properties

    @Published private(set) var isAuthorized = false
    @Published private(set) var fcmToken: String?
    @Published private(set) var apnsToken: Data?

    // MARK: - Private Properties

    private let apiService: APIServiceProtocol
    private var hasRegisteredToken = false

    // MARK: - Initialization

    private override init() {
        self.apiService = APIService()
        super.init()
    }

    // For testing
    init(apiService: APIServiceProtocol) {
        self.apiService = apiService
        super.init()
    }

    // MARK: - Public Methods

    /// Request notification authorization from the user
    func requestAuthorization() async -> Bool {
        let center = UNUserNotificationCenter.current()

        do {
            let granted = try await center.requestAuthorization(options: [.alert, .badge, .sound])
            isAuthorized = granted

            if granted {
                // Register for remote notifications on the main thread
                await MainActor.run {
                    UIApplication.shared.registerForRemoteNotifications()
                }
            }

            return granted
        } catch {
            print("PushNotificationService: Authorization request failed: \(error)")
            return false
        }
    }

    /// Check current authorization status
    func checkAuthorizationStatus() async {
        let center = UNUserNotificationCenter.current()
        let settings = await center.notificationSettings()

        isAuthorized = settings.authorizationStatus == .authorized

        if isAuthorized {
            await MainActor.run {
                UIApplication.shared.registerForRemoteNotifications()
            }
        }
    }

    /// Called when APNs token is received
    func didRegisterForRemoteNotifications(deviceToken: Data) {
        apnsToken = deviceToken

        let tokenString = deviceToken.map { String(format: "%02.2hhx", $0) }.joined()
        print("PushNotificationService: APNs token received: \(tokenString)")

        // If Firebase is not configured, use APNs token directly
        // Otherwise, Firebase will handle token management
        #if !FIREBASE_ENABLED
        Task {
            await registerDeviceToken(tokenString)
        }
        #endif
    }

    /// Called when APNs registration fails
    func didFailToRegisterForRemoteNotifications(error: Error) {
        print("PushNotificationService: Failed to register for remote notifications: \(error)")
    }

    /// Set FCM token (called by Firebase Messaging delegate)
    func setFCMToken(_ token: String) {
        fcmToken = token
        print("PushNotificationService: FCM token received: \(token)")

        Task {
            await registerDeviceToken(token)
        }
    }

    /// Register device token with the backend
    func registerDeviceToken(_ token: String) async {
        // Avoid registering the same token multiple times
        guard !hasRegisteredToken else { return }

        let deviceName = await UIDevice.current.name
        let appVersion = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String

        let request = RegisterDeviceRequest(
            token: token,
            platform: .iOS,
            deviceName: deviceName,
            appVersion: appVersion
        )

        do {
            let _ = try await apiService.registerDevice(request)
            hasRegisteredToken = true
            print("PushNotificationService: Device registered successfully")
        } catch {
            print("PushNotificationService: Failed to register device: \(error)")
        }
    }

    /// Refresh FCM token (call periodically or when token might have changed)
    func refreshToken() async {
        hasRegisteredToken = false

        if let token = fcmToken {
            await registerDeviceToken(token)
        } else if let apnsToken = apnsToken {
            let tokenString = apnsToken.map { String(format: "%02.2hhx", $0) }.joined()
            await registerDeviceToken(tokenString)
        }
    }

    /// Handle incoming notification when app is in foreground
    func handleForegroundNotification(_ notification: UNNotification) async -> UNNotificationPresentationOptions {
        let content = notification.request.content
        print("PushNotificationService: Received notification in foreground: \(content.title)")

        // Show banner and play sound even when app is in foreground
        return [.banner, .sound, .badge]
    }

    /// Handle notification tap
    func handleNotificationTap(_ response: UNNotificationResponse) async {
        let userInfo = response.notification.request.content.userInfo
        print("PushNotificationService: Notification tapped: \(userInfo)")

        // Post notification for the app to handle navigation
        await MainActor.run {
            NotificationCenter.default.post(
                name: .didTapPushNotification,
                object: nil,
                userInfo: userInfo
            )
        }
    }

    /// Clear badge count
    func clearBadge() async {
        await MainActor.run {
            UIApplication.shared.applicationIconBadgeNumber = 0
        }

        // Also clear on the notification center
        let center = UNUserNotificationCenter.current()
        await center.setBadgeCount(0)
    }

    /// Update badge count
    func setBadge(_ count: Int) async {
        await MainActor.run {
            UIApplication.shared.applicationIconBadgeNumber = count
        }

        let center = UNUserNotificationCenter.current()
        try? await center.setBadgeCount(count)
    }

    // MARK: - Reset

    /// Reset token registration (call on logout)
    func reset() {
        hasRegisteredToken = false
        fcmToken = nil
    }
}

// MARK: - Notification Names

extension Notification.Name {
    static let didTapPushNotification = Notification.Name("didTapPushNotification")
    static let didReceiveRemoteNotification = Notification.Name("didReceiveRemoteNotification")
}
