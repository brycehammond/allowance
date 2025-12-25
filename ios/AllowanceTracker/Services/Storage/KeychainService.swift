import Foundation
import Security

// MARK: - Keychain Error

enum KeychainError: Error, Equatable {
    case notFound
    case unableToSave
    case unableToDelete
    case unableToRetrieve
    case invalidData
}

// MARK: - Keychain Service

final class KeychainService: KeychainServiceProtocol {

    // MARK: - Singleton

    static let shared = KeychainService()

    private init() {}

    // MARK: - Constants

    private let service = "com.allowancetracker.app"
    private let tokenAccount = "authToken"
    private let expirationAccount = "tokenExpiration"
    private let biometricAccount = "biometricEnabled"

    // MARK: - Token Management

    /// Save JWT token to Keychain
    /// - Parameter token: JWT token string
    /// - Throws: KeychainError if unable to save
    func saveToken(_ token: String) throws {
        try saveString(token, account: tokenAccount)
    }

    /// Retrieve JWT token from Keychain
    /// - Returns: JWT token string
    /// - Throws: KeychainError if unable to retrieve or not found
    func getToken() throws -> String {
        try getString(account: tokenAccount)
    }

    /// Delete JWT token from Keychain
    /// - Throws: KeychainError if unable to delete (but not if item doesn't exist)
    func deleteToken() throws {
        try deleteItem(account: tokenAccount)
    }

    // MARK: - Token Expiration

    /// Save token expiration date to Keychain
    /// - Parameter date: Token expiration date
    /// - Throws: KeychainError if unable to save
    func saveTokenExpiration(_ date: Date) throws {
        let timestamp = String(date.timeIntervalSince1970)
        try saveString(timestamp, account: expirationAccount)
    }

    /// Retrieve token expiration date from Keychain
    /// - Returns: Token expiration date
    /// - Throws: KeychainError if unable to retrieve or not found
    func getTokenExpiration() throws -> Date {
        let timestamp = try getString(account: expirationAccount)
        guard let interval = TimeInterval(timestamp) else {
            throw KeychainError.invalidData
        }
        return Date(timeIntervalSince1970: interval)
    }

    /// Delete token expiration from Keychain
    /// - Throws: KeychainError if unable to delete
    func deleteTokenExpiration() throws {
        try deleteItem(account: expirationAccount)
    }

    // MARK: - Biometric Settings

    /// Save biometric enabled setting to Keychain
    /// - Parameter enabled: Whether biometric authentication is enabled
    /// - Throws: KeychainError if unable to save
    func saveBiometricEnabled(_ enabled: Bool) throws {
        try saveString(enabled ? "true" : "false", account: biometricAccount)
    }

    /// Check if biometric authentication is enabled
    /// - Returns: True if biometric authentication is enabled, false otherwise
    func isBiometricEnabled() -> Bool {
        guard let value = try? getString(account: biometricAccount) else {
            return false
        }
        return value == "true"
    }

    /// Delete biometric setting from Keychain
    /// - Throws: KeychainError if unable to delete
    func deleteBiometricEnabled() throws {
        try deleteItem(account: biometricAccount)
    }

    // MARK: - Convenience Methods

    /// Check if a valid token exists in the Keychain
    /// - Returns: True if token exists, false otherwise
    func hasValidToken() -> Bool {
        guard (try? getToken()) != nil else {
            return false
        }
        // Check if token is expired
        guard let expiration = try? getTokenExpiration() else {
            // If no expiration stored, assume token is valid
            return true
        }
        return expiration > Date()
    }

    /// Check if token is expiring soon
    /// - Parameter minutes: Number of minutes to check
    /// - Returns: True if token expires within the specified minutes
    func isTokenExpiringSoon(withinMinutes minutes: Int) -> Bool {
        guard let expiration = try? getTokenExpiration() else {
            return false
        }
        let threshold = Date().addingTimeInterval(TimeInterval(minutes * 60))
        return expiration <= threshold
    }

    /// Clear all authentication data from Keychain
    /// - Throws: KeychainError if unable to delete
    func clearAllAuthData() throws {
        try? deleteToken()
        try? deleteTokenExpiration()
        // Note: We don't delete biometric setting - that's a user preference
    }

    // MARK: - Private Helpers

    private func saveString(_ value: String, account: String) throws {
        guard let data = value.data(using: .utf8) else {
            throw KeychainError.invalidData
        }

        // Delete existing item first (if any)
        try? deleteItem(account: account)

        // Create query dictionary for saving
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
            kSecValueData as String: data,
            kSecAttrAccessible as String: kSecAttrAccessibleWhenUnlockedThisDeviceOnly
        ]

        // Add item to keychain
        let status = SecItemAdd(query as CFDictionary, nil)

        guard status == errSecSuccess else {
            throw KeychainError.unableToSave
        }
    }

    private func getString(account: String) throws -> String {
        // Create query dictionary for retrieval
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
            kSecReturnData as String: true,
            kSecMatchLimit as String: kSecMatchLimitOne
        ]

        // Retrieve item from keychain
        var result: AnyObject?
        let status = SecItemCopyMatching(query as CFDictionary, &result)

        guard status == errSecSuccess else {
            throw status == errSecItemNotFound ? KeychainError.notFound : KeychainError.unableToRetrieve
        }

        guard let data = result as? Data,
              let value = String(data: data, encoding: .utf8) else {
            throw KeychainError.invalidData
        }

        return value
    }

    private func deleteItem(account: String) throws {
        // Create query dictionary for deletion
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account
        ]

        // Delete item from keychain
        let status = SecItemDelete(query as CFDictionary)

        // Success or item not found are both acceptable outcomes
        guard status == errSecSuccess || status == errSecItemNotFound else {
            throw KeychainError.unableToDelete
        }
    }
}
