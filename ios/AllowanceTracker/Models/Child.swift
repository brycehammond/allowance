import Foundation

struct Child: Codable, Identifiable, Equatable {
    let id: UUID
    let firstName: String
    let lastName: String
    let weeklyAllowance: Decimal
    let currentBalance: Decimal
    let savingsBalance: Decimal
    let lastAllowanceDate: Date?
    var allowanceDay: Weekday? = nil
    let savingsAccountEnabled: Bool
    let savingsTransferType: SavingsTransferType
    let savingsTransferPercentage: Decimal?
    let savingsTransferAmount: Decimal?
    let savingsBalanceVisibleToChild: Bool

    var fullName: String {
        "\(firstName) \(lastName)"
    }

    var formattedBalance: String {
        currentBalance.currencyFormatted
    }

    var formattedSavingsBalance: String {
        savingsBalance.currencyFormatted
    }

    var formattedTotalBalance: String {
        (currentBalance + savingsBalance).currencyFormatted
    }

    var totalBalance: Decimal {
        currentBalance + savingsBalance
    }

    var allowanceDayDisplay: String {
        guard let day = allowanceDay else {
            return "Rolling 7-day window"
        }
        return "Every \(day.rawValue)"
    }

    /// Calculate example savings amount based on current configuration
    var exampleSavingsAmount: Decimal {
        guard savingsAccountEnabled else { return 0 }
        switch savingsTransferType {
        case .percentage:
            let percentage = savingsTransferPercentage ?? 0
            return weeklyAllowance * percentage / 100
        case .fixedAmount:
            let amount = savingsTransferAmount ?? 0
            return min(amount, weeklyAllowance)
        case .none:
            return 0
        }
    }

    /// Calculate example spending amount based on current configuration
    var exampleSpendingAmount: Decimal {
        weeklyAllowance - exampleSavingsAmount
    }
}

/// Represents days of the week for allowance scheduling
enum Weekday: String, Codable, CaseIterable {
    case sunday = "Sunday"
    case monday = "Monday"
    case tuesday = "Tuesday"
    case wednesday = "Wednesday"
    case thursday = "Thursday"
    case friday = "Friday"
    case saturday = "Saturday"
}

// MARK: - Child Settings DTOs

struct UpdateChildSettingsRequest: Codable {
    let weeklyAllowance: Decimal
    let savingsAccountEnabled: Bool
    let savingsTransferType: SavingsTransferType
    let savingsTransferPercentage: Decimal?
    let savingsTransferAmount: Decimal?
    let allowanceDay: Weekday?
    let savingsBalanceVisibleToChild: Bool?

    init(
        weeklyAllowance: Decimal,
        savingsAccountEnabled: Bool = false,
        savingsTransferType: SavingsTransferType = .percentage,
        savingsTransferPercentage: Decimal? = nil,
        savingsTransferAmount: Decimal? = nil,
        allowanceDay: Weekday? = nil,
        savingsBalanceVisibleToChild: Bool? = nil
    ) {
        self.weeklyAllowance = weeklyAllowance
        self.savingsAccountEnabled = savingsAccountEnabled
        self.savingsTransferType = savingsTransferType
        self.savingsTransferPercentage = savingsTransferPercentage
        self.savingsTransferAmount = savingsTransferAmount
        self.allowanceDay = allowanceDay
        self.savingsBalanceVisibleToChild = savingsBalanceVisibleToChild
    }
}

/// Request to create a new child account
struct CreateChildRequest: Codable {
    let email: String
    let password: String
    let firstName: String
    let lastName: String
    let weeklyAllowance: Decimal
    let savingsAccountEnabled: Bool
    let savingsTransferType: SavingsTransferType
    let savingsTransferPercentage: Decimal?
    let savingsTransferAmount: Decimal?
    let initialBalance: Decimal?
    let initialSavingsBalance: Decimal?
}

enum SavingsTransferType: String, Codable {
    case none = "None"
    case percentage = "Percentage"
    case fixedAmount = "FixedAmount"
}

struct UpdateChildSettingsResponse: Codable {
    let childId: UUID
    let firstName: String
    let lastName: String
    let weeklyAllowance: Decimal
    let allowanceDay: Weekday?
    let savingsAccountEnabled: Bool
    let savingsTransferType: String
    let savingsTransferPercentage: Int
    let savingsTransferAmount: Decimal
    let message: String
}
