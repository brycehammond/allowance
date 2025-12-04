import Foundation
import SwiftUI

/// ViewModel for savings account management
@MainActor
final class SavingsAccountViewModel: ObservableObject {

    // MARK: - Published Properties

    @Published private(set) var accounts: [SavingsAccount] = []
    @Published private(set) var selectedAccount: SavingsAccount?
    @Published private(set) var transactions: [SavingsTransaction] = []
    @Published private(set) var summary: SavingsAccountSummary?
    @Published private(set) var isLoading = false
    @Published private(set) var isProcessing = false
    @Published private(set) var isBalanceHidden = false
    @Published var errorMessage: String?

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol
    private let childId: UUID

    // MARK: - Initialization

    init(childId: UUID, apiService: APIServiceProtocol = APIService()) {
        self.childId = childId
        self.apiService = apiService
    }

    // MARK: - Public Methods

    /// Load savings accounts for the child
    func loadAccounts() async {
        // Clear previous errors
        errorMessage = nil
        isBalanceHidden = false

        // Set loading state
        isLoading = true
        defer { isLoading = false }

        // First, check the savings summary to see if balance is hidden
        do {
            summary = try await apiService.getSavingsSummary(forChild: childId)
            if summary?.isBalanceHidden == true {
                isBalanceHidden = true
                accounts = []
                selectedAccount = nil
                transactions = []
                return
            }
        } catch {
            // If summary fails, continue to try loading accounts
            // (may fail for parents who don't have summary endpoint access)
        }

        do {
            accounts = try await apiService.getSavingsAccounts(forChild: childId)

            // Auto-select first account if none selected
            if selectedAccount == nil, let firstAccount = accounts.first {
                selectedAccount = firstAccount
                await loadTransactions(for: firstAccount.id)
            }

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load savings accounts. Please try again."
        }
    }

    /// Load transactions for a specific account
    /// - Parameter accountId: Savings account identifier
    func loadTransactions(for accountId: UUID) async {
        // Clear previous errors
        errorMessage = nil

        do {
            transactions = try await apiService.getSavingsTransactions(forAccount: accountId)

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load transactions. Please try again."
        }
    }

    /// Create a new savings account
    /// - Parameters:
    ///   - name: Account name
    ///   - targetAmount: Optional target amount
    ///   - autoTransferEnabled: Enable auto-transfer
    ///   - autoTransferPercentage: Auto-transfer percentage
    func createAccount(
        name: String,
        targetAmount: Decimal?,
        autoTransferEnabled: Bool,
        autoTransferPercentage: Decimal?
    ) async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Validate inputs
        guard !name.isEmpty else {
            errorMessage = "Please enter an account name."
            return false
        }

        if let target = targetAmount, target <= 0 {
            errorMessage = "Target amount must be greater than zero."
            return false
        }

        if autoTransferEnabled {
            guard let percentage = autoTransferPercentage, percentage > 0, percentage <= 100 else {
                errorMessage = "Auto-transfer percentage must be between 1 and 100."
                return false
            }
        }

        // Set processing state
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = CreateSavingsAccountRequest(
                childId: childId,
                name: name,
                targetAmount: targetAmount,
                autoTransferEnabled: autoTransferEnabled,
                autoTransferPercentage: autoTransferPercentage
            )

            let newAccount = try await apiService.createSavingsAccount(request)

            // Update local state
            accounts.append(newAccount)

