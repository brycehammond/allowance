import Foundation

// MARK: - Enums

enum BadgeCategory: String, Codable, CaseIterable {
    case Saving
    case Spending
    case Goals
    case Chores
    case Streaks
    case Milestones
    case Special

    var displayName: String {
        rawValue
    }

    var systemImage: String {
        switch self {
        case .Saving: return "banknote"
        case .Spending: return "cart"
        case .Goals: return "target"
        case .Chores: return "checkmark.circle"
        case .Streaks: return "flame"
        case .Milestones: return "flag"
        case .Special: return "sparkles"
        }
    }
}

enum BadgeRarity: String, Codable, CaseIterable {
    case Common
    case Uncommon
    case Rare
    case Epic
    case Legendary

    var displayName: String {
        rawValue
    }

    var sortOrder: Int {
        switch self {
        case .Common: return 1
        case .Uncommon: return 2
        case .Rare: return 3
        case .Epic: return 4
        case .Legendary: return 5
        }
    }
}

// MARK: - Badge DTO

struct BadgeDto: Codable, Identifiable {
    let id: UUID
    let code: String
    let name: String
    let description: String
    let iconUrl: String
    let category: BadgeCategory
    let categoryName: String
    let rarity: BadgeRarity
    let rarityName: String
    let pointsValue: Int
    let isSecret: Bool
    let isEarned: Bool
    let earnedAt: Date?
    let isDisplayed: Bool
    let currentProgress: Int?
    let targetProgress: Int?
    let progressPercentage: Double?
}

// MARK: - Child Badge DTO (Earned Badge)

struct ChildBadgeDto: Codable, Identifiable {
    let id: UUID
    let badgeId: UUID
    let badgeName: String
    let badgeDescription: String
    let iconUrl: String
    let category: BadgeCategory
    let categoryName: String
    let rarity: BadgeRarity
    let rarityName: String
    let pointsValue: Int
    let earnedAt: Date
    let isDisplayed: Bool
    let isNew: Bool
    let earnedContext: String?

    var formattedEarnedDate: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        return formatter.string(from: earnedAt)
    }
}

// MARK: - Badge Progress DTO

struct BadgeProgressDto: Codable, Identifiable {
    var id: UUID { badgeId }
    let badgeId: UUID
    let badgeName: String
    let description: String
    let iconUrl: String
    let category: BadgeCategory
    let categoryName: String
    let rarity: BadgeRarity
    let rarityName: String
    let pointsValue: Int
    let currentProgress: Int
    let targetProgress: Int
    let progressPercentage: Double
    let progressText: String

    var progressFraction: Double {
        min(progressPercentage / 100.0, 1.0)
    }
}

// MARK: - Child Points DTO

struct ChildPointsDto: Codable {
    let totalPoints: Int
    let availablePoints: Int
    let spentPoints: Int
    let badgesEarned: Int
    let rewardsUnlocked: Int
}

// MARK: - Achievement Summary DTO

struct AchievementSummaryDto: Codable {
    let totalBadges: Int
    let earnedBadges: Int
    let totalPoints: Int
    let availablePoints: Int
    let recentBadges: [ChildBadgeDto]
    let inProgressBadges: [BadgeProgressDto]
    let badgesByCategory: [String: Int]
}

// MARK: - Request DTOs

struct UpdateBadgeDisplayRequest: Codable {
    let isDisplayed: Bool
}

struct MarkBadgesSeenRequest: Codable {
    let badgeIds: [UUID]
}
