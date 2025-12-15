import Foundation
import SwiftUI

/// ViewModel for transaction management and history
@Observable
@MainActor
final class TransactionViewModel {

    // MARK: - Observable Properties

    private(set) var transactions: [Transaction] = []
    private(set) var currentBalance: Decimal = 0
    private(set) var savingsBalance: Decimal = 0
    private(set) var allowDebt: Bool = false
    private(set) var isLoading = false
    private(set) var isCreatingTransaction = false
    var errorMessage: String?

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol
    let childId: UUID

    // MARK: - Initialization

    init(
        childId: UUID,
        savingsBalance: Decimal = 0,
        allowDebt: Bool = false,
        apiService: APIServiceProtocol = APIService()
    ) {
        self.childId = childId
        self.savingsBalance = savingsBalance
        self.allowDebt = allowDebt
        self.apiService = apiService
    }

    /// Update child settings (savings balance and allow debt)
    func updateChildSettings(savingsBalance: Decimal, allowDebt: Bool) {
        self.savingsBalance = savingsBalance
        self.allowDebt = allowDebt
    }

    // MARK: - Public Methods

    /// Load transactions for the child
    /// - Parameter limit: Maximum number of transactions to fetch
    func loadTransactions(limit: Int = 20) async {
        // Clear previous errors
        errorMessage = nil

        // Set loading state
        isLoading = true
        defer { isLoading = false }

        do {
            transactions = try await apiService.getTransactions(forChild: childId, limit: limit)

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load transactions. Please try again."
        }
    }

    /// Load current balance for the child
    func loadBalance() async {
        // Don't show loading spinner for balance refresh
        do {
            currentBalance = try await apiService.getBalance(forChild: childId)

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load balance. Please try again."
        }
    }

    /// Create a new transaction
    /// - Parameters:
    ///   - amount: Transaction amount (must be positive)
    ///   - type: Credit or Debit
    ///   - category: Transaction category
    ///   - description: Transaction description
    ///   - drawFromSavings: Whether to draw from savings if spending balance is insufficient
    func createTransaction(
        amount: Decimal,
        type: TransactionType,
        category: String,
        description: String,
        drawFromSavings: Bool = false
    ) async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Validate inputs
        guard amount > 0 else {
            errorMessage = "Amount must be greater than zero."
            return false
        }

        guard !category.isEmpty else {
            errorMessage = "Please select a category."
            return false
        }

        guard !description.isEmpty else {
            errorMessage = "Please enter a description."
            return false
        }

        // Set creating state
        isCreatingTransaction = true
        defer { isCreatingTransaction = false }

        do {
            let request = CreateTransactionRequest(
                childId: childId,
                amount: amount,
                type: type,
                category: category,
                description: description,
                drawFromSavings: drawFromSavings
            )

            let newTransaction = try await apiService.createTransaction(request)

            // Update local state
            transactions.insert(newTransaction, at: 0)
            currentBalance = newTransaction.balanceAfter

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to create transaction. Please try again."
            return false
        }
    }

    /// Check if a debit transaction would require drawing from savings or going into debt
    /// - Parameters:
    ///   - amount: The transaction amount
    /// - Returns: Tuple with (needsConfirmation, fromSpending, fromSavings, intoDebt)
    func checkDebitImpact(amount: Decimal) -> (needsConfirmation: Bool, fromSpending: Decimal, fromSavings: Decimal, intoDebt: Decimal) {
        guard amount > currentBalance else {
            // Transaction fits within spending balance
            return (false, amount, 0, 0)
        }

        let shortfall = amount - currentBalance
        let totalAvailable = currentBalance + savingsBalance

        if amount <= totalAvailable {
            // Can cover with savings
            return (true, currentBalance, shortfall, 0)
        } else if allowDebt {
            // Will need to go into debt
            let debtAmount = amount - totalAvailable
            return (true, currentBalance, savingsBalance, debtAmount)
        } else {
            // Not enough funds and debt not allowed
            return (true, currentBalance, savingsBalance, 0)
        }
    }

    /// Refresh transactions and balance
    func refresh() async {
        await loadTransactions()
        await loadBalance()
    }

    /// Clear error message
    func clearError() {
        errorMessage = nil
    }

    /// Get formatted balance string
    var formattedBalance: String {
        currentBalance.currencyFormatted
    }
}
