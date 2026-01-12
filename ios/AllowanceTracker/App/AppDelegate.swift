import UIKit
import UserNotifications

/// AppDelegate for handling push notifications and Firebase configuration
/// Uses UIApplicationDelegateAdaptor pattern for SwiftUI integration
class AppDelegate: NSObject, UIApplicationDelegate {

    // MARK: - Application Lifecycle

    func application(
        _ application: UIApplication,
        didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]? = nil
    ) -> Bool {
        // Configure Firebase if available
        configureFirebase()

        // Set up notification delegate
        UNUserNotificationCenter.current().delegate = self

        // Check if launched from notification
        if let notification = launchOptions?[.remoteNotification] as? [String: AnyObject] {
            handleLaunchNotification(notification)
        }

        return true
    }

    // MARK: - Remote Notifications

    func application(
        _ application: UIApplication,
        didRegisterForRemoteNotificationsWithDeviceToken deviceToken: Data
    ) {
        Task { @MainActor in
            PushNotificationService.shared.didRegisterForRemoteNotifications(deviceToken: deviceToken)
        }

        // If using Firebase, pass token to Firebase Messaging
        #if FIREBASE_ENABLED
        Messaging.messaging().apnsToken = deviceToken
        #endif
    }

    func application(
        _ application: UIApplication,
        didFailToRegisterForRemoteNotificationsWithError error: Error
    ) {
        Task { @MainActor in
            PushNotificationService.shared.didFailToRegisterForRemoteNotifications(error: error)
        }
    }

    func application(
        _ application: UIApplication,
        didReceiveRemoteNotification userInfo: [AnyHashable: Any],
        fetchCompletionHandler completionHandler: @escaping (UIBackgroundFetchResult) -> Void
    ) {
        // Handle silent/background notification
        print("AppDelegate: Received remote notification: \(userInfo)")

        // Post notification for handling
        NotificationCenter.default.post(
            name: .didReceiveRemoteNotification,
            object: nil,
            userInfo: userInfo as? [String: Any]
        )

        completionHandler(.newData)
    }

    // MARK: - Firebase Configuration

    private func configureFirebase() {
        #if FIREBASE_ENABLED
        // Only configure if GoogleService-Info.plist exists
        if Bundle.main.path(forResource: "GoogleService-Info", ofType: "plist") != nil {
            FirebaseApp.configure()

            // Set messaging delegate
            Messaging.messaging().delegate = self

            print("AppDelegate: Firebase configured successfully")
        } else {
            print("AppDelegate: GoogleService-Info.plist not found, Firebase not configured")
        }
        #else
        print("AppDelegate: Firebase not enabled, using APNs directly")
        #endif
    }

    // MARK: - Launch Notification Handling

    private func handleLaunchNotification(_ notification: [String: AnyObject]) {
        print("AppDelegate: App launched from notification: \(notification)")

        // Delay posting to ensure app is ready
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) {
            NotificationCenter.default.post(
                name: .didTapPushNotification,
                object: nil,
                userInfo: notification
            )
        }
    }
}

// MARK: - UNUserNotificationCenterDelegate

extension AppDelegate: UNUserNotificationCenterDelegate {

    /// Handle notification when app is in foreground
    func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        willPresent notification: UNNotification,
        withCompletionHandler completionHandler: @escaping (UNNotificationPresentationOptions) -> Void
    ) {
        Task { @MainActor in
            let options = await PushNotificationService.shared.handleForegroundNotification(notification)
            completionHandler(options)
        }
    }

    /// Handle notification tap
    func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        didReceive response: UNNotificationResponse,
        withCompletionHandler completionHandler: @escaping () -> Void
    ) {
        Task { @MainActor in
            await PushNotificationService.shared.handleNotificationTap(response)
            completionHandler()
        }
    }
}

// MARK: - Firebase Messaging Delegate (Conditional)

#if FIREBASE_ENABLED
import FirebaseCore
import FirebaseMessaging

extension AppDelegate: MessagingDelegate {

    func messaging(_ messaging: Messaging, didReceiveRegistrationToken fcmToken: String?) {
        guard let token = fcmToken else { return }

        print("AppDelegate: FCM token received: \(token)")

        Task { @MainActor in
            PushNotificationService.shared.setFCMToken(token)
        }

        // Post notification for other parts of the app
        NotificationCenter.default.post(
            name: .fcmTokenReceived,
            object: nil,
            userInfo: ["token": token]
        )
    }
}

extension Notification.Name {
    static let fcmTokenReceived = Notification.Name("fcmTokenReceived")
}
#endif
