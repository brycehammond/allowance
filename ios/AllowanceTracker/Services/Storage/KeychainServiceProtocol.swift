import Foundation

/// Protocol for KeychainService to enable dependency injection and testing
protocol KeychainServiceProtocol {
    func saveToken(_ token: String) throws
    func getToken() throws -> String
    func deleteToken() throws
}
