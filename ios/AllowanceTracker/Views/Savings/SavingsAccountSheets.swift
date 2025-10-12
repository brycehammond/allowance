import SwiftUI

// MARK: - Add Savings Account Sheet

struct AddSavingsAccountSheet: View {
    @ObservedObject var viewModel: SavingsAccountViewModel
    @Environment(\.dismiss) private var dismiss

    @State private var name = ""
    @State private var hasTarget = false
    @State private var targetAmount = ""
    @State private var autoTransferEnabled = false
    @State private var autoTransferPercentage = "10"
    @State private var isProcessing = false

    var body: some View {
        NavigationStack {
            Form {
                Section("Account Details") {
                    TextField("Account Name", text: $name)
                        .autocorrectionDisabled()

                    Toggle("Set Savings Goal", isOn: $hasTarget)

                    if hasTarget {
                        TextField("Target Amount", text: $targetAmount)
                            .keyboardType(.decimalPad)
                            .fontDesign(.monospaced)
                    }
                }

                Section {
                    Toggle("Auto-Transfer from Allowance", isOn: $autoTransferEnabled)

                    if autoTransferEnabled {
                        HStack {
                            TextField("Percentage", text: $autoTransferPercentage)
                                .keyboardType(.numberPad)
                                .fontDesign(.monospaced)
                            Text("%")
                        }
                    }
                } header: {
                    Text("Auto-Transfer")
                } footer: {
                    if autoTransferEnabled {
                        Text("Automatically transfer \(autoTransferPercentage)% of allowance to this savings account")
                    }
                }
            }
            .navigationTitle("New Savings Account")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        dismiss()
                    }
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button("Create") {
                        Task {
                            await createAccount()
                        }
                    }
                    .disabled(isProcessing || name.isEmpty)
                }
            }
        }
    }

    private func createAccount() async {
        isProcessing = true

        let target: Decimal? = hasTarget ? Decimal(string: targetAmount) : nil
        let percentage: Decimal? = autoTransferEnabled ? Decimal(string: autoTransferPercentage) : nil

        let success = await viewModel.createAccount(
            name: name,
            targetAmount: target,
            autoTransferEnabled: autoTransferEnabled,
            autoTransferPercentage: percentage
        )

        isProcessing = false

        if success {
            dismiss()
        }
    }
}

// MARK: - Edit Savings Account Sheet

struct EditSavingsAccountSheet: View {
    @ObservedObject var viewModel: SavingsAccountViewModel
    let account: SavingsAccount
    @Environment(\.dismiss) private var dismiss

    @State private var name: String
    @State private var hasTarget: Bool
    @State private var targetAmount: String
    @State private var autoTransferEnabled: Bool
    @State private var autoTransferPercentage: String
    @State private var isProcessing = false
    @State private var showingDeleteConfirmation = false

    init(viewModel: SavingsAccountViewModel, account: SavingsAccount) {
        self.viewModel = viewModel
        self.account = account
        _name = State(initialValue: account.name)
        _hasTarget = State(initialValue: account.hasTarget)
        _targetAmount = State(initialValue: account.targetAmount?.description ?? "")
        _autoTransferEnabled = State(initialValue: account.autoTransferEnabled)
        _autoTransferPercentage = State(initialValue: account.autoTransferPercentage?.description ?? "10")
    }

