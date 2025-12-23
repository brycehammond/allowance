import SwiftUI

/// Main savings account view showing accounts and transactions
@MainActor
struct SavingsAccountView: View {

    // MARK: - Properties

    @State private var viewModel: SavingsAccountViewModel
    @State private var showingAddAccount = false
    @State private var showingDeposit = false
    @State private var showingWithdraw = false
    @State private var showingEditAccount = false

    let childId: UUID

    // MARK: - Initialization

    init(childId: UUID) {
        self.childId = childId
        _viewModel = State(wrappedValue: SavingsAccountViewModel(childId: childId))
    }

    // MARK: - Body

    var body: some View {
        ZStack {
            if viewModel.isLoading && !viewModel.hasAccounts {
                ProgressView("Loading savings accounts...")
            } else if viewModel.isBalanceHidden || !viewModel.hasAccounts {
                emptyStateView
            } else {
                accountsView
            }
        }
        .navigationTitle("Savings")
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                Button {
                    showingAddAccount = true
                } label: {
                    Image(systemName: "plus")
                }
            }
        }
        .sheet(isPresented: $showingAddAccount) {
            AddSavingsAccountSheet(viewModel: viewModel)
        }
        .sheet(isPresented: $showingDeposit) {
            if let account = viewModel.selectedAccount {
                DepositSheet(viewModel: viewModel, account: account)
            }
        }
        .sheet(isPresented: $showingWithdraw) {
            if let account = viewModel.selectedAccount {
                WithdrawSheet(viewModel: viewModel, account: account)
            }
        }
        .sheet(isPresented: $showingEditAccount) {
            if let account = viewModel.selectedAccount {
                EditSavingsAccountSheet(viewModel: viewModel, account: account)
            }
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
        .refreshable {
            await viewModel.refresh()
        }
        .task {
            await viewModel.loadAccounts()
        }
    }

    // MARK: - Subviews

    /// Main accounts view with account selector and transactions
    private var accountsView: some View {
        VStack(spacing: 0) {
            // Account summary cards
            if viewModel.accounts.count > 1 {
                accountSelectorSection
            } else if let account = viewModel.selectedAccount {
                singleAccountHeader(account)
            }

            Divider()

            // Transactions list
            transactionsList
        }
    }

    /// Account selector for multiple accounts
    private var accountSelectorSection: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 12) {
                ForEach(viewModel.accounts) { account in
                    AccountCard(
                        account: account,
                        isSelected: viewModel.selectedAccount?.id == account.id,
                        onTap: {
                            Task {
                                await viewModel.selectAccount(account)
                            }
                        }
                    )
                }
            }
            .padding()
        }
        .background(Color(.systemGroupedBackground))
    }

    /// Single account header when only one account exists
    private func singleAccountHeader(_ account: SavingsAccount) -> some View {
        VStack(spacing: 16) {
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text(account.name)
                        .font(.headline)

                    if account.hasTarget, let progress = account.targetProgress {
                        HStack(spacing: 4) {
                            Text("\(Int(progress * 100))% of goal")
                                .font(.caption)
                                .foregroundStyle(.secondary)

                            if account.isGoalReached {
                                Image(systemName: "checkmark.circle.fill")
                                    .font(.caption)
                                    .foregroundStyle(.green)
                            }
                        }
                    }
                }

                Spacer()

                VStack(alignment: .trailing, spacing: 4) {
                    Text(account.formattedBalance)
                        .font(.title2)
                        .fontWeight(.bold)
                        .fontDesign(.monospaced)
                        .foregroundStyle(DesignSystem.Colors.primary)

                    if account.hasTarget {
                        Text("of \(account.formattedTargetAmount)")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
            }

            // Progress bar
            if account.hasTarget, let progress = account.targetProgress {
                ProgressView(value: progress)
                    .tint(account.isGoalReached ? .green : DesignSystem.Colors.primary)
            }

            // Action buttons
            actionButtons
        }
        .padding()
        .background(Color(.systemGroupedBackground))
    }

    /// Action buttons for deposit/withdraw
    private var actionButtons: some View {
        HStack(spacing: 12) {
            Button {
                showingDeposit = true
            } label: {
                Label("Deposit", systemImage: "plus.circle.fill")
                    .font(.subheadline)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
            }
            .buttonStyle(.borderedProminent)

            Button {
                showingWithdraw = true
            } label: {
                Label("Withdraw", systemImage: "minus.circle.fill")
                    .font(.subheadline)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
            }
            .buttonStyle(.bordered)

            Button {
                showingEditAccount = true
            } label: {
                Image(systemName: "ellipsis.circle")
                    .font(.title3)
                    .padding(.vertical, 12)
            }
            .buttonStyle(.bordered)
        }
    }

    /// Transactions list
    private var transactionsList: some View {
        List {
            if viewModel.transactions.isEmpty {
                ContentUnavailableView(
                    "No Transactions",
                    systemImage: "list.bullet.rectangle",
                    description: Text("Deposits and withdrawals will appear here")
                )
            } else {
                ForEach(viewModel.transactions) { transaction in
                    SavingsTransactionRow(transaction: transaction)
                }
            }
        }
        .listStyle(.plain)
    }

    /// Empty state when no accounts exist
    private var emptyStateView: some View {
        VStack(spacing: 24) {
            Image(systemName: "banknote")
                .font(.system(size: 60))
                .foregroundStyle(.secondary)

            VStack(spacing: 8) {
                Text("No Savings Accounts")
                    .font(.title2)
                    .fontWeight(.bold)

                Text("Create a savings account to start saving for goals")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }

            Button {
                showingAddAccount = true
            } label: {
                Label("Create Savings Account", systemImage: "plus.circle.fill")
                    .fontWeight(.semibold)
                    .frame(maxWidth: .infinity)
                    .padding()
                    .background(DesignSystem.Colors.primary)
                    .foregroundStyle(.white)
                    .clipShape(RoundedRectangle(cornerRadius: 12))
            }
            .padding(.horizontal, 40)
        }
        .padding()
    }
}

