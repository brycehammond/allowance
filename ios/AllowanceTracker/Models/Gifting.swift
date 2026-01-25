import Foundation

// MARK: - Enums

enum GiftLinkVisibility: String, Codable, CaseIterable {
    case Minimal
    case WithGoals
    // Note: WithWishList is kept for backward compatibility but behaves same as Minimal
    case WithWishList
    case Full

    var displayName: String {
        switch self {
        case .Minimal, .WithWishList: return "Minimal"
        case .WithGoals: return "With Goals"
        case .Full: return "Full"
        }
    }

    var description: String {
        switch self {
        case .Minimal, .WithWishList: return "Just child's name"
        case .WithGoals: return "Show savings goals"
        case .Full: return "Show all savings goals"
        }
    }

    var systemImage: String {
        switch self {
        case .Minimal, .WithWishList: return "eye.slash"
        case .WithGoals: return "target"
        case .Full: return "eye"
        }
    }
}

enum GiftOccasion: String, Codable, CaseIterable {
    case Birthday
    case Christmas
    case Hanukkah
    case Easter
    case Graduation
    case GoodGrades
    case Holiday
    case JustBecause
    case Reward
    case Other

    var displayName: String {
        switch self {
        case .Birthday: return "Birthday"
        case .Christmas: return "Christmas"
        case .Hanukkah: return "Hanukkah"
        case .Easter: return "Easter"
        case .Graduation: return "Graduation"
        case .GoodGrades: return "Good Grades"
        case .Holiday: return "Holiday"
        case .JustBecause: return "Just Because"
        case .Reward: return "Reward"
        case .Other: return "Other"
        }
    }

    var emoji: String {
        switch self {
        case .Birthday: return "🎂"
        case .Christmas: return "🎄"
        case .Hanukkah: return "🕎"
        case .Easter: return "🐰"
        case .Graduation: return "🎓"
        case .GoodGrades: return "📚"
        case .Holiday: return "🎉"
        case .JustBecause: return "💝"
        case .Reward: return "🏆"
        case .Other: return "🎁"
        }
    }

    var systemImage: String {
        switch self {
        case .Birthday: return "birthday.cake"
        case .Christmas: return "snowflake"
        case .Hanukkah: return "flame"
        case .Easter: return "hare"
        case .Graduation: return "graduationcap"
        case .GoodGrades: return "book"
        case .Holiday: return "party.popper"
        case .JustBecause: return "heart"
        case .Reward: return "trophy"
        case .Other: return "gift"
        }
    }
}

enum GiftStatus: String, Codable, CaseIterable {
    case Pending
    case Approved
    case Rejected
    case Expired

    var displayName: String {
        rawValue
    }

    var systemImage: String {
        switch self {
        case .Pending: return "clock"
        case .Approved: return "checkmark.circle.fill"
        case .Rejected: return "xmark.circle.fill"
        case .Expired: return "clock.badge.xmark"
        }
    }

    var color: String {
        switch self {
        case .Pending: return "yellow"
        case .Approved: return "green"
        case .Rejected: return "red"
        case .Expired: return "gray"
        }
    }
}

// MARK: - Gift Link DTO

struct GiftLinkDto: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let childFirstName: String
    let token: String
    let name: String
    let description: String?
    let visibility: GiftLinkVisibility
    let isActive: Bool
    let expiresAt: Date?
    let maxUses: Int?
    let useCount: Int
    let minAmount: Decimal?
    let maxAmount: Decimal?
    let allowedOccasions: [GiftOccasion]?
    let defaultOccasion: GiftOccasion?
    let createdAt: Date
    let updatedAt: Date?
    let portalUrl: String

    var isExpired: Bool {
        if let expiresAt = expiresAt {
            return expiresAt < Date()
        }
        return false
    }

    var hasReachedMaxUses: Bool {
        if let maxUses = maxUses {
            return useCount >= maxUses
        }
        return false
    }

    var formattedMinAmount: String? {
        minAmount?.currencyFormatted
    }

    var formattedMaxAmount: String? {
        maxAmount?.currencyFormatted
    }

    var statusDescription: String {
        if !isActive {
            return "Deactivated"
        }
        if isExpired {
            return "Expired"
        }
        if hasReachedMaxUses {
            return "Max uses reached"
        }
        return "Active"
    }
}

// MARK: - Gift Link Stats DTO

struct GiftLinkStatsDto: Codable {
    let giftLinkId: UUID
    let totalGifts: Int
    let pendingGifts: Int
    let approvedGifts: Int
    let rejectedGifts: Int
    let expiredGifts: Int
    let totalAmountReceived: Decimal
    let averageGiftAmount: Decimal?

    var formattedTotalAmount: String {
        totalAmountReceived.currencyFormatted
    }

    var formattedAverageAmount: String? {
        averageGiftAmount?.currencyFormatted
    }
}

// MARK: - Gift DTO

struct GiftDto: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let childFirstName: String
    let giftLinkId: UUID
    let giverName: String
    let giverEmail: String?
    let giverRelationship: String?
    let amount: Decimal
    let occasion: GiftOccasion
    let customOccasion: String?
    let message: String?
    let status: GiftStatus
    let rejectionReason: String?
    let allocatedToGoalId: UUID?
    let allocatedToGoalName: String?
    let savingsPercentage: Int?
    let hasThankYouNote: Bool
    let createdAt: Date
    let processedAt: Date?
    let processedByName: String?

    var formattedAmount: String {
        amount.currencyFormatted
    }

    var occasionDisplay: String {
        if let customOccasion = customOccasion, !customOccasion.isEmpty {
            return customOccasion
        }
        return occasion.displayName
    }

    var formattedDate: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        return formatter.string(from: createdAt)
    }

    var daysSinceReceived: Int {
        Calendar.current.dateComponents([.day], from: createdAt, to: Date()).day ?? 0
    }
}

