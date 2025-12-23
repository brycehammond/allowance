import Foundation

/// Application-wide constants
enum AppConstants {

    // MARK: - API Configuration

    enum API {
        /// Base URL for production API
        static let baseURL = "https://api.allowancetracker.com"

        /// API version
        static let version = "v1"

        /// Full API endpoint
        static var endpoint: String {
            "\(baseURL)/api/\(version)"
        }

        /// Request timeout interval in seconds
        static let timeoutInterval: TimeInterval = 30
    }

    // MARK: - Validation

    enum Validation {
        /// Minimum password length
        static let minimumPasswordLength = 6

        /// Maximum password length
        static let maximumPasswordLength = 128

        /// Email regex pattern
        static let emailPattern = "[A-Z0-9a-z._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,64}"
    }

    // MARK: - UI

    enum UI {
        /// Standard corner radius
        static let cornerRadius: CGFloat = 12

        /// Standard padding
        static let padding: CGFloat = 16

        /// Large padding
        static let largePadding: CGFloat = 24

        /// Small padding
        static let smallPadding: CGFloat = 8

        /// Animation duration
        static let animationDuration: Double = 0.3
    }

    // MARK: - Cache

    enum Cache {
        /// Cache expiration time in seconds (15 minutes)
        static let expirationTime: TimeInterval = 900

        /// Maximum cache size in items
        static let maxSize = 100
    }

    // MARK: - Keychain

    enum Keychain {
        /// Service identifier for keychain
        static let service = "com.allowancetracker.app"

        /// Account identifier for auth token
        static let tokenAccount = "authToken"
    }

    // MARK: - User Defaults

    enum UserDefaults {
        /// Key for storing last sync timestamp
        static let lastSyncKey = "lastSyncTimestamp"

        /// Key for storing user preferences
        static let userPreferencesKey = "userPreferences"

        /// Key for app version
        static let appVersionKey = "appVersion"
    }

    // MARK: - Feature Flags

    enum Features {
        /// Enable offline mode
        static let offlineMode = true

        /// Enable push notifications
        static let pushNotifications = false

        /// Enable analytics
        static let analytics = false

        /// Enable SignalR real-time updates
        static let realTimeUpdates = false
    }

    // MARK: - App Info

    enum App {
        /// App name
        static let name = "Earn & Learn"

        /// App version from bundle
        static var version: String {
            Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "1.0.0"
        }

        /// Build number from bundle
        static var build: String {
            Bundle.main.infoDictionary?["CFBundleVersion"] as? String ?? "1"
        }

        /// Full version string
        static var fullVersion: String {
            "\(version) (\(build))"
        }
    }
}
