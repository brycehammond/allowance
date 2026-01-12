import Foundation

// MARK: - Enums

enum GoalStatus: String, Codable, CaseIterable {
    case Active
    case Completed
    case Purchased
    case Cancelled
    case Paused

    var displayName: String {
        rawValue
    }

    var systemImage: String {
        switch self {
        case .Active: return "target"
        case .Completed: return "checkmark.circle.fill"
        case .Purchased: return "bag.fill"
        case .Cancelled: return "xmark.circle"
        case .Paused: return "pause.circle"
        }
    }
}

enum GoalCategory: String, Codable, CaseIterable {
    case Toy
    case Game
    case Electronics
    case Clothing
    case Experience
    case Savings
    case Charity
    case Other

    var displayName: String {
        rawValue
    }

    var emoji: String {
        switch self {
        case .Toy: return "🧸"
        case .Game: return "🎮"
        case .Electronics: return "📱"
        case .Clothing: return "👕"
        case .Experience: return "🎢"
        case .Savings: return "🏦"
        case .Charity: return "💝"
        case .Other: return "🎯"
        }
    }

    var systemImage: String {
        switch self {
        case .Toy: return "teddybear"
        case .Game: return "gamecontroller"
        case .Electronics: return "iphone"
        case .Clothing: return "tshirt"
        case .Experience: return "ticket"
        case .Savings: return "building.columns"
        case .Charity: return "heart.circle"
        case .Other: return "star"
        }
    }
}

enum ContributionType: String, Codable, CaseIterable {
    case ChildDeposit
    case AutoTransfer
    case ParentMatch
    case ParentGift
    case ChallengeBonus
    case Withdrawal
    case ExternalGift

    var displayName: String {
        switch self {
        case .ChildDeposit: return "Deposit"
        case .AutoTransfer: return "Auto Transfer"
        case .ParentMatch: return "Parent Match"
        case .ParentGift: return "Parent Gift"
        case .ChallengeBonus: return "Challenge Bonus"
        case .Withdrawal: return "Withdrawal"
        case .ExternalGift: return "Gift"
        }
    }

    var systemImage: String {
        switch self {
        case .ChildDeposit: return "plus.circle"
        case .AutoTransfer: return "arrow.right.circle"
        case .ParentMatch: return "arrow.2.squarepath"
        case .ParentGift: return "gift"
        case .ChallengeBonus: return "trophy"
        case .Withdrawal: return "minus.circle"
        case .ExternalGift: return "gift.fill"
        }
    }

    var isPositive: Bool {
        self != .Withdrawal
    }
}

enum MatchingType: String, Codable, CaseIterable {
    case RatioMatch
    case PercentageMatch
    case MilestoneBonus

    var displayName: String {
        switch self {
        case .RatioMatch: return "Ratio Match"
        case .PercentageMatch: return "Percentage Match"
        case .MilestoneBonus: return "Milestone Bonus"
        }
    }
}

enum ChallengeStatus: String, Codable, CaseIterable {
    case Active
    case Completed
    case Failed
    case Cancelled

    var displayName: String {
        rawValue
    }
}

enum AutoTransferType: String, Codable, CaseIterable {
    case None
    case FixedAmount
    case Percentage

    var displayName: String {
        switch self {
        case .None: return "None"
        case .FixedAmount: return "Fixed Amount"
        case .Percentage: return "Percentage"
        }
    }
}

// MARK: - Savings Goal DTO

struct SavingsGoalDto: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let name: String
    let description: String?
    let targetAmount: Decimal
    let currentAmount: Decimal
    let category: GoalCategory
    let categoryName: String
    let status: GoalStatus
    let statusName: String
    let imageUrl: String?
    let autoTransferType: AutoTransferType
    let autoTransferAmount: Decimal?
    let autoTransferPercentage: Decimal?
    let priority: Int
    let progressPercentage: Double
    let amountRemaining: Decimal
    let isCompleted: Bool
    let createdAt: Date
    let completedAt: Date?
    let purchasedAt: Date?
    let milestones: [GoalMilestoneDto]
    let hasActiveChallenge: Bool
    let hasMatchingRule: Bool

    var progressFraction: Double {
        min(progressPercentage / 100.0, 1.0)
    }

    var formattedTargetAmount: String {
        targetAmount.currencyFormatted
    }

    var formattedCurrentAmount: String {
        currentAmount.currencyFormatted
    }

    var formattedAmountRemaining: String {
        amountRemaining.currencyFormatted
    }
}

// MARK: - Goal Milestone DTO

struct GoalMilestoneDto: Codable, Identifiable {
    let id: UUID
    let percentComplete: Int
    let isAchieved: Bool
    let achievedAt: Date?
    let bonusAmount: Decimal?
}

// MARK: - Goal Contribution DTO

struct GoalContributionDto: Codable, Identifiable {
    let id: UUID
    let goalId: UUID
    let childId: UUID
    let amount: Decimal
    let type: ContributionType
    let typeName: String
    let description: String?
    let goalBalanceAfter: Decimal
    let parentMatchId: UUID?
    let createdAt: Date
    let createdById: UUID
    let createdByName: String

