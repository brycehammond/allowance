import Foundation
import SwiftUI

/// ViewModel for transaction management and history
@MainActor
final class TransactionViewModel: ObservableObject {

    // MARK: - Published Properties

    @Published private(set) var transactions: [Transaction] = []
    @Published private(set) var currentBalance: Decimal = 0
    @Published private(set) var isLoading = false
    @Published private(set) var isCreatingTransaction = false
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
    func createTransaction(
        amount: Decimal,
        type: TransactionType,
        category: String,
        description: String
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
                description: description
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
