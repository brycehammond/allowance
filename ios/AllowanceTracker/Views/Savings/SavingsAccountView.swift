import SwiftUI

/// Main savings view showing balance, summary cards, and transaction history
/// Uses single-balance-per-child model matching the API
@MainActor
struct SavingsAccountView: View {

    // MARK: - Properties

    @State private var viewModel: SavingsAccountViewModel
    @State private var showingDeposit = false
    @State private var showingWithdraw = false
    @State private var depositAmount: String = ""
    @State private var depositDescription: String = ""
    @State private var withdrawAmount: String = ""
    @State private var withdrawDescription: String = ""

    let childId: UUID

    // MARK: - Initialization

    init(childId: UUID) {
        self.childId = childId
        _viewModel = State(wrappedValue: SavingsAccountViewModel(childId: childId))
    }

    // MARK: - Body

    var body: some View {
        ZStack {
            if viewModel.isLoading && viewModel.summary == nil {
                ProgressView("Loading savings...")
            } else if viewModel.isBalanceHidden {
                balanceHiddenView
            } else if !viewModel.isEnabled {
                savingsNotEnabledView
            } else {
                savingsContentView
            }
        }
        .navigationTitle("Savings")
        .alert("Error", isPresented: .constant(viewModel.errorMessage != nil)) {
            Button("OK") {
                viewModel.clearError()
            }
        } message: {
            if let errorMessage = viewModel.errorMessage {
                Text(errorMessage)
            }
        }
        .refreshable {
            await viewModel.refresh()
        }
        .task {
            await viewModel.loadSavingsData()
        }
        .sheet(isPresented: $showingDeposit) {
            depositSheet
        }
        .sheet(isPresented: $showingWithdraw) {
            withdrawSheet
        }
    }

    // MARK: - Main Content View

    private var savingsContentView: some View {
        List {
            // Summary section
            Section {
                summaryHeader
            }

            // Action buttons
            Section {
                actionButtons
            }

            // Transaction history
            Section {
                if viewModel.hasTransactions {
                    ForEach(viewModel.transactions) { transaction in
                        SavingsTransactionRow(transaction: transaction)
                    }
                } else {
                    ContentUnavailableView(
                        "No Transactions",
                        systemImage: "list.bullet.rectangle",
                        description: Text("Deposits and withdrawals will appear here")
                    )
                }
            } header: {
                Text("Transaction History")
            }
        }
        .listStyle(.insetGrouped)
    }

    // MARK: - Summary Header

