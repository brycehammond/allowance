import SwiftUI

/// Screen for managing category budgets for a child
struct BudgetManagementView: View {
    @StateObject private var viewModel = BudgetViewModel()
    @State private var budgetToEdit: CategoryBudget?
    @State private var showDeleteConfirmation = false
    @State private var budgetToDelete: CategoryBudget?

    let child: Child

    var body: some View {
        Group {
            if viewModel.isLoading && viewModel.budgets.isEmpty {
                // Initial loading state
                ProgressView("Loading budgets...")
            } else if viewModel.budgets.isEmpty {
                // Empty state
                ContentUnavailableView(
                    "No Budgets Set",
                    systemImage: "calendar.badge.exclamationmark",
                    description: Text("Create a budget to start tracking spending by category")
                )
            } else {
                // Budget list
                ScrollView {
                    LazyVStack(spacing: 12) {
                        ForEach(viewModel.budgets) { budget in
                            BudgetCardView(
                                budget: budget,
                                status: viewModel.getStatus(for: budget),
                                onEdit: {
                                    budgetToEdit = budget
                                },
                                onDelete: {
                                    budgetToDelete = budget
                                    showDeleteConfirmation = true
                                }
                            )
                        }
                    }
                    .padding()
                }
            }
        }
        .navigationTitle("Budget Management")
        .navigationBarTitleDisplayMode(.large)
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                Button {
                    viewModel.showAddBudget = true
                } label: {
                    Label("Add Budget", systemImage: "plus")
                }
            }
        }
        .sheet(isPresented: $viewModel.showAddBudget) {
            AddBudgetSheet(childId: child.id, existingBudget: nil)
        }
        .sheet(item: $budgetToEdit) { budget in
            AddBudgetSheet(childId: child.id, existingBudget: budget)
        }
        .confirmationDialog(
            "Delete Budget",
            isPresented: $showDeleteConfirmation,
            presenting: budgetToDelete
        ) { budget in
            Button("Delete", role: .destructive) {
                Task {
                    await viewModel.deleteBudget(budget)
                }
            }
            Button("Cancel", role: .cancel) {}
        } message: { budget in
            Text("Are you sure you want to delete the budget for \(budget.category.displayName)?")
        }
        .alert("Error", isPresented: .constant(viewModel.errorMessage != nil)) {
            Button("OK") {
                viewModel.clearError()
            }
        } message: {
            Text(viewModel.errorMessage ?? "")
        }
        .task {
            await viewModel.loadBudgets(for: child.id)
        }
        .refreshable {
            await viewModel.loadBudgets(for: child.id)
        }
    }
}

// MARK: - Preview Provider

#Preview("With Budgets") {
    NavigationStack {
        BudgetManagementViewPreview(hasBudgets: true)
    }
}

#Preview("Empty State") {
    NavigationStack {
        BudgetManagementViewPreview(hasBudgets: false)
    }
}

#Preview("Loading State") {
    NavigationStack {
        BudgetManagementViewPreview(isLoading: true)
    }
}

private struct BudgetManagementViewPreview: View {
    let hasBudgets: Bool
    let isLoading: Bool

    init(hasBudgets: Bool = true, isLoading: Bool = false) {
        self.hasBudgets = hasBudgets
        self.isLoading = isLoading
    }

    var body: some View {
        BudgetManagementView(
            child: Child(
                id: UUID(),
                firstName: "Alice",
                lastName: "Johnson",
                weeklyAllowance: 10.00,
                currentBalance: 50.00,
                savingsBalance: 25.00,
                lastAllowanceDate: Date(),
                allowanceDay: .monday,
                savingsAccountEnabled: true,
                savingsTransferType: .percentage,
                savingsTransferPercentage: 20,
                savingsTransferAmount: nil,
                savingsBalanceVisibleToChild: true
            )
        )
    }
}