// MARK: - Pending Thank You DTO

struct PendingThankYouDto: Codable, Identifiable {
    let giftId: UUID
    let childId: UUID
    let giverName: String
    let giverEmail: String?
    let giverRelationship: String?
    let amount: Decimal
    let occasion: GiftOccasion
    let customOccasion: String?
    let receivedAt: Date
    let daysSinceReceived: Int
    let hasNote: Bool

    var id: UUID { giftId }

    var formattedAmount: String {
        amount.currencyFormatted
    }

    var occasionDisplay: String {
        if let customOccasion = customOccasion, !customOccasion.isEmpty {
            return customOccasion
        }
        return occasion.displayName
    }

    var formattedDate: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        return formatter.string(from: receivedAt)
    }
}

// MARK: - Thank You Note DTO

struct ThankYouNoteDto: Codable, Identifiable {
    let id: UUID
    let giftId: UUID
    let childId: UUID
    let childFirstName: String
    let giverName: String
    let giverEmail: String?
    let message: String
    let imageUrl: String?
    let isSent: Bool
    let sentAt: Date?
    let createdAt: Date
    let updatedAt: Date?

    var formattedSentDate: String? {
        guard let sentAt = sentAt else { return nil }
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        formatter.timeStyle = .short
        return formatter.string(from: sentAt)
    }
}

// MARK: - Gift Portal Data DTO

struct GiftPortalDataDto: Codable {
    let childFirstName: String
    let linkName: String
    let linkDescription: String?
    let visibility: GiftLinkVisibility
    let minAmount: Decimal?
    let maxAmount: Decimal?
    let allowedOccasions: [GiftOccasion]?
    let defaultOccasion: GiftOccasion?
    let savingsGoals: [PortalSavingsGoalDto]?
}

struct PortalSavingsGoalDto: Codable, Identifiable {
    let id: UUID
    let name: String
    let targetAmount: Decimal
    let currentAmount: Decimal
    let progressPercentage: Double

    var formattedTargetAmount: String {
        targetAmount.currencyFormatted
    }

    var formattedCurrentAmount: String {
        currentAmount.currencyFormatted
    }

    var progressFraction: Double {
        min(progressPercentage / 100.0, 1.0)
    }
}

// MARK: - Gift Submission Result DTO

struct GiftSubmissionResultDto: Codable {
    let giftId: UUID
    let childFirstName: String
    let amount: Decimal
    let message: String

    var formattedAmount: String {
        amount.currencyFormatted
    }
}

// MARK: - Request DTOs

struct CreateGiftLinkRequest: Codable {
    let childId: UUID
    let name: String
    let description: String?
    let visibility: GiftLinkVisibility
    let expiresAt: Date?
    let maxUses: Int?
    let minAmount: Decimal?
    let maxAmount: Decimal?
    let allowedOccasions: [GiftOccasion]?
    let defaultOccasion: GiftOccasion?

    init(
        childId: UUID,
        name: String,
        description: String? = nil,
        visibility: GiftLinkVisibility = .Minimal,
        expiresAt: Date? = nil,
        maxUses: Int? = nil,
        minAmount: Decimal? = nil,
        maxAmount: Decimal? = nil,
        allowedOccasions: [GiftOccasion]? = nil,
        defaultOccasion: GiftOccasion? = nil
    ) {
        self.childId = childId
        self.name = name
        self.description = description
        self.visibility = visibility
        self.expiresAt = expiresAt
        self.maxUses = maxUses
        self.minAmount = minAmount
        self.maxAmount = maxAmount
        self.allowedOccasions = allowedOccasions
        self.defaultOccasion = defaultOccasion
    }
}

struct UpdateGiftLinkRequest: Codable {
    let name: String?
    let description: String?
    let visibility: GiftLinkVisibility?
    let isActive: Bool?
    let expiresAt: Date?
    let maxUses: Int?
    let minAmount: Decimal?
    let maxAmount: Decimal?
    let allowedOccasions: [GiftOccasion]?
    let defaultOccasion: GiftOccasion?
}

struct SubmitGiftRequest: Codable {
    let giverName: String
    let giverEmail: String?
    let giverRelationship: String?
    let amount: Decimal
    let occasion: GiftOccasion
    let customOccasion: String?
    let message: String?
}

struct ApproveGiftRequest: Codable {
    let allocateToGoalId: UUID?
    let savingsPercentage: Int?

    init(allocateToGoalId: UUID? = nil, savingsPercentage: Int? = nil) {
        self.allocateToGoalId = allocateToGoalId
        self.savingsPercentage = savingsPercentage
    }
}

struct RejectGiftRequest: Codable {
    let reason: String?

    init(reason: String? = nil) {
        self.reason = reason
    }
}

struct CreateThankYouNoteRequest: Codable {
    let message: String
    let imageUrl: String?

    init(message: String, imageUrl: String? = nil) {
        self.message = message
        self.imageUrl = imageUrl
    }
}

struct UpdateThankYouNoteRequest: Codable {
    let message: String?
    let imageUrl: String?
}
