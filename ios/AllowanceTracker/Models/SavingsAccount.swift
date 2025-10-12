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
    let savingsAccountId: UUID
    let amount: Decimal
    let type: SavingsTransactionType
    let transferType: SavingsTransferType
    let balanceAfter: Decimal
    let notes: String?
    let createdAt: Date

    // MARK: - Computed Properties

    var isDeposit: Bool {
        type == .deposit
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
        switch transferType {
        case .manual:
            return isDeposit ? "Manual Deposit" : "Manual Withdrawal"
        case .autoTransfer:
            return "Auto Transfer"
        case .goal:
            return "Goal Transfer"
        }
    }
}

// MARK: - Enums

enum SavingsTransactionType: String, Codable {
    case deposit = "Deposit"
    case withdrawal = "Withdrawal"
}

enum SavingsTransferType: String, Codable {
    case manual = "Manual"
    case autoTransfer = "AutoTransfer"
    case goal = "Goal"
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