// MARK: - Account Card

/// Compact account card for selector
struct AccountCard: View {
    let account: SavingsAccount
    let isSelected: Bool
    let onTap: () -> Void

    var body: some View {
        Button(action: onTap) {
            VStack(alignment: .leading, spacing: 8) {
                Text(account.name)
                    .font(.subheadline)
                    .fontWeight(.medium)
                    .lineLimit(1)

                Text(account.formattedBalance)
                    .font(.title3)
                    .fontWeight(.bold)
                    .fontDesign(.monospaced)
                    .foregroundStyle(isSelected ? .white : DesignSystem.Colors.primary)

                if account.hasTarget, let progress = account.targetProgress {
                    ProgressView(value: progress)
                        .tint(isSelected ? .white : DesignSystem.Colors.primary)
                        .scaleEffect(x: 1, y: 0.8)
                }
            }
            .frame(width: 140)
            .padding()
            .background(isSelected ? DesignSystem.Colors.primary : Color(.systemBackground))
            .foregroundStyle(isSelected ? .white : .primary)
            .clipShape(RoundedRectangle(cornerRadius: 12))
            .overlay(
                RoundedRectangle(cornerRadius: 12)
                    .stroke(isSelected ? Color.clear : Color(.separator), lineWidth: 1)
            )
            .shadow(color: isSelected ? DesignSystem.Colors.primary.opacity(0.3) : .clear,
                   radius: 8, x: 0, y: 4)
        }
        .buttonStyle(.plain)
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

                Text("\(transaction.formattedDate) • \(transaction.createdByName)")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            VStack(alignment: .trailing, spacing: 2) {
                Text(transaction.formattedAmount)
                    .font(.headline)
                    .fontDesign(.monospaced)
                    .foregroundStyle(transaction.isDeposit ? .green : .orange)

                Text("Balance: \(transaction.balanceAfter.currencyFormatted)")
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
