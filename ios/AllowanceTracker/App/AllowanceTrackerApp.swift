import SwiftUI

@main
struct AllowanceTrackerApp: App {

    // MARK: - Properties

    @StateObject private var authViewModel = AuthViewModel()

    // MARK: - Body

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(authViewModel)
        }
    }
}
