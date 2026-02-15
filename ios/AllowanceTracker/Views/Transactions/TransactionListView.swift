import SwiftUI

/// View displaying transaction history for a child
@MainActor
struct TransactionListView: View {

    // MARK: - Properties

    @State private var viewModel: TransactionViewModel
    @Environment(AuthViewModel.self) private var authViewModel
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @State private var showingCreateTransaction = false

    // Child settings passed from parent view
    private let initialSavingsBalance: Decimal
    private let initialAllowDebt: Bool

    // MARK: - Computed Properties

    private var isParent: Bool {
        authViewModel.effectiveIsParent
    }

    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    // MARK: - Initialization

    init(
        childId: UUID,
        savingsBalance: Decimal = 0,
        allowDebt: Bool = false,
        apiService: APIServiceProtocol = ServiceProvider.apiService
    ) {
        self.initialSavingsBalance = savingsBalance
        self.initialAllowDebt = allowDebt
        _viewModel = State(wrappedValue: TransactionViewModel(
            childId: childId,
            savingsBalance: savingsBalance,
            allowDebt: allowDebt,
            apiService: apiService
        ))
    }

    /// Update view model with new child settings
    func updateChildSettings(savingsBalance: Decimal, allowDebt: Bool) {
        viewModel.updateChildSettings(savingsBalance: savingsBalance, allowDebt: allowDebt)
    }

    // MARK: - Body

    var body: some View {
        ZStack {
            if viewModel.isLoading && viewModel.transactions.isEmpty {
                // Loading state
                ProgressView("Loading transactions...")
            } else if viewModel.transactions.isEmpty {
                // Empty state
                emptyStateView
            } else {
                // Transaction list
                transactionListView
            }
        }
        .navigationTitle("Transactions")
        .toolbar {
            if isParent {
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button {
                        showingCreateTransaction = true
                    } label: {
                        Image(systemName: "plus.circle.fill")
                    }
                }
            }
        }
        .sheet(isPresented: $showingCreateTransaction) {
            CreateTransactionView(viewModel: viewModel)
        }
        .refreshable {
            await viewModel.refresh()
        }
        .alert("Error", isPresented: .constant(viewModel.errorMessage != nil)) {
            Button("OK") {
                viewModel.clearError()
            }
        } message: {
            if let errorMessage = viewModel.errorMessage {
                Text(errorMessage)
            }
        }
        .task {
            await viewModel.loadTransactions()
            await viewModel.loadBalance()
        }
    }

    // MARK: - Subviews

    /// List of transactions
    private var transactionListView: some View {
        ScrollView {
            VStack(spacing: 16) {
                // Balance card
                balanceCard

                // Transactions - use grid on iPad for multi-column layout
                if isRegularWidth {
                    AdaptiveGrid(minItemWidth: 350, spacing: 12) {
                        ForEach(viewModel.transactions) { transaction in
                            TransactionRowView(transaction: transaction)
                        }
                    }
                } else {
                    VStack(spacing: 8) {
                        ForEach(viewModel.transactions) { transaction in
                            TransactionRowView(transaction: transaction)
                        }
                    }
                }
            }
            .adaptivePadding(.horizontal)
            .padding(.vertical, 16)
        }
    }

    /// Balance display card
    private var balanceCard: some View {
        VStack(spacing: 12) {
            HStack {
                Label("Current Balance", systemImage: "dollarsign.circle.fill")
                    .font(.headline)
                    .foregroundStyle(Color.green600)

                Spacer()
            }

            HStack {
                Text(viewModel.formattedBalance)
                    .font(.system(size: 36, weight: .bold, design: .monospaced))
                    .foregroundStyle(balanceColor)

                Spacer()
            }

            HStack {
                Text("\(viewModel.transactions.count) transactions")
                    .font(.caption)
                    .foregroundStyle(.secondary)

                Spacer()

                if isParent {
                    Button {
                        showingCreateTransaction = true
                    } label: {
                        Label("Add Transaction", systemImage: "plus")
                            .font(.caption)
                            .fontWeight(.semibold)
                    }
                    .buttonStyle(.borderedProminent)
                    .tint(Color.green600)
                    .controlSize(.small)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }

    /// Empty state when no transactions exist
    private var emptyStateView: some View {
        VStack(spacing: 24) {
            Image(systemName: "list.bullet.clipboard")
                .font(.system(size: 60))
                .foregroundStyle(.secondary)

            VStack(spacing: 8) {
                Text("No Transactions Yet")
                    .font(.title2)
                    .fontWeight(.bold)

                Text(isParent ? "Add your first transaction to get started" : "No transactions to display yet")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }

            if isParent {
                Button {
                    showingCreateTransaction = true
                } label: {
                    Label("Add Transaction", systemImage: "plus.circle.fill")
                        .fontWeight(.semibold)
                        .frame(maxWidth: .infinity)
                        .padding()
                        .background(Color.green600)
                        .foregroundStyle(.white)
                        .clipShape(RoundedRectangle(cornerRadius: 12))
                }
                .padding(.horizontal, 40)
                .padding(.top)
            }
        }
        .frame(maxWidth: 500)
        .adaptivePadding()
    }

    // MARK: - Computed Properties

    /// Color based on balance amount
    private var balanceColor: Color {
        if viewModel.currentBalance < 0 {
            return .error
        } else if viewModel.currentBalance == 0 {
            return .secondary
        } else {
            return .green500
        }
    }
}

// MARK: - Preview Provider

#Preview("Transaction List - With Data") {
    NavigationStack {
        TransactionListView(childId: UUID())
    }
}

#Preview("Transaction List - Empty") {
    NavigationStack {
        TransactionListView(childId: UUID())
    }
}
