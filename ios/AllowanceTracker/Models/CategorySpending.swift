import Foundation
import SwiftUI

// MARK: - Category Spending

struct CategorySpending: Codable, Identifiable, Equatable {
    let category: TransactionCategory
    let categoryName: String
    let totalAmount: Decimal
    let transactionCount: Int
    let percentage: Decimal

    var id: String { category.rawValue }

    var formattedAmount: String {
        totalAmount.currencyFormatted
    }
}

// MARK: - Category Budget Status

struct CategoryBudgetStatus: Codable, Identifiable, Equatable {
    let category: TransactionCategory
    let categoryName: String
    let budgetLimit: Decimal
    let currentSpending: Decimal
    let remaining: Decimal
    let percentUsed: Int
    let status: BudgetStatus
    let period: BudgetPeriod

    var id: String { category.rawValue }

    var progressColor: Color {
        switch status {
        case .safe: return .green
        case .warning: return .orange
        case .atLimit, .overBudget: return .red
        }
    }

    var formattedBudgetLimit: String {
        budgetLimit.currencyFormatted
    }

    var formattedCurrentSpending: String {
        currentSpending.currencyFormatted
    }

    var formattedRemaining: String {
        remaining.currencyFormatted
    }
}
