import Foundation

// MARK: - Parent Invite Models

/// Request to send a parent invite
struct SendParentInviteRequest: Codable {
    let email: String
    let firstName: String
    let lastName: String
}

/// Response from sending a parent invite
struct ParentInviteResponse: Codable {
    let inviteId: String
    let email: String
    let firstName: String
    let lastName: String
    let isExistingUser: Bool
    let expiresAt: Date
    let message: String
}

/// A pending invite
struct PendingInvite: Codable, Identifiable {
    let id: String
    let email: String
    let firstName: String
    let lastName: String
    let isExistingUser: Bool
    let status: InviteStatus
    let expiresAt: Date
    let createdAt: Date

    var fullName: String {
        "\(firstName) \(lastName)"
    }

    var expirationDisplay: String {
        let now = Date()
        let calendar = Calendar.current
        let components = calendar.dateComponents([.day], from: now, to: expiresAt)

        guard let days = components.day else {
            return "Expired"
        }

        if days <= 0 {
            return "Expired"
        } else if days == 1 {
            return "Expires tomorrow"
        } else {
            return "Expires in \(days) days"
        }
    }
}

/// Invite status
enum InviteStatus: String, Codable {
    case pending = "Pending"
    case accepted = "Accepted"
    case expired = "Expired"
    case cancelled = "Cancelled"
}
