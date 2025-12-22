import SwiftUI

/// Form for creating a new transaction
@MainActor
struct CreateTransactionView: View {

    // MARK: - Properties

    @Environment(\.dismiss) private var dismiss
    var viewModel: TransactionViewModel

    @State private var amount: String = ""
    @State private var transactionType: TransactionType = .credit
    @State private var category: TransactionCategory = .allowance
    @State private var description: String = ""
    @State private var showingConfirmation = false
    @State private var pendingTransaction: PendingTransaction?
    @FocusState private var focusedField: Field?

    // MARK: - Field enum

    private enum Field {
        case amount, description
    }

    // MARK: - Pending Transaction

    private struct PendingTransaction {
        let amount: Decimal
        let type: TransactionType
        let category: String
        let description: String
        let fromSpending: Decimal
        let fromSavings: Decimal
        let intoDebt: Decimal
    }

    // MARK: - Body

    var body: some View {
        NavigationStack {
            Form {
                // Transaction type section
                Section {
                    Picker("Type", selection: $transactionType) {
                        Label("Income", systemImage: "arrow.down.circle.fill")
                            .tag(TransactionType.credit)

                        Label("Spending", systemImage: "arrow.up.circle.fill")
                            .tag(TransactionType.debit)
                    }
                    .pickerStyle(.segmented)
                    .accessibilityIdentifier(AccessibilityIdentifier.transactionTypePicker)
                    .onChange(of: transactionType) { oldValue, newValue in
                        // Reset category when type changes
                        category = newValue == .credit ? .allowance : .toys
                    }
                } header: {
                    Text("Transaction Type")
                }

                // Amount section
                Section {
                    HStack {
                        Text("$")
                            .font(.title3)
                            .foregroundStyle(.secondary)

                        TextField("0.00", text: $amount)
                            .keyboardType(.decimalPad)
                            .font(.title3)
                            .fontDesign(.monospaced)
                            .focused($focusedField, equals: .amount)
                            .accessibilityIdentifier(AccessibilityIdentifier.transactionAmountField)
                    }
                } header: {
                    Text("Amount")
                } footer: {
                    if let amountValue = amountDecimal {
                        Text("Amount: \(amountValue.currencyFormatted)")
                            .foregroundStyle(.secondary)
                    }
                }

                // Category section
                Section {
                    CategoryPicker(
                        selectedCategory: $category,
                        transactionType: transactionType
                    )
                } header: {
                    Text("Category")
                }

                // Description section
                Section {
                    TextField("Enter description", text: $description, axis: .vertical)
                        .lineLimit(3...6)
                        .focused($focusedField, equals: .description)
                        .accessibilityIdentifier(AccessibilityIdentifier.transactionDescriptionField)
                } header: {
                    Text("Description")
                } footer: {
                    Text("Describe what this transaction is for")
                        .foregroundStyle(.secondary)
                }

                // Preview section
                Section {
                    HStack {
                        Image(systemName: transactionType == .credit ? "arrow.down.circle.fill" : "arrow.up.circle.fill")
                            .foregroundStyle(transactionType == .credit ? .green : .red)

                        VStack(alignment: .leading, spacing: 4) {
                            Text(description.isEmpty ? "Transaction" : description)
                                .font(.body)
                                .fontWeight(.medium)

                            Text(category.displayName)
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }

                        Spacer()

                        if let amountValue = amountDecimal {
                            Text(transactionType == .credit ? "+\(amountValue.currencyFormatted)" : "-\(amountValue.currencyFormatted)")
                                .font(.body)
                                .fontWeight(.semibold)
                                .fontDesign(.monospaced)
                                .foregroundStyle(transactionType == .credit ? .green : .red)
                        }
                    }
                } header: {
                    Text("Preview")
                }
            }
            .navigationTitle("New Transaction")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        dismiss()
                    }
                    .accessibilityIdentifier(AccessibilityIdentifier.transactionCancelButton)
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button("Save") {
                        Task {
                            await saveTransaction()
                        }
                    }
                    .disabled(!isValid)
                    .accessibilityIdentifier(AccessibilityIdentifier.transactionSaveButton)
                }

