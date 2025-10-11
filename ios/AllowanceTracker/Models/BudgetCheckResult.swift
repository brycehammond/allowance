import Foundation

// MARK: - Budget Check Result

struct BudgetCheckResult: Codable, Equatable {
    let allowed: Bool
    let message: String
    let currentSpending: Decimal
    let budgetLimit: Decimal
    let remainingAfter: Decimal

    var formattedCurrentSpending: String {
        currentSpending.currencyFormatted
    }

    var formattedBudgetLimit: String {
        budgetLimit.currencyFormatted
    }

    var formattedRemainingAfter: String {
        remainingAfter.currencyFormatted
    }

    var isWarning: Bool {
        allowed && remainingAfter < (budgetLimit * 0.2) // Less than 20% remaining
    }

    var isError: Bool {
        !allowed
    }
}
