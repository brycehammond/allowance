import SwiftUI

/// View for managing child settings (Parent only)
struct ChildSettingsView: View {

    // MARK: - Properties

    let childId: UUID
    let apiService: APIServiceProtocol

    @State private var child: Child?
    @State private var isLoading = false
    @State private var isSaving = false
    @State private var errorMessage: String?
    @State private var showSuccessAlert = false

    // Allowance form fields
    @State private var weeklyAllowance: String = ""
    @State private var selectedAllowanceDay: Weekday? = nil
    @State private var useScheduledDay: Bool = false

    // Savings form fields
    @State private var savingsAccountEnabled: Bool = false
    @State private var savingsTransferType: SavingsTransferType = .percentage
    @State private var savingsTransferPercentage: String = "20"
    @State private var savingsTransferAmount: String = "2.00"
    @State private var savingsBalanceVisibleToChild: Bool = true

    // Spending form fields
    @State private var allowDebt: Bool = false

    // MARK: - Body

    var body: some View {
        Form {
            if isLoading {
                Section {
                    HStack {
                        Spacer()
                        ProgressView()
                        Spacer()
                    }
                }
            } else if let child = child {
                // Allowance section
                allowanceSettingsSection

                // Savings section
                savingsSettingsSection

                // Savings transfer settings
                if savingsAccountEnabled {
                    savingsTransferSection
                }

                // Spending settings section
                spendingSettingsSection

                // Current status section
                currentStatusSection(child: child)

                // Save button
                saveButtonSection
            }
        }
        .navigationTitle("Settings")
        .navigationBarTitleDisplayMode(.inline)
        .task {
            await loadChild()
        }
        .alert("Success", isPresented: $showSuccessAlert) {
            Button("OK", role: .cancel) { }
        } message: {
            Text("Settings updated successfully!")
        }
        .alert("Error", isPresented: .constant(errorMessage != nil)) {
            Button("OK") {
                errorMessage = nil
            }
        } message: {
            if let errorMessage = errorMessage {
                Text(errorMessage)
            }
        }
    }

    // MARK: - View Components

    private var allowanceSettingsSection: some View {
        Section("Allowance Settings") {
            // Weekly allowance amount
            HStack {
                Text("Weekly Allowance")
                Spacer()
                Text("$")
                    .foregroundStyle(.secondary)
                TextField("Amount", text: $weeklyAllowance)
                    .keyboardType(.decimalPad)
                    .multilineTextAlignment(.trailing)
                    .frame(width: 80)
            }

            // Allowance day toggle
            Toggle("Schedule Specific Day", isOn: $useScheduledDay)
                .onChange(of: useScheduledDay) { _, newValue in
                    if !newValue {
                        selectedAllowanceDay = nil
                    } else if selectedAllowanceDay == nil {
                        selectedAllowanceDay = .friday
                    }
                }

            // Day picker (only shown if toggle is on)
            if useScheduledDay {
                Picker("Allowance Day", selection: $selectedAllowanceDay) {
                    ForEach(Weekday.allCases, id: \.self) { day in
                        Text(day.rawValue).tag(day as Weekday?)
                    }
                }
                .pickerStyle(.menu)
            }

            Text(useScheduledDay
                 ? "Allowance will be paid every \(selectedAllowanceDay?.rawValue ?? "Friday")"
                 : "Allowance is paid 7 days after the last payment")
                .font(.caption)
                .foregroundStyle(.secondary)
        }
    }