                ToolbarItem(placement: .keyboard) {
                    Button("Done") {
                        focusedField = nil
                    }
                }
            }
            .disabled(viewModel.isCreatingTransaction)
            .overlay {
                if viewModel.isCreatingTransaction {
                    ProgressView("Creating transaction...")
                        .padding()
                        .background(Color(.systemBackground))
                        .clipShape(RoundedRectangle(cornerRadius: 12))
                        .shadow(radius: 4)
                }
            }
            .alert("Confirm Transaction", isPresented: $showingConfirmation, presenting: pendingTransaction) { pending in
                Button("Cancel", role: .cancel) {
                    pendingTransaction = nil
                }
                Button("Confirm", role: .destructive) {
                    Task {
                        await confirmTransaction(pending)
                    }
                }
            } message: { pending in
                Text(confirmationMessage(for: pending))
            }
        }
    }

    // MARK: - Confirmation Message

    private func confirmationMessage(for pending: PendingTransaction) -> String {
        var lines: [String] = []
        lines.append("This transaction will:")

        if pending.fromSpending > 0 {
            lines.append("• Use \(pending.fromSpending.currencyFormatted) from spending")
        }

        if pending.fromSavings > 0 {
            lines.append("• Use \(pending.fromSavings.currencyFormatted) from savings")
        }

        if pending.intoDebt > 0 {
            lines.append("• Go \(pending.intoDebt.currencyFormatted) into debt")
        }

        return lines.joined(separator: "\n")
    }

    // MARK: - Computed Properties

    /// Convert amount string to Decimal
    private var amountDecimal: Decimal? {
        guard !amount.isEmpty else { return nil }
        return Decimal(string: amount)
    }

    /// Check if form is valid
    private var isValid: Bool {
        guard let amountValue = amountDecimal, amountValue > 0 else {
            return false
        }

        guard !description.isEmpty else {
            return false
        }

        return true
    }

    // MARK: - Methods

    /// Save the transaction (checks if confirmation is needed for debits)
    private func saveTransaction() async {
        guard let amountValue = amountDecimal else { return }

        // For debit transactions, check if we need to draw from savings or go into debt
        if transactionType == .debit {
            let impact = viewModel.checkDebitImpact(amount: amountValue)

            if impact.needsConfirmation {
                // Check if transaction is possible
                let totalAvailable = viewModel.currentBalance + viewModel.savingsBalance
                if amountValue > totalAvailable && !viewModel.allowDebt {
                    viewModel.errorMessage = "Insufficient funds. This child has \(totalAvailable.currencyFormatted) available and debt is not allowed."
                    return
                }

                // Store pending transaction and show confirmation
                pendingTransaction = PendingTransaction(
                    amount: amountValue,
                    type: transactionType,
                    category: category.rawValue,
                    description: description,
                    fromSpending: impact.fromSpending,
                    fromSavings: impact.fromSavings,
                    intoDebt: impact.intoDebt
                )
                showingConfirmation = true
                return
            }
        }

        // No confirmation needed, proceed directly
        let success = await viewModel.createTransaction(
            amount: amountValue,
            type: transactionType,
            category: category.rawValue,
            description: description,
            drawFromSavings: false
        )

        if success {
            dismiss()
        }
    }

    /// Confirm and execute a pending transaction that requires drawing from savings
    private func confirmTransaction(_ pending: PendingTransaction) async {
        let success = await viewModel.createTransaction(
            amount: pending.amount,
            type: pending.type,
            category: pending.category,
            description: pending.description,
            drawFromSavings: true
        )

        pendingTransaction = nil

        if success {
            dismiss()
        }
    }
}

// MARK: - Preview Provider

#Preview("Create Transaction") {
    CreateTransactionView(
        viewModel: TransactionViewModel(childId: UUID())
    )
}
