import Foundation

/// Protocol for KeychainService to enable dependency injection and testing
protocol KeychainServiceProtocol {
    // MARK: - Token Management
    func saveToken(_ token: String) throws
    func getToken() throws -> String
    func deleteToken() throws

    // MARK: - Token Expiration
    func saveTokenExpiration(_ date: Date) throws
    func getTokenExpiration() throws -> Date
    func deleteTokenExpiration() throws

    // MARK: - Biometric Settings
    func saveBiometricEnabled(_ enabled: Bool) throws
    func isBiometricEnabled() -> Bool
    func deleteBiometricEnabled() throws

    // MARK: - Convenience
    func hasValidToken() -> Bool
    func isTokenExpiringSoon(withinMinutes minutes: Int) -> Bool
    func clearAllAuthData() throws
}
