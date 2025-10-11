import Foundation
import SwiftUI

/// View model for managing budgets and budget-related state
@MainActor
final class BudgetViewModel: ObservableObject {

    // MARK: - Published Properties

    @Published var budgets: [CategoryBudget] = []
    @Published var budgetStatuses: [CategoryBudgetStatus] = []
    @Published var isLoading = false
    @Published var errorMessage: String?
    @Published var showAddBudget = false

    // MARK: - Services

    private let budgetService: BudgetServiceProtocol
    private let categoryService: CategoryServiceProtocol

    // MARK: - Initialization

    init(
        budgetService: BudgetServiceProtocol = BudgetService.shared,
        categoryService: CategoryServiceProtocol = CategoryService.shared
    ) {
        self.budgetService = budgetService
        self.categoryService = categoryService
    }

    // MARK: - Public Methods

    /// Load all budgets and their current status for a child
    func loadBudgets(for childId: UUID) async {
        isLoading = true
        errorMessage = nil

        do {
            // Load budgets and statuses in parallel
            async let budgetsTask = budgetService.getBudgets(for: childId)
            async let weeklyStatusTask = categoryService.getBudgetStatus(for: childId, period: .weekly)
            async let monthlyStatusTask = categoryService.getBudgetStatus(for: childId, period: .monthly)

            budgets = try await budgetsTask
            let weeklyStatus = try await weeklyStatusTask
            let monthlyStatus = try await monthlyStatusTask
            budgetStatuses = weeklyStatus + monthlyStatus

        } catch {
            errorMessage = "Failed to load budgets: \(error.localizedDescription)"
            budgets = []
            budgetStatuses = []
        }

        isLoading = false
    }

    /// Create a new budget
    func createBudget(_ request: SetBudgetRequest) async -> Bool {
        errorMessage = nil

        do {
            let budget = try await budgetService.setBudget(request)
            budgets.append(budget)
            return true
        } catch {
            errorMessage = "Failed to create budget: \(error.localizedDescription)"
            return false
        }
    }

    /// Update an existing budget
    func updateBudget(_ request: SetBudgetRequest) async -> Bool {
        errorMessage = nil

        do {
            let updated = try await budgetService.setBudget(request)
            if let index = budgets.firstIndex(where: { $0.id == updated.id }) {
                budgets[index] = updated
            }
            return true
        } catch {
            errorMessage = "Failed to update budget: \(error.localizedDescription)"
            return false
        }
    }

    /// Delete a budget
    func deleteBudget(_ budget: CategoryBudget) async -> Bool {
        errorMessage = nil

        do {
            try await budgetService.deleteBudget(for: budget.childId, category: budget.category)
            budgets.removeAll { $0.id == budget.id }
            budgetStatuses.removeAll { $0.category == budget.category && $0.period == budget.period }
            return true
        } catch {
            errorMessage = "Failed to delete budget: \(error.localizedDescription)"
            return false
        }
    }

    /// Get current status for a specific budget
    func getStatus(for budget: CategoryBudget) -> CategoryBudgetStatus? {
        budgetStatuses.first {
            $0.category == budget.category && $0.period == budget.period
        }
    }

    /// Check if a category already has a budget
    func hasBudget(for category: TransactionCategory, period: BudgetPeriod) -> Bool {
        budgets.contains { $0.category == category && $0.period == period }
    }

    /// Clear error message
    func clearError() {
        errorMessage = nil
    }
}
