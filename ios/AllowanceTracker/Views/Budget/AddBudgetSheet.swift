import SwiftUI

/// Sheet for creating or editing a budget
@MainActor
struct AddBudgetSheet: View {
    @Environment(\.dismiss) private var dismiss
    @State private var viewModel = BudgetViewModel()

    let childId: UUID
    var existingBudget: CategoryBudget?

    @State private var selectedCategory: TransactionCategory = .toys
    @State private var limit: String = "50.00"
    @State private var period: BudgetPeriod = .weekly
    @State private var alertThreshold: Int = 80
    @State private var enforceLimit = false
    @State private var isSaving = false

    private var isEditing: Bool {
        existingBudget != nil
    }

    private var limitDecimal: Decimal {
        Decimal(string: limit) ?? 0
    }

    private var isValid: Bool {
        limitDecimal > 0
    }

    var body: some View {
        NavigationStack {
            Form {
                // Category Selection
                Section("Budget Details") {
                    if !isEditing {
                        Picker("Category", selection: $selectedCategory) {
                            ForEach(TransactionCategory.spendingCategories) { category in
                                Label {
                                    Text(category.displayName)
                                } icon: {
                                    Image(systemName: category.icon)
                                        .foregroundStyle(Color.green600)
                                }
                                .tag(category)
                            }
                        }
                        .pickerStyle(.menu)
                    } else {
                        // Show selected category (read-only when editing)
                        HStack {
                            Label {
                                Text(selectedCategory.displayName)
                            } icon: {
                                Image(systemName: selectedCategory.icon)
                                    .foregroundStyle(Color.green600)
                            }
                            Spacer()
                            Text("Category")
                                .foregroundStyle(.secondary)
                                .font(.caption)
                        }
                    }

                    // Limit Input
                    HStack {
                        Text("Limit")
                        Spacer()
                        TextField("Amount", text: $limit)
                            .keyboardType(.decimalPad)
                            .multilineTextAlignment(.trailing)
                            .fontDesign(.monospaced)
                            .frame(maxWidth: 120)
                    }

                    // Period Selection
                    Picker("Period", selection: $period) {
                        ForEach(BudgetPeriod.allCases) { periodOption in
                            Label {
                                Text(periodOption.rawValue)
                            } icon: {
                                Image(systemName: periodOption.icon)
                                    .foregroundStyle(Color.green600)
                            }
                            .tag(periodOption)
                        }
                    }
                }

                // Alert Settings
                Section("Alert Settings") {
                    Stepper(
                        "Alert at \(alertThreshold)% used",
                        value: $alertThreshold,
                        in: 50...95,
                        step: 5
                    )

                    Toggle("Enforce Limit", isOn: $enforceLimit)
                }

                // Help Text
                Section {
                    if enforceLimit {
                        Label {
                            Text("Transactions will be blocked when this budget is exceeded")
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        } icon: {
                            Image(systemName: "lock.fill")
                                .foregroundStyle(.red)
                        }
                    } else {
                        Label {
                            Text("Warnings will be shown, but transactions are allowed")
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        } icon: {
                            Image(systemName: "exclamationmark.triangle.fill")
                                .foregroundStyle(.orange)
                        }
                    }
                }

                // Preview Section
                if isValid {
                    Section("Preview") {
                        HStack {
                            VStack(alignment: .leading, spacing: 4) {
                                Text(selectedCategory.displayName)
                                    .font(.headline)
                                Text(period.rawValue)
                                    .font(.caption)
                                    .foregroundStyle(.secondary)
                            }

                            Spacer()

                            Text(limitDecimal.currencyFormatted)
                                .font(.title3)
                                .fontWeight(.semibold)
                                .fontDesign(.monospaced)
                        }
                    }
                }
            }
            .navigationTitle(isEditing ? "Edit Budget" : "Add Budget")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        dismiss()
                    }
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button(isEditing ? "Update" : "Save") {
                        Task {
                            await saveBudget()
                        }
                    }
                    .disabled(!isValid || isSaving)
                }
            }
            .disabled(isSaving)
            .overlay {
                if isSaving {
                    ProgressView()
                        .scaleEffect(1.5)
                        .frame(maxWidth: .infinity, maxHeight: .infinity)
                        .background(Color.black.opacity(0.2))
                }
            }
        }
        .onAppear {
            if let budget = existingBudget {
                selectedCategory = budget.category
                limit = String(describing: budget.limit)
                period = budget.period
                alertThreshold = budget.alertThresholdPercent
                enforceLimit = budget.enforceLimit
            }
        }
    }

    private func saveBudget() async {
        isSaving = true

        let request = SetBudgetRequest(
            childId: childId,
            category: selectedCategory,
            limit: limitDecimal,
            period: period,
            alertThresholdPercent: alertThreshold,
            enforceLimit: enforceLimit
        )

        let success = isEditing
            ? await viewModel.updateBudget(request)
            : await viewModel.createBudget(request)

        isSaving = false

        if success {
            dismiss()
        }
    }
}

// MARK: - Preview Provider

#Preview("Add Budget") {
    AddBudgetSheet(childId: UUID(), existingBudget: nil)
}

#Preview("Edit Budget") {
    AddBudgetSheet(
        childId: UUID(),
        existingBudget: CategoryBudget(
            id: UUID(),
            childId: UUID(),
            category: .toys,
            limit: 50.00,
            period: .weekly,
            alertThresholdPercent: 80,
            enforceLimit: true,
            createdAt: Date(),
            updatedAt: Date()
        )
    )
}

#Preview("With Enforcement") {
    AddBudgetSheet(
        childId: UUID(),
        existingBudget: CategoryBudget(
            id: UUID(),
            childId: UUID(),
            category: .snacks,
            limit: 25.00,
            period: .monthly,
            alertThresholdPercent: 75,
            enforceLimit: true,
            createdAt: Date(),
            updatedAt: Date()
        )
    )
}
