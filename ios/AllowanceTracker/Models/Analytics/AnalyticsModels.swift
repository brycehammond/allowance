import Foundation

// MARK: - Balance Point

struct BalancePoint: Codable, Identifiable {
    var id: UUID { UUID() }
    let date: Date
    let balance: Decimal
    let transactionDescription: String?

    var formattedDate: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .short
        return formatter.string(from: date)
    }

    var formattedBalance: String {
        balance.currencyFormatted
    }
}

// MARK: - Income vs Spending

struct IncomeSpendingSummary: Codable {
    let totalIncome: Decimal
    let totalSpending: Decimal
    let netSavings: Decimal
    let incomeTransactionCount: Int
    let spendingTransactionCount: Int
    let savingsRate: Decimal

    var formattedIncome: String {
        totalIncome.currencyFormatted
    }

    var formattedSpending: String {
        totalSpending.currencyFormatted
    }

    var formattedSavings: String {
        netSavings.currencyFormatted
    }

    var formattedSavingsRate: String {
        savingsRate.percentFormatted
    }
}

// MARK: - Monthly Comparison

struct MonthlyComparison: Codable, Identifiable {
    var id: UUID { UUID() }
    let year: Int
    let month: Int
    let monthName: String
    let income: Decimal
    let spending: Decimal
    let netSavings: Decimal
    let endingBalance: Decimal

    var formattedIncome: String {
        income.currencyFormatted
    }

    var formattedSpending: String {
        spending.currencyFormatted
    }

    var formattedSavings: String {
        netSavings.currencyFormatted
    }

    var formattedBalance: String {
        endingBalance.currencyFormatted
    }
}

// MARK: - Category Breakdown

struct CategoryBreakdown: Codable, Identifiable {
    var id: UUID { UUID() }
    let category: String
    let amount: Decimal
    let percentage: Decimal
    let transactionCount: Int

    var formattedAmount: String {
        amount.currencyFormatted
    }

    var formattedPercentage: String {
        percentage.percentFormatted
    }
}
