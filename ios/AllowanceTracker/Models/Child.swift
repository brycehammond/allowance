import Foundation

struct Child: Codable, Identifiable, Equatable {
    let id: UUID
    let firstName: String
    let lastName: String
    let weeklyAllowance: Decimal
    let currentBalance: Decimal
    let lastAllowanceDate: Date?
    var allowanceDay: Weekday? = nil

    var fullName: String {
        "\(firstName) \(lastName)"
    }

    var formattedBalance: String {
        currentBalance.currencyFormatted
    }

    var allowanceDayDisplay: String {
        guard let day = allowanceDay else {
            return "Rolling 7-day window"
        }
        return "Every \(day.rawValue)"
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

    init(
        weeklyAllowance: Decimal,
        savingsAccountEnabled: Bool = false,
        savingsTransferType: SavingsTransferType = .percentage,
        savingsTransferPercentage: Decimal? = nil,
        savingsTransferAmount: Decimal? = nil,
        allowanceDay: Weekday? = nil
    ) {
        self.weeklyAllowance = weeklyAllowance
        self.savingsAccountEnabled = savingsAccountEnabled
        self.savingsTransferType = savingsTransferType
        self.savingsTransferPercentage = savingsTransferPercentage
        self.savingsTransferAmount = savingsTransferAmount
        self.allowanceDay = allowanceDay
    }
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
