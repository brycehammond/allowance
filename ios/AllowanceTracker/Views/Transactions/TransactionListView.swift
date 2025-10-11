import SwiftUI

/// View displaying transaction history for a child
struct TransactionListView: View {

    // MARK: - Properties

    @StateObject private var viewModel: TransactionViewModel
    @State private var showingCreateTransaction = false

    // MARK: - Initialization

    init(childId: UUID, apiService: APIServiceProtocol = APIService()) {
        _viewModel = StateObject(wrappedValue: TransactionViewModel(childId: childId, apiService: apiService))
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
            ToolbarItem(placement: .navigationBarTrailing) {
                Button {
                    showingCreateTransaction = true
                } label: {
                    Image(systemName: "plus.circle.fill")
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

                // Transactions
                VStack(spacing: 8) {
                    ForEach(viewModel.transactions) { transaction in
                        TransactionRowView(transaction: transaction)
                    }
                }
            }
            .padding()
        }
    }

    /// Balance display card
    private var balanceCard: some View {
        VStack(spacing: 12) {
            HStack {
                Label("Current Balance", systemImage: "dollarsign.circle.fill")
                    .font(.headline)
                    .foregroundStyle(.blue)

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

                Button {
                    showingCreateTransaction = true
                } label: {
                    Label("Add Transaction", systemImage: "plus")
                        .font(.caption)
                        .fontWeight(.semibold)
                }
                .buttonStyle(.borderedProminent)
                .controlSize(.small)
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

                Text("Add your first transaction to get started")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }

            Button {
                showingCreateTransaction = true
            } label: {
                Label("Add Transaction", systemImage: "plus.circle.fill")
                    .fontWeight(.semibold)
                    .frame(maxWidth: .infinity)
                    .padding()
                    .background(Color.blue)
                    .foregroundStyle(.white)
                    .clipShape(RoundedRectangle(cornerRadius: 12))
            }
            .padding(.horizontal, 40)
            .padding(.top)
        }
        .padding()
    }

    // MARK: - Computed Properties

    /// Color based on balance amount
    private var balanceColor: Color {
        if viewModel.currentBalance < 0 {
            return .red
        } else if viewModel.currentBalance == 0 {
            return .secondary
        } else {
            return .green
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
