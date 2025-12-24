import Foundation

// MARK: - Savings Transaction

struct SavingsTransaction: Codable, Identifiable, Equatable {
    let id: UUID
    let childId: UUID
    let type: SavingsTransactionType
    let amount: Decimal
    let description: String
    let balanceAfter: Decimal
    let createdAt: Date
    let createdById: UUID
    let createdByName: String

    // MARK: - Computed Properties

    var isDeposit: Bool {
        type == .deposit || type == .autoTransfer
    }

    var formattedAmount: String {
        let prefix = isDeposit ? "+" : "-"
        return "\(prefix)\(amount.currencyFormatted)"
    }

    var formattedDate: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        formatter.timeStyle = .short
        return formatter.string(from: createdAt)
    }

    var typeDescription: String {
        switch type {
        case .deposit:
            return "Deposit"
        case .withdrawal:
            return "Withdrawal"
        case .autoTransfer:
            return "Auto Transfer"
        }
    }
}

// MARK: - Enums

enum SavingsTransactionType: String, Codable {
    case deposit = "Deposit"
    case withdrawal = "Withdrawal"
    case autoTransfer = "AutoTransfer"
}

// MARK: - Savings Account Summary (matches backend API response)

/// Summary of a child's savings account from the backend
struct SavingsAccountSummary: Codable {
    let childId: UUID
    let isEnabled: Bool
    let currentBalance: Decimal?
    let transferType: String
    let transferAmount: Decimal
    let transferPercentage: Decimal
    let totalTransactions: Int?
    let totalDeposited: Decimal?
    let totalWithdrawn: Decimal?
    let lastTransactionDate: Date?
    let configDescription: String
    let balanceHidden: Bool?

    /// Whether the balance is hidden from the child
    var isBalanceHidden: Bool {
        balanceHidden ?? false
    }

    /// Formatted current balance or placeholder if hidden
    var formattedBalance: String {
        guard let balance = currentBalance else { return "Hidden" }
        return balance.currencyFormatted
    }
}

// MARK: - DTOs for API requests

/// Request to deposit to savings from main balance
struct DepositToSavingsRequest: Codable {
    let childId: UUID
    let amount: Decimal
    let description: String
}

/// Request to withdraw from savings to main balance
struct WithdrawFromSavingsRequest: Codable {
    let childId: UUID
    let amount: Decimal
    let description: String
}