    private var summaryHeader: some View {
        VStack(spacing: 16) {
            // Current balance
            VStack(spacing: 4) {
                Text("Savings Balance")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)

                Text(viewModel.formattedBalance)
                    .font(.system(size: 36, weight: .bold))
                    .fontDesign(.monospaced)
                    .foregroundStyle(DesignSystem.Colors.primary)
            }
            .frame(maxWidth: .infinity)
            .padding(.vertical)

            // Stats row
            HStack(spacing: 24) {
                VStack(spacing: 4) {
                    Text(viewModel.totalDeposited.currencyFormatted)
                        .font(.headline)
                        .fontDesign(.monospaced)
                        .foregroundStyle(.green)
                    Text("Deposited")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Divider()
                    .frame(height: 30)

                VStack(spacing: 4) {
                    Text(viewModel.totalWithdrawn.currencyFormatted)
                        .font(.headline)
                        .fontDesign(.monospaced)
                        .foregroundStyle(.orange)
                    Text("Withdrawn")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Divider()
                    .frame(height: 30)

                VStack(spacing: 4) {
                    Text(viewModel.configDescription)
                        .font(.headline)
                        .lineLimit(1)
                        .minimumScaleFactor(0.8)
                    Text("Auto Transfer")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
        }
    }

    // MARK: - Action Buttons

    private var actionButtons: some View {
        HStack(spacing: 12) {
            Button {
                depositAmount = ""
                depositDescription = ""
                showingDeposit = true
            } label: {
                Label("Deposit", systemImage: "plus.circle.fill")
                    .font(.subheadline)
                    .fontWeight(.medium)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
            }
            .buttonStyle(.borderedProminent)
            .tint(.green)

            Button {
                withdrawAmount = ""
                withdrawDescription = ""
                showingWithdraw = true
            } label: {
                Label("Withdraw", systemImage: "minus.circle.fill")
                    .font(.subheadline)
                    .fontWeight(.medium)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
            }
            .buttonStyle(.borderedProminent)
            .tint(.orange)
        }
        .listRowInsets(EdgeInsets())
        .listRowBackground(Color.clear)
    }

    // MARK: - Deposit Sheet

    private var depositSheet: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Amount", text: $depositAmount)
                        .keyboardType(.decimalPad)
                } header: {
                    Text("Amount")
                } footer: {
                    Text("Enter the amount to transfer from spending to savings")
                }

                Section {
                    TextField("Description", text: $depositDescription)
                } header: {
                    Text("Description")
                }
            }
            .navigationTitle("Deposit to Savings")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        showingDeposit = false
                    }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Deposit") {
                        Task {
                            if let amount = Decimal(string: depositAmount) {
                                let success = await viewModel.deposit(
                                    amount: amount,
                                    description: depositDescription
                                )
                                if success {
                                    showingDeposit = false
                                }
                            }
                        }
                    }
                    .disabled(depositAmount.isEmpty || depositDescription.isEmpty || viewModel.isProcessing)
                }
            }
        }
        .presentationDetents([.medium])
    }

    // MARK: - Withdraw Sheet

    private var withdrawSheet: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Amount", text: $withdrawAmount)
                        .keyboardType(.decimalPad)
                } header: {
                    Text("Amount")
                } footer: {
                    Text("Available: \(viewModel.formattedBalance)")
                }

                Section {
                    TextField("Description", text: $withdrawDescription)
                } header: {
                    Text("Description")
                }
            }
            .navigationTitle("Withdraw from Savings")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        showingWithdraw = false
                    }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Withdraw") {
                        Task {
                            if let amount = Decimal(string: withdrawAmount) {
                                let success = await viewModel.withdraw(
                                    amount: amount,
                                    description: withdrawDescription
                                )
                                if success {
                                    showingWithdraw = false
                                }
                            }
                        }
                    }
                    .disabled(withdrawAmount.isEmpty || withdrawDescription.isEmpty || viewModel.isProcessing)
                }
            }
        }
        .presentationDetents([.medium])
    }

    // MARK: - Empty States

    private var savingsNotEnabledView: some View {
        ContentUnavailableView(
            "Savings Not Enabled",
            systemImage: "banknote",
            description: Text("Savings account is not enabled for this child. Enable it in Settings.")
        )
    }

    private var balanceHiddenView: some View {
        ContentUnavailableView(
            "Balance Hidden",
            systemImage: "eye.slash",
            description: Text("The savings balance is hidden from this view.")
        )
    }
}

// MARK: - Savings Transaction Row

/// Row displaying a savings transaction
struct SavingsTransactionRow: View {
    let transaction: SavingsTransaction

    var body: some View {
        HStack(spacing: 12) {
            Image(systemName: transaction.isDeposit ? "arrow.down.circle.fill" : "arrow.up.circle.fill")
                .font(.title2)
                .foregroundStyle(transaction.isDeposit ? .green : .orange)

            VStack(alignment: .leading, spacing: 2) {
                Text(transaction.typeDescription)
                    .font(.headline)

                if !transaction.description.isEmpty {
                    Text(transaction.description)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .lineLimit(1)
                }

                Text("\(transaction.formattedDate) - \(transaction.createdByName)")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            VStack(alignment: .trailing, spacing: 2) {
                Text(transaction.formattedAmount)
                    .font(.headline)
                    .fontDesign(.monospaced)
                    .foregroundStyle(transaction.isDeposit ? .green : .orange)

                Text("Bal: \(transaction.balanceAfter.currencyFormatted)")
                    .font(.caption)
                    .fontDesign(.monospaced)
                    .foregroundStyle(.secondary)
            }
        }
        .padding(.vertical, 4)
    }
}

// MARK: - Preview Provider

#Preview("Savings Account View") {
    NavigationStack {
        SavingsAccountView(childId: UUID())
    }
}
