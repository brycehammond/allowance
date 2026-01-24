import Foundation

/// Provides the correct service instances based on the current environment.
/// In UI test mode (mock), returns mock services. Otherwise, returns real services.
enum ServiceProvider {

    // MARK: - API Service

    /// Returns the appropriate API service for the current environment
    static var apiService: APIServiceProtocol {
        if UITestEnvironment.isUITesting && !Configuration.isUITestingWithRealAPI {
            return MockAPIService.shared
        }
        return APIService()
    }

    /// Returns a shared API service instance (for ViewModels that need consistent state)
    static var sharedAPIService: APIServiceProtocol {
        if UITestEnvironment.isUITesting && !Configuration.isUITestingWithRealAPI {
            return MockAPIService.shared
        }
        return sharedRealAPIService
    }

    /// Shared real API service instance
    private static let sharedRealAPIService = APIService()

    // MARK: - Keychain Service

    /// Returns the appropriate keychain service for the current environment
    static var keychainService: KeychainServiceProtocol {
        if UITestEnvironment.isUITesting && !Configuration.isUITestingWithRealAPI {
            return sharedMockKeychainService
        }
        return KeychainService.shared
    }

    /// Shared mock keychain service instance for test consistency
    private static let sharedMockKeychainService = MockKeychainService()

    // MARK: - Test Mode Helpers

    /// Whether we're running in mock mode (UI tests without real API)
    static var isMockMode: Bool {
        UITestEnvironment.isUITesting && !Configuration.isUITestingWithRealAPI
    }
}
