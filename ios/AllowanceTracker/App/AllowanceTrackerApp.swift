import SwiftUI

@main
@MainActor
struct AllowanceTrackerApp: App {

    // MARK: - Properties

    @UIApplicationDelegateAdaptor(AppDelegate.self) var appDelegate
    @State private var authViewModel: AuthViewModel

    // MARK: - Initialization

    init() {
        // AuthViewModel uses ServiceProvider which automatically selects
        // mock or real services based on the environment
        _authViewModel = State(initialValue: AuthViewModel())

        #if DEBUG
        if ServiceProvider.isMockMode {
            print("UI TEST MODE: Using mock services")
        } else if UITestEnvironment.isUITesting {
            print("UI TEST MODE: Using REAL API at \(Configuration.apiBaseURL)")
        }
        Configuration.printConfiguration()
        #endif

        // Only register background tasks in normal operation (not UI tests)
        if !UITestEnvironment.isUITesting {
            BackgroundRefreshManager.shared.registerBackgroundTasks()
            BackgroundRefreshManager.shared.scheduleAppRefresh()
        }
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
