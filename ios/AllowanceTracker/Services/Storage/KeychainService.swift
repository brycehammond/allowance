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
    private let account = "authToken"

    // MARK: - Public Methods

    /// Save JWT token to Keychain
    /// - Parameter token: JWT token string
    /// - Throws: KeychainError if unable to save
    func saveToken(_ token: String) throws {
        guard let data = token.data(using: .utf8) else {
            throw KeychainError.invalidData
        }

        // Delete existing token first (if any)
        try? deleteToken()

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

    /// Retrieve JWT token from Keychain
    /// - Returns: JWT token string
    /// - Throws: KeychainError if unable to retrieve or not found
    func getToken() throws -> String {
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
              let token = String(data: data, encoding: .utf8) else {
            throw KeychainError.invalidData
        }

        return token
    }

    /// Delete JWT token from Keychain
    /// - Throws: KeychainError if unable to delete (but not if item doesn't exist)
    func deleteToken() throws {
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
