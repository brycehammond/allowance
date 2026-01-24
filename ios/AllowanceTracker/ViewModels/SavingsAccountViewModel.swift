import Foundation
import SwiftUI

/// ViewModel for savings account management
/// Uses single-balance-per-child model matching the API
@Observable
@MainActor
final class SavingsAccountViewModel {

    // MARK: - Observable Properties

    private(set) var summary: SavingsAccountSummary?
    private(set) var transactions: [SavingsTransaction] = []
    private(set) var isLoading = false
    private(set) var isProcessing = false
    var errorMessage: String?

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol
    private let childId: UUID

    // MARK: - Initialization

    init(childId: UUID, apiService: APIServiceProtocol = ServiceProvider.apiService) {
        self.childId = childId
        self.apiService = apiService
    }

    // MARK: - Public Methods

    /// Load savings summary and transaction history
    func loadSavingsData() async {
        errorMessage = nil
        isLoading = true
        defer { isLoading = false }

        do {
            // Load summary and history in parallel
            async let summaryTask = apiService.getSavingsSummary(forChild: childId)
            async let historyTask = apiService.getSavingsHistory(forChild: childId, limit: 50)

            summary = try await summaryTask
            transactions = try await historyTask
        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load savings data. Please try again."
        }
    }

    /// Deposit money to savings from main balance
    /// - Parameters:
    ///   - amount: Deposit amount
    ///   - description: Description for the transaction
    func deposit(amount: Decimal, description: String) async -> Bool {
        errorMessage = nil

        guard amount > 0 else {
            errorMessage = "Deposit amount must be greater than zero."
            return false
        }

        guard !description.isEmpty else {
            errorMessage = "Please enter a description."
            return false
        }

        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = DepositToSavingsRequest(
                childId: childId,
                amount: amount,
                description: description
            )
            let transaction = try await apiService.depositToSavings(request)

            // Update local state
            transactions.insert(transaction, at: 0)

            // Reload to get updated balance
            await loadSavingsData()

            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to deposit. Please try again."
            return false
        }
    }

    /// Withdraw money from savings to main balance
    /// - Parameters:
    ///   - amount: Withdrawal amount
    ///   - description: Description for the transaction
    func withdraw(amount: Decimal, description: String) async -> Bool {
        errorMessage = nil

        guard amount > 0 else {
            errorMessage = "Withdrawal amount must be greater than zero."
            return false
        }

        guard !description.isEmpty else {
            errorMessage = "Please enter a description."
            return false
        }

        // Check sufficient balance
        if let balance = summary?.currentBalance, balance < amount {
            errorMessage = "Insufficient balance. Available: \(balance.currencyFormatted)"
            return false
        }

        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = WithdrawFromSavingsRequest(
                childId: childId,
                amount: amount,
                description: description
            )
            let transaction = try await apiService.withdrawFromSavings(request)

            // Update local state
            transactions.insert(transaction, at: 0)

            // Reload to get updated balance
            await loadSavingsData()

            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to withdraw. Please try again."
            return false
        }
    }

    /// Refresh savings data
    func refresh() async {
        await loadSavingsData()
    }

    /// Clear error message
    func clearError() {
        errorMessage = nil
    }

    // MARK: - Computed Properties

    /// Whether savings is enabled for this child
    var isEnabled: Bool {
        summary?.isEnabled ?? false
    }

    /// Whether the balance is hidden from the child
    var isBalanceHidden: Bool {
        summary?.isBalanceHidden ?? false
    }

    /// Current savings balance
    var currentBalance: Decimal {
        summary?.currentBalance ?? 0
    }

    /// Formatted current balance
    var formattedBalance: String {
        currentBalance.currencyFormatted
    }

    /// Total deposited
    var totalDeposited: Decimal {
        summary?.totalDeposited ?? 0
    }

    /// Total withdrawn
    var totalWithdrawn: Decimal {
        summary?.totalWithdrawn ?? 0
    }

    /// Configuration description
    var configDescription: String {
        summary?.configDescription ?? "Not configured"
    }

    /// Has any transactions
    var hasTransactions: Bool {
        !transactions.isEmpty
    }
}
