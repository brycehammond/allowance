import Foundation

struct User: Codable, Identifiable {
    let id: UUID
    let email: String
    let firstName: String
    let lastName: String
    let role: UserRole
    let familyId: UUID?

    var fullName: String {
        "\(firstName) \(lastName)"
    }

    var isParent: Bool {
        role == .parent
    }

    enum CodingKeys: String, CodingKey {
        case id = "userId"
        case email
        case firstName
        case lastName
        case role
        case familyId
    }
}

enum UserRole: String, Codable {
    case parent = "Parent"
    case child = "Child"
}

// MARK: - Auth DTOs
struct LoginRequest: Codable {
    let email: String
    let password: String
    let rememberMe: Bool = false
}

struct RegisterRequest: Codable {
    let email: String
    let password: String
    let firstName: String
    let lastName: String
    let role: UserRole
}

struct AuthResponse: Codable {
    let userId: UUID
    let email: String
    let firstName: String
    let lastName: String
    let role: String
    let familyId: UUID?
    let familyName: String?
    let token: String
    let expiresAt: Date

    var user: User {
        User(
            id: userId,
            email: email,
            firstName: firstName,
            lastName: lastName,
            role: UserRole(rawValue: role) ?? .child,
            familyId: familyId
        )
    }
}
