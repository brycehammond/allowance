import SwiftUI

/// Form for creating a new transaction
struct CreateTransactionView: View {

    // MARK: - Properties

    @Environment(\.dismiss) private var dismiss
    @ObservedObject var viewModel: TransactionViewModel

    @State private var amount: String = ""
    @State private var transactionType: TransactionType = .credit
    @State private var category: TransactionCategory = .allowance
    @State private var description: String = ""
    @FocusState private var focusedField: Field?

    // MARK: - Field enum

    private enum Field {
        case amount, description
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
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button("Save") {
                        Task {
                            await saveTransaction()
                        }
                    }
                    .disabled(!isValid)
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
        }
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

    /// Save the transaction
    private func saveTransaction() async {
        guard let amountValue = amountDecimal else { return }

        let success = await viewModel.createTransaction(
            amount: amountValue,
            type: transactionType,
            category: category.rawValue,
            description: description
        )

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
