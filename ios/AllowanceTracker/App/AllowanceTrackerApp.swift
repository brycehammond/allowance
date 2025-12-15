import SwiftUI

@main
struct AllowanceTrackerApp: App {

    // MARK: - Properties

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
        }
    }
}