            // Select the new account
            selectedAccount = newAccount
            transactions = []

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to create savings account. Please try again."
            return false
        }
    }

    /// Update an existing savings account
    /// - Parameters:
    ///   - id: Account identifier
    ///   - name: Updated name
    ///   - targetAmount: Updated target amount
    ///   - autoTransferEnabled: Updated auto-transfer setting
    ///   - autoTransferPercentage: Updated auto-transfer percentage
    func updateAccount(
        id: UUID,
        name: String,
        targetAmount: Decimal?,
        autoTransferEnabled: Bool,
        autoTransferPercentage: Decimal?
    ) async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Validate inputs
        guard !name.isEmpty else {
            errorMessage = "Please enter an account name."
            return false
        }

        if let target = targetAmount, target <= 0 {
            errorMessage = "Target amount must be greater than zero."
            return false
        }

        if autoTransferEnabled {
            guard let percentage = autoTransferPercentage, percentage > 0, percentage <= 100 else {
                errorMessage = "Auto-transfer percentage must be between 1 and 100."
                return false
            }
        }

        // Set processing state
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = UpdateSavingsAccountRequest(
                name: name,
                targetAmount: targetAmount,
                autoTransferEnabled: autoTransferEnabled,
                autoTransferPercentage: autoTransferPercentage
            )

            let updatedAccount = try await apiService.updateSavingsAccount(id: id, request)

            // Update local state
            if let index = accounts.firstIndex(where: { $0.id == id }) {
                accounts[index] = updatedAccount
            }

            if selectedAccount?.id == id {
                selectedAccount = updatedAccount
            }

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to update savings account. Please try again."
            return false
        }
    }

    /// Delete a savings account
    /// - Parameter id: Account identifier
    func deleteAccount(id: UUID) async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Set processing state
        isProcessing = true
        defer { isProcessing = false }

        do {
            try await apiService.deleteSavingsAccount(id: id)

            // Update local state
            accounts.removeAll { $0.id == id }

            if selectedAccount?.id == id {
                selectedAccount = accounts.first
                if let account = selectedAccount {
                    await loadTransactions(for: account.id)
                } else {
                    transactions = []
                }
            }

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to delete savings account. Please try again."
            return false
        }
    }

    /// Deposit money into a savings account
    /// - Parameters:
    ///   - accountId: Account identifier
    ///   - amount: Deposit amount
    ///   - notes: Optional notes
    func deposit(accountId: UUID, amount: Decimal, notes: String?) async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Validate amount
        guard amount > 0 else {
            errorMessage = "Deposit amount must be greater than zero."
            return false
        }

        // Set processing state
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = DepositRequest(amount: amount, notes: notes)
            let transaction = try await apiService.depositToSavings(accountId: accountId, request)

            // Update local state
            transactions.insert(transaction, at: 0)

            // Update account balance
            if let index = accounts.firstIndex(where: { $0.id == accountId }) {
                var updatedAccount = accounts[index]
                // Note: Backend should return updated balance, but we update locally as well
                accounts[index] = updatedAccount
            }

            // Reload account to get updated balance
            await loadAccounts()

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to deposit. Please try again."
            return false
        }
    }

    /// Withdraw money from a savings account
    /// - Parameters:
    ///   - accountId: Account identifier
    ///   - amount: Withdrawal amount
    ///   - notes: Optional notes
    func withdraw(accountId: UUID, amount: Decimal, notes: String?) async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Validate amount
        guard amount > 0 else {
            errorMessage = "Withdrawal amount must be greater than zero."
            return false
        }

        // Check sufficient balance
        if let account = accounts.first(where: { $0.id == accountId }) {
            guard account.currentBalance >= amount else {
                errorMessage = "Insufficient balance. Current balance: \(account.formattedBalance)"
                return false
            }
        }

        // Set processing state
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = WithdrawRequest(amount: amount, notes: notes)
            let transaction = try await apiService.withdrawFromSavings(accountId: accountId, request)

            // Update local state
            transactions.insert(transaction, at: 0)

            // Reload account to get updated balance
            await loadAccounts()

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to withdraw. Please try again."
            return false
        }
    }

    /// Select an account and load its transactions
    /// - Parameter account: Account to select
    func selectAccount(_ account: SavingsAccount) async {
        selectedAccount = account
        await loadTransactions(for: account.id)
    }

    /// Refresh accounts and transactions
    func refresh() async {
        await loadAccounts()
        if let selectedId = selectedAccount?.id {
            await loadTransactions(for: selectedId)
        }
    }

    /// Clear error message
    func clearError() {
        errorMessage = nil
    }

    // MARK: - Computed Properties

    /// Total balance across all savings accounts
    var totalSavingsBalance: Decimal {
        accounts.reduce(0) { $0 + $1.currentBalance }
    }

    /// Formatted total savings balance
    var formattedTotalBalance: String {
        totalSavingsBalance.currencyFormatted
    }

    /// Number of goal-reached accounts
    var goalsReachedCount: Int {
        accounts.filter { $0.isGoalReached }.count
    }

    /// Has any accounts
    var hasAccounts: Bool {
        !accounts.isEmpty
    }
}
