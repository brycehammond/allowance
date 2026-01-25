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
        }
    }
}
