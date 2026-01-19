import Foundation

/// Mock keychain service for UI testing - stores data in memory
final class MockKeychainService: KeychainServiceProtocol {

    // MARK: - In-Memory Storage

    private var token: String?
    private var tokenExpiration: Date?
    private var biometricEnabled: Bool = false

    // MARK: - Token Management

    func saveToken(_ token: String) throws {
        self.token = token
    }

    func getToken() throws -> String {
        guard let token = token else {
            throw KeychainError.notFound
        }
        return token
    }

    func deleteToken() throws {
        token = nil
    }

    func hasValidToken() -> Bool {
        guard token != nil else { return false }
        if let expiration = tokenExpiration {
            return expiration > Date()
        }
        return true
    }

    // MARK: - Token Expiration

    func saveTokenExpiration(_ date: Date) throws {
        tokenExpiration = date
    }

    func getTokenExpiration() throws -> Date {
        guard let expiration = tokenExpiration else {
            throw KeychainError.notFound
        }
        return expiration
    }

    func deleteTokenExpiration() throws {
        tokenExpiration = nil
    }

    func isTokenExpiringSoon(withinMinutes minutes: Int) -> Bool {
        guard let expiration = tokenExpiration else { return false }
        let threshold = Date().addingTimeInterval(Double(minutes) * 60)
        return expiration < threshold
    }

    // MARK: - Biometric Settings

    func saveBiometricEnabled(_ enabled: Bool) throws {
        biometricEnabled = enabled
    }

    func isBiometricEnabled() -> Bool {
        return biometricEnabled
    }

    func deleteBiometricEnabled() throws {
        biometricEnabled = false
    }

    // MARK: - Clear All

    func clearAllAuthData() throws {
        token = nil
        tokenExpiration = nil
    }
}