    var formattedAmount: String {
        let prefix = type.isPositive ? "+" : "-"
        return "\(prefix)\(abs(amount).currencyFormatted)"
    }

    var formattedDate: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        formatter.timeStyle = .short
        return formatter.string(from: createdAt)
    }
}

// MARK: - Matching Rule DTO

struct MatchingRuleDto: Codable, Identifiable {
    let id: UUID
    let goalId: UUID
    let matchType: MatchingType
    let matchTypeName: String
    let matchRatio: Decimal
    let maxMatchAmount: Decimal?
    let totalMatchedAmount: Decimal
    let isActive: Bool
    let createdAt: Date

    var matchDescription: String {
        switch matchType {
        case .RatioMatch:
            return "$\(matchRatio) for every $1"
        case .PercentageMatch:
            return "\(Int(Double(truncating: matchRatio as NSNumber) * 100))% match"
        case .MilestoneBonus:
            return "Milestone bonus"
        }
    }
}

// MARK: - Goal Challenge DTO

struct GoalChallengeDto: Codable, Identifiable {
    let id: UUID
    let goalId: UUID
    let targetAmount: Decimal
    let currentAmount: Decimal
    let bonusAmount: Decimal
    let startDate: Date
    let endDate: Date
    let status: ChallengeStatus
    let statusName: String
    let progressPercentage: Double
    let daysRemaining: Int
    let isExpired: Bool
    let completedAt: Date?

    var progressFraction: Double {
        min(progressPercentage / 100.0, 1.0)
    }

    var formattedTargetAmount: String {
        targetAmount.currencyFormatted
    }

    var formattedBonusAmount: String {
        bonusAmount.currencyFormatted
    }

    var formattedEndDate: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        return formatter.string(from: endDate)
    }
}

// MARK: - Goal Progress Event DTO

struct GoalProgressEventDto: Codable {
    let contribution: GoalContributionDto
    let goal: SavingsGoalDto
    let newMilestonesAchieved: [GoalMilestoneDto]
    let matchContribution: GoalContributionDto?
    let challengeCompleted: Bool
    let challengeBonus: GoalContributionDto?
}

// MARK: - Request DTOs

struct CreateSavingsGoalRequest: Codable {
    let childId: UUID
    let name: String
    let description: String?
    let targetAmount: Decimal
    let category: GoalCategory
    let imageUrl: String?
    let autoTransferType: AutoTransferType?
    let autoTransferAmount: Decimal?
    let autoTransferPercentage: Decimal?
    let priority: Int?

    init(
        childId: UUID,
        name: String,
        targetAmount: Decimal,
        category: GoalCategory,
        description: String? = nil,
        imageUrl: String? = nil,
        autoTransferType: AutoTransferType? = nil,
        autoTransferAmount: Decimal? = nil,
        autoTransferPercentage: Decimal? = nil,
        priority: Int? = nil
    ) {
        self.childId = childId
        self.name = name
        self.targetAmount = targetAmount
        self.category = category
        self.description = description
        self.imageUrl = imageUrl
        self.autoTransferType = autoTransferType
        self.autoTransferAmount = autoTransferAmount
        self.autoTransferPercentage = autoTransferPercentage
        self.priority = priority
    }
}

struct UpdateSavingsGoalRequest: Codable {
    let name: String?
    let description: String?
    let targetAmount: Decimal?
    let category: GoalCategory?
    let imageUrl: String?
    let autoTransferType: AutoTransferType?
    let autoTransferAmount: Decimal?
    let autoTransferPercentage: Decimal?
    let priority: Int?
}

struct ContributeToGoalRequest: Codable {
    let amount: Decimal
    let description: String?

    init(amount: Decimal, description: String? = nil) {
        self.amount = amount
        self.description = description
    }
}

struct WithdrawFromGoalRequest: Codable {
    let amount: Decimal
    let reason: String?
}

struct CreateMatchingRuleRequest: Codable {
    let matchType: MatchingType
    let matchRatio: Decimal
    let maxMatchAmount: Decimal?

    init(matchType: MatchingType, matchRatio: Decimal, maxMatchAmount: Decimal? = nil) {
        self.matchType = matchType
        self.matchRatio = matchRatio
        self.maxMatchAmount = maxMatchAmount
    }
}

struct UpdateMatchingRuleRequest: Codable {
    let matchRatio: Decimal?
    let maxMatchAmount: Decimal?
    let isActive: Bool?
}

struct CreateGoalChallengeRequest: Codable {
    let targetAmount: Decimal
    let endDate: Date
    let bonusAmount: Decimal

    init(targetAmount: Decimal, endDate: Date, bonusAmount: Decimal) {
        self.targetAmount = targetAmount
        self.endDate = endDate
        self.bonusAmount = bonusAmount
    }
}

struct MarkGoalPurchasedRequest: Codable {
    let purchaseNotes: String?
}