    private var savingsSettingsSection: some View {
        Section {
            Toggle(isOn: $savingsAccountEnabled) {
                HStack {
                    Image(systemName: "banknote")
                        .foregroundStyle(DesignSystem.Colors.primary)
                    VStack(alignment: .leading) {
                        Text("Automatic Savings")
                            .font(.body)
                        Text("Transfer part of each allowance to savings")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
            }
        } header: {
            Text("Savings Account")
        }
    }

    private var savingsTransferSection: some View {
        Section("Transfer Settings") {
            // Transfer type picker
            Picker("Transfer Type", selection: $savingsTransferType) {
                Text("Percentage").tag(SavingsTransferType.percentage)
                Text("Fixed Amount").tag(SavingsTransferType.fixedAmount)
            }
            .pickerStyle(.segmented)

            // Percentage input
            if savingsTransferType == .percentage {
                HStack {
                    Text("Savings Percentage")
                    Spacer()
                    TextField("", text: $savingsTransferPercentage)
                        .keyboardType(.numberPad)
                        .multilineTextAlignment(.trailing)
                        .frame(width: 60)
                    Text("%")
                        .foregroundStyle(.secondary)
                }
            } else {
                // Fixed amount input
                HStack {
                    Text("Savings Amount")
                    Spacer()
                    Text("$")
                        .foregroundStyle(.secondary)
                    TextField("", text: $savingsTransferAmount)
                        .keyboardType(.decimalPad)
                        .multilineTextAlignment(.trailing)
                        .frame(width: 80)
                }
            }

            // Weekly breakdown preview
            if let preview = weeklyBreakdownPreview {
                VStack(alignment: .leading, spacing: 8) {
                    Text("Weekly Breakdown Preview")
                        .font(.caption)
                        .foregroundStyle(.secondary)

                    HStack {
                        VStack(alignment: .leading) {
                            Text("To Spending")
                                .font(.caption)
                                .foregroundStyle(.secondary)
                            Text(preview.spending)
                                .font(.headline)
                        }
                        Spacer()
                        VStack(alignment: .trailing) {
                            Text("To Savings")
                                .font(.caption)
                                .foregroundStyle(.secondary)
                            Text(preview.savings)
                                .font(.headline)
                                .foregroundStyle(DesignSystem.Colors.primary)
                        }
                    }
                }
                .padding(.vertical, 8)
            }

            // Visibility toggle
            Toggle(isOn: $savingsBalanceVisibleToChild) {
                HStack {
                    Image(systemName: savingsBalanceVisibleToChild ? "eye" : "eye.slash")
                        .foregroundStyle(savingsBalanceVisibleToChild ? DesignSystem.Colors.primary : .secondary)
                    VStack(alignment: .leading) {
                        Text("Show Savings to Child")
                            .font(.body)
                        Text(savingsBalanceVisibleToChild
                             ? "Child can see their savings balance"
                             : "Savings balance is hidden from child")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
            }
        }
    }

    private var spendingSettingsSection: some View {
        Section {
            Toggle(isOn: $allowDebt) {
                HStack {
                    Image(systemName: "creditcard")
                        .foregroundStyle(allowDebt ? DesignSystem.Colors.secondary : .secondary)
                    VStack(alignment: .leading) {
                        Text("Allow Debt")
                            .font(.body)
                        Text(allowDebt
                             ? "Child can spend more than their balance"
                             : "Transactions blocked if balance is insufficient")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
            }
            .tint(DesignSystem.Colors.secondary)

            if allowDebt {
                Text("When spending exceeds available funds, savings will be used first before going into debt.")
                    .font(.caption)
                    .foregroundStyle(DesignSystem.Colors.secondary)
                    .padding(.vertical, 4)
            }
        } header: {
            Text("Spending Settings")
        }
    }

    private func currentStatusSection(child: Child) -> some View {
        Section("Current Status") {
            HStack {
                Text("Spending Balance")
                Spacer()
                Text(child.formattedBalance)
                    .fontDesign(.monospaced)
                    .fontWeight(.semibold)
            }

            HStack {
                Text("Savings Balance")
                Spacer()
                Text(child.formattedSavingsBalance)
                    .fontDesign(.monospaced)
                    .fontWeight(.semibold)
                    .foregroundStyle(DesignSystem.Colors.primary)
            }

            HStack {
                Text("Total Balance")
                Spacer()
                Text(child.formattedTotalBalance)
                    .fontDesign(.monospaced)
                    .fontWeight(.bold)
            }

            if let lastDate = child.lastAllowanceDate {
                HStack {
                    Text("Last Allowance")
                    Spacer()
                    Text(lastDate.formattedDisplay)
                        .foregroundStyle(.secondary)
                }
            }
        }
    }

    private var saveButtonSection: some View {
        Section {
            Button {
                Task {
                    await saveSettings()
                }
            } label: {
                HStack {
                    Spacer()
                    if isSaving {
                        ProgressView()
                            .padding(.trailing, 8)
                    }
                    Text("Save Changes")
                        .fontWeight(.semibold)
                    Spacer()
                }
            }
            .disabled(isSaving || !isFormValid)
        }
    }

    // MARK: - Computed Properties

    private var isFormValid: Bool {
        guard let amount = Decimal(string: weeklyAllowance) else {
            return false
        }
        return amount >= 0 && amount <= 10000
    }

    private var weeklyBreakdownPreview: (spending: String, savings: String)? {
        guard let allowance = Decimal(string: weeklyAllowance), allowance > 0 else {
            return nil
        }

        let savingsAmount: Decimal
        if savingsTransferType == .percentage {
            let percentage = Decimal(string: savingsTransferPercentage) ?? 0
            savingsAmount = allowance * percentage / 100
        } else {
            let amount = Decimal(string: savingsTransferAmount) ?? 0
            savingsAmount = min(amount, allowance)
        }

        let spendingAmount = allowance - savingsAmount

        return (
            spending: spendingAmount.currencyFormatted,
            savings: savingsAmount.currencyFormatted
        )
    }

    // MARK: - Methods

    private func loadChild() async {
        isLoading = true
        errorMessage = nil

        do {
            let loadedChild = try await apiService.getChild(id: childId)
            child = loadedChild

            // Initialize allowance form fields
            weeklyAllowance = String(describing: loadedChild.weeklyAllowance)
            selectedAllowanceDay = loadedChild.allowanceDay
            useScheduledDay = loadedChild.allowanceDay != nil

            // Initialize savings form fields
            savingsAccountEnabled = loadedChild.savingsAccountEnabled
            savingsTransferType = loadedChild.savingsTransferType
            if let percentage = loadedChild.savingsTransferPercentage {
                savingsTransferPercentage = String(describing: percentage)
            }
            if let amount = loadedChild.savingsTransferAmount {
                savingsTransferAmount = String(describing: amount)
            }
            savingsBalanceVisibleToChild = loadedChild.savingsBalanceVisibleToChild
            allowDebt = loadedChild.allowDebt
        } catch {
            errorMessage = "Failed to load child settings"
        }

        isLoading = false
    }

    private func saveSettings() async {
        guard let amount = Decimal(string: weeklyAllowance) else {
            errorMessage = "Invalid allowance amount"
            return
        }

        isSaving = true
        errorMessage = nil

        do {
            let request = UpdateChildSettingsRequest(
                weeklyAllowance: amount,
                savingsAccountEnabled: savingsAccountEnabled,
                savingsTransferType: savingsAccountEnabled ? savingsTransferType : .none,
                savingsTransferPercentage: savingsTransferType == .percentage ? Decimal(string: savingsTransferPercentage) : nil,
                savingsTransferAmount: savingsTransferType == .fixedAmount ? Decimal(string: savingsTransferAmount) : nil,
                allowanceDay: useScheduledDay ? selectedAllowanceDay : nil,
                savingsBalanceVisibleToChild: savingsBalanceVisibleToChild,
                allowDebt: allowDebt
            )

            _ = try await apiService.updateChildSettings(childId: childId, request)

            // Reload child to show updated values
            await loadChild()

            showSuccessAlert = true
        } catch {
            errorMessage = "Failed to save settings"
        }

        isSaving = false
    }
}

// MARK: - Preview Provider

#Preview("Child Settings") {
    NavigationStack {
        ChildSettingsView(
            childId: UUID(),
            apiService: APIService()
        )
    }
}
