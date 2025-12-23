import Foundation

// MARK: - Savings Account

struct SavingsAccount: Codable, Identifiable, Equatable {
    let id: UUID
    let childId: UUID
    let name: String
    let targetAmount: Decimal?
    let currentBalance: Decimal
    let autoTransferEnabled: Bool
    let autoTransferPercentage: Decimal?
    let createdAt: Date

    // MARK: - Computed Properties

    var hasTarget: Bool {
        targetAmount != nil
    }

    var targetProgress: Double? {
        guard let target = targetAmount, target > 0 else { return nil }
        let progress = Double(truncating: currentBalance as NSDecimalNumber) /
                      Double(truncating: target as NSDecimalNumber)
        return min(progress, 1.0)
    }

    var isGoalReached: Bool {
        guard let target = targetAmount else { return false }
        return currentBalance >= target
    }

    var formattedBalance: String {
        currentBalance.currencyFormatted
    }

    var formattedTargetAmount: String {
        targetAmount?.currencyFormatted ?? "No target"
    }

    var formattedAutoTransfer: String {
        guard autoTransferEnabled, let percentage = autoTransferPercentage else {
            return "Disabled"
        }
        return "\(percentage)% of allowance"
    }
}

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

struct CreateSavingsAccountRequest: Codable {
    let childId: UUID
    let name: String
    let targetAmount: Decimal?
    let autoTransferEnabled: Bool
    let autoTransferPercentage: Decimal?
}

struct UpdateSavingsAccountRequest: Codable {
    let name: String
    let targetAmount: Decimal?
    let autoTransferEnabled: Bool
    let autoTransferPercentage: Decimal?
}

struct DepositRequest: Codable {
    let amount: Decimal
    let notes: String?
}

struct WithdrawRequest: Codable {
    let amount: Decimal
    let notes: String?
}
