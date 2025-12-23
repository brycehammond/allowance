import Foundation

/// Configuration manager for reading app settings from Info.plist
enum Configuration {

    // MARK: - Keys

    private enum Keys {
        static let apiBaseURL = "API_BASE_URL"
    }

    // MARK: - Public Properties

    /// API base URL from Info.plist
    static var apiBaseURL: URL {
        guard let urlString = infoPlistValue(for: Keys.apiBaseURL) as? String,
              let url = URL(string: urlString) else {
            fatalError("API_BASE_URL not configured in Info.plist")
        }
        return url
    }

    /// App version from Info.plist
    static var appVersion: String {
        infoPlistValue(for: "CFBundleShortVersionString") as? String ?? "Unknown"
    }

    /// Build number from Info.plist
    static var buildNumber: String {
        infoPlistValue(for: "CFBundleVersion") as? String ?? "Unknown"
    }

    /// Full version string (e.g., "1.0.0 (1)")
    static var fullVersionString: String {
        "\(appVersion) (\(buildNumber))"
    }

    /// App display name from Info.plist
    static var appName: String {
        infoPlistValue(for: "CFBundleDisplayName") as? String ?? "Earn & Learn"
    }

    /// Bundle identifier from Info.plist
    static var bundleIdentifier: String {
        Bundle.main.bundleIdentifier ?? "com.allowancetracker.app"
    }

    // MARK: - Private Helpers

    /// Read value from Info.plist
    /// - Parameter key: The key to read
    /// - Returns: The value for the key
    private static func infoPlistValue(for key: String) -> Any? {
        Bundle.main.infoDictionary?[key]
    }
}

// MARK: - Environment Support

extension Configuration {

    /// Environment types
    enum Environment: String {
        case development = "Development"
        case staging = "Staging"
        case production = "Production"

        /// Determine environment from API base URL
        static var current: Environment {
            let urlString = Configuration.apiBaseURL.absoluteString.lowercased()

            if urlString.contains("localhost") || urlString.contains("127.0.0.1") {
                return .development
            } else if urlString.contains("staging") || urlString.contains("dev") {
                return .staging
            } else {
                return .production
            }
        }
    }

    /// Current environment
    static var environment: Environment {
        Environment.current
    }

    /// Is running in development environment
    static var isDevelopment: Bool {
        environment == .development
    }

    /// Is running in staging environment
    static var isStaging: Bool {
        environment == .staging
    }

    /// Is running in production environment
    static var isProduction: Bool {
        environment == .production
    }
}

// MARK: - Debug Helpers

#if DEBUG
extension Configuration {

    /// Print current configuration
    static func printConfiguration() {
        print("""
        ╔══════════════════════════════════════════════════════════╗
        ║                 EARN & LEARN CONFIG                      ║
        ╠══════════════════════════════════════════════════════════╣
        ║ App Name:         \(appName.padding(toLength: 35, withPad: " ", startingAt: 0))║
        ║ Version:          \(fullVersionString.padding(toLength: 35, withPad: " ", startingAt: 0))║
        ║ Bundle ID:        \(bundleIdentifier.padding(toLength: 35, withPad: " ", startingAt: 0))║
        ║ Environment:      \(environment.rawValue.padding(toLength: 35, withPad: " ", startingAt: 0))║
        ║ API Base URL:     \(apiBaseURL.absoluteString.padding(toLength: 35, withPad: " ", startingAt: 0))║
        ╚══════════════════════════════════════════════════════════╝
        """)
    }
}
#endif
