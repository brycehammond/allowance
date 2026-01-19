import SwiftUI

@main
@MainActor
struct AllowanceTrackerApp: App {

    // MARK: - Properties

    @UIApplicationDelegateAdaptor(AppDelegate.self) var appDelegate
    @State private var authViewModel: AuthViewModel

    // MARK: - Initialization

    init() {
        // Create auth view model with appropriate services based on environment
        if UITestEnvironment.isUITesting {
            if Configuration.isUITestingWithRealAPI {
                // UI testing with real API - use real services but with test API URL
                // The TEST_API_BASE_URL environment variable overrides the API URL
                _authViewModel = State(initialValue: AuthViewModel())
                #if DEBUG
                print("UI TEST MODE: Using REAL API at \(Configuration.apiBaseURL)")
                #endif
            } else {
                // Legacy UI testing mode - use mock services
                _authViewModel = State(initialValue: AuthViewModel(
                    apiService: MockAPIService(),
                    keychainService: MockKeychainService(),
                    biometricService: BiometricService.shared
                ))
                #if DEBUG
                print("UI TEST MODE: Using mock services")
                #endif
            }
        } else {
            // Use real services for normal operation
            _authViewModel = State(initialValue: AuthViewModel())

            // Register background refresh tasks
            BackgroundRefreshManager.shared.registerBackgroundTasks()

            // Schedule initial background refresh
            BackgroundRefreshManager.shared.scheduleAppRefresh()
        }

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
