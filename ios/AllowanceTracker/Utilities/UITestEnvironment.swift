import Foundation

/// Utility to detect UI test environment
enum UITestEnvironment {

    /// Check if app is running in UI test mode
    static var isUITesting: Bool {
        CommandLine.arguments.contains("--uitesting") ||
        ProcessInfo.processInfo.environment["UITEST_MODE"] == "1"
    }
}
