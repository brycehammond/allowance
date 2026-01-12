import SwiftUI

@main
@MainActor
struct AllowanceTrackerApp: App {

    // MARK: - Properties

    @UIApplicationDelegateAdaptor(AppDelegate.self) var appDelegate
    @State private var authViewModel = AuthViewModel()

    // MARK: - Initialization

    init() {
        // Register background refresh tasks
        BackgroundRefreshManager.shared.registerBackgroundTasks()

        // Schedule initial background refresh
        BackgroundRefreshManager.shared.scheduleAppRefresh()

        #if DEBUG
        Configuration.printConfiguration()
        #endif
    }

    // MARK: - Body

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environment(authViewModel)
                .task {
                    // Check notification authorization on launch
                    await PushNotificationService.shared.checkAuthorizationStatus()
                }
                .onReceive(NotificationCenter.default.publisher(for: .didTapPushNotification)) { notification in
                    handlePushNotificationTap(notification.userInfo)
                }
        }
    }

    // MARK: - Private Methods

    private func handlePushNotificationTap(_ userInfo: [AnyHashable: Any]?) {
        guard let userInfo = userInfo else { return }

        // Extract notification data for navigation
        if let entityType = userInfo["relatedEntityType"] as? String,
           let entityId = userInfo["relatedEntityId"] as? String {
            print("AllowanceTrackerApp: Navigate to \(entityType) with id \(entityId)")
            // Navigation handling can be implemented here based on entity type
            // For example: child profile, transaction, goal, etc.
        }
    }
}