    var body: some View {
        NavigationStack {
            Form {
                Section("Account Details") {
                    TextField("Account Name", text: $name)
                        .autocorrectionDisabled()

                    HStack {
                        Text("Current Balance")
                        Spacer()
                        Text(account.formattedBalance)
                            .fontDesign(.monospaced)
                            .foregroundStyle(.secondary)
                    }

                    Toggle("Set Savings Goal", isOn: $hasTarget)

                    if hasTarget {
                        TextField("Target Amount", text: $targetAmount)
                            .keyboardType(.decimalPad)
                            .fontDesign(.monospaced)
                    }
                }

                Section {
                    Toggle("Auto-Transfer from Allowance", isOn: $autoTransferEnabled)

                    if autoTransferEnabled {
                        HStack {
                            TextField("Percentage", text: $autoTransferPercentage)
                                .keyboardType(.numberPad)
                                .fontDesign(.monospaced)
                            Text("%")
                        }
                    }
                } header: {
                    Text("Auto-Transfer")
                }

                Section {
                    Button(role: .destructive) {
                        showingDeleteConfirmation = true
                    } label: {
                        Label("Delete Account", systemImage: "trash")
                    }
                }
            }
            .navigationTitle("Edit Account")
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
                            await updateAccount()
                        }
                    }
                    .disabled(isProcessing || name.isEmpty)
                }
            }
            .confirmationDialog(
                "Delete Account",
                isPresented: $showingDeleteConfirmation,
                titleVisibility: .visible
            ) {
                Button("Delete", role: .destructive) {
                    Task {
                        await deleteAccount()
                    }
                }
                Button("Cancel", role: .cancel) {}
            } message: {
                Text("This will permanently delete \(account.name) and all its transactions. This cannot be undone.")
            }
        }
    }

    private func updateAccount() async {
        isProcessing = true

        let target: Decimal? = hasTarget ? Decimal(string: targetAmount) : nil
        let percentage: Decimal? = autoTransferEnabled ? Decimal(string: autoTransferPercentage) : nil

        let success = await viewModel.updateAccount(
            id: account.id,
            name: name,
            targetAmount: target,
            autoTransferEnabled: autoTransferEnabled,
            autoTransferPercentage: percentage
        )

        isProcessing = false

        if success {
            dismiss()
        }
    }

    private func deleteAccount() async {
        let success = await viewModel.deleteAccount(id: account.id)

        if success {
            dismiss()
        }
    }
}

// MARK: - Deposit Sheet

struct DepositSheet: View {
    @ObservedObject var viewModel: SavingsAccountViewModel
    let account: SavingsAccount
    @Environment(\.dismiss) private var dismiss

    @State private var amount = ""
    @State private var notes = ""
    @State private var isProcessing = false

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    HStack {
                        Text("Current Balance")
                        Spacer()
                        Text(account.formattedBalance)
                            .fontDesign(.monospaced)
                            .fontWeight(.semibold)
                    }

                    if account.hasTarget {
                        HStack {
                            Text("Goal")
                            Spacer()
                            Text(account.formattedTargetAmount)
                                .fontDesign(.monospaced)
                                .foregroundStyle(.secondary)
                        }
                    }
                }

                Section("Deposit Amount") {
                    TextField("Amount", text: $amount)
                        .keyboardType(.decimalPad)
                        .fontDesign(.monospaced)
                        .font(.title2)

                    if let depositAmount = Decimal(string: amount), depositAmount > 0 {
                        HStack {
                            Text("New Balance")
                            Spacer()
                            Text((account.currentBalance + depositAmount).currencyFormatted)
                                .fontDesign(.monospaced)
                                .fontWeight(.semibold)
                                .foregroundStyle(.green)
                        }
                    }
                }

                Section("Notes (Optional)") {
                    TextField("Add a note", text: $notes, axis: .vertical)
                        .lineLimit(3...5)
                }

                // Quick amount buttons
                Section {
                    LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible()), GridItem(.flexible())], spacing: 12) {
                        QuickAmountButton(amount: 5, currentAmount: $amount)
                        QuickAmountButton(amount: 10, currentAmount: $amount)
                        QuickAmountButton(amount: 20, currentAmount: $amount)
                        QuickAmountButton(amount: 50, currentAmount: $amount)
                        QuickAmountButton(amount: 100, currentAmount: $amount)
                    }
                } header: {
                    Text("Quick Amounts")
                }
            }
            .navigationTitle("Deposit to \(account.name)")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        dismiss()
                    }
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button("Deposit") {
                        Task {
                            await deposit()
                        }
                    }
                    .disabled(isProcessing || amount.isEmpty || Decimal(string: amount) == nil)
                }
            }
        }
    }

    private func deposit() async {
        guard let depositAmount = Decimal(string: amount) else { return }

        isProcessing = true

        let success = await viewModel.deposit(
            accountId: account.id,
            amount: depositAmount,
            notes: notes.isEmpty ? nil : notes
        )

        isProcessing = false

        if success {
            dismiss()
        }
    }
}

