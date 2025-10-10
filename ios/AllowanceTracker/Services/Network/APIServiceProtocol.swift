import Foundation

/// Protocol for APIService to enable dependency injection and testing
protocol APIServiceProtocol {
    func login(_ request: LoginRequest) async throws -> AuthResponse
    func register(_ request: RegisterRequest) async throws -> AuthResponse
    func logout() async throws
    func getChildren() async throws -> [Child]
    func getChild(id: UUID) async throws -> Child
}
