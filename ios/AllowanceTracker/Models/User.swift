import Foundation

// MARK: - User Model

struct User: Codable, Identifiable, Equatable {
    let id: UUID
    let email: String
    let firstName: String
    let lastName: String
    let role: UserRole
    let familyId: UUID?

    var fullName: String {
        "\(firstName) \(lastName)"
    }
}

// MARK: - UserRole Enum

enum UserRole: String, Codable {
    case parent = "Parent"
    case child = "Child"
}

// MARK: - Auth Request/Response Models

struct LoginRequest: Codable {
    let email: String
    let password: String
}

struct RegisterRequest: Codable {
    let email: String
    let password: String
    let firstName: String
    let lastName: String
    let role: UserRole
}

struct AuthResponse: Codable {
    let token: String
    let expiresAt: Date
    let user: User
}