// MARK: - Withdraw Sheet

struct WithdrawSheet: View {
    @ObservedObject var viewModel: SavingsAccountViewModel
    let account: SavingsAccount
    @Environment(\.dismiss) private var dismiss

    @State private var amount = ""
    @State private var notes = ""
    @State private var isProcessing = false

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    HStack {
                        Text("Current Balance")
                        Spacer()
                        Text(account.formattedBalance)
                            .fontDesign(.monospaced)
                            .fontWeight(.semibold)
                    }
                }

                Section("Withdrawal Amount") {
                    TextField("Amount", text: $amount)
                        .keyboardType(.decimalPad)
                        .fontDesign(.monospaced)
                        .font(.title2)

                    if let withdrawAmount = Decimal(string: amount), withdrawAmount > 0 {
                        let newBalance = account.currentBalance - withdrawAmount

                        HStack {
                            Text("New Balance")
                            Spacer()
                            Text(newBalance.currencyFormatted)
                                .fontDesign(.monospaced)
                                .fontWeight(.semibold)
                                .foregroundStyle(newBalance < 0 ? .red : .orange)
                        }

                        if newBalance < 0 {
                            Label("Insufficient balance", systemImage: "exclamationmark.triangle.fill")
                                .font(.caption)
                                .foregroundStyle(.red)
                        }
                    }
                }

                Section("Notes (Optional)") {
                    TextField("Add a note", text: $notes, axis: .vertical)
                        .lineLimit(3...5)
                }

                // Quick amount buttons
                Section {
                    LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible()), GridItem(.flexible())], spacing: 12) {
                        QuickAmountButton(amount: 5, currentAmount: $amount)
                        QuickAmountButton(amount: 10, currentAmount: $amount)
                        QuickAmountButton(amount: 20, currentAmount: $amount)

                        // All balance button
                        Button {
                            amount = account.currentBalance.description
                        } label: {
                            Text("All")
                                .font(.subheadline)
                                .fontWeight(.medium)
                                .frame(maxWidth: .infinity)
                                .padding(.vertical, 8)
                                .background(Color.orange.opacity(0.1))
                                .foregroundStyle(.orange)
                                .clipShape(RoundedRectangle(cornerRadius: 8))
                        }
                    }
                } header: {
                    Text("Quick Amounts")
                }
            }
            .navigationTitle("Withdraw from \(account.name)")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        dismiss()
                    }
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button("Withdraw") {
                        Task {
                            await withdraw()
                        }
                    }
                    .disabled(isProcessing || amount.isEmpty || Decimal(string: amount) == nil)
                }
            }
        }
    }

    private func withdraw() async {
        guard let withdrawAmount = Decimal(string: amount) else { return }

        isProcessing = true

        let success = await viewModel.withdraw(
            accountId: account.id,
            amount: withdrawAmount,
            notes: notes.isEmpty ? nil : notes
        )

        isProcessing = false

        if success {
            dismiss()
        }
    }
}

// MARK: - Quick Amount Button

struct QuickAmountButton: View {
    let amount: Int
    @Binding var currentAmount: String

    var body: some View {
        Button {
            currentAmount = String(amount)
        } label: {
            Text("$\(amount)")
                .font(.subheadline)
                .fontWeight(.medium)
                .frame(maxWidth: .infinity)
                .padding(.vertical, 8)
                .background(DesignSystem.Colors.primary.opacity(0.1))
                .foregroundStyle(DesignSystem.Colors.primary)
                .clipShape(RoundedRectangle(cornerRadius: 8))
        }
    }
}

// MARK: - Preview Provider

#Preview("Add Account") {
    AddSavingsAccountSheet(viewModel: SavingsAccountViewModel(childId: UUID()))
}
