import SwiftUI

/// View for creating a new savings goal
@MainActor
struct AddGoalView: View {

    // MARK: - Properties

    @Bindable var viewModel: SavingsGoalViewModel
    @Environment(\.dismiss) private var dismiss

    @State private var name = ""
    @State private var description = ""
    @State private var targetAmount = ""
    @State private var selectedCategory: GoalCategory = .Toy
    @State private var autoTransferType: AutoTransferType = .None
    @State private var autoTransferAmount = ""
    @State private var autoTransferPercentage = ""

    // MARK: - Body

    var body: some View {
        NavigationStack {
            Form {
                // Basic info
                Section("Goal Details") {
                    TextField("Goal Name", text: $name)

                    TextField("Description (optional)", text: $description)

                    TextField("Target Amount", text: $targetAmount)
                        .keyboardType(.decimalPad)

                    Picker("Category", selection: $selectedCategory) {
                        ForEach(GoalCategory.allCases, id: \.self) { category in
                            Label {
                                Text(category.displayName)
                            } icon: {
                                Text(category.emoji)
                            }
                            .tag(category)
                        }
                    }
                }

                // Auto-transfer settings
                Section {
                    Picker("Auto Transfer", selection: $autoTransferType) {
                        ForEach(AutoTransferType.allCases, id: \.self) { type in
                            Text(type.displayName).tag(type)
                        }
                    }

                    if autoTransferType == .FixedAmount {
                        TextField("Amount per allowance", text: $autoTransferAmount)
                            .keyboardType(.decimalPad)
                    }

                    if autoTransferType == .Percentage {
                        TextField("Percentage of allowance", text: $autoTransferPercentage)
                            .keyboardType(.decimalPad)
                    }
                } header: {
                    Text("Auto Transfer")
                } footer: {
                    Text("Automatically transfer a portion of each allowance payment to this goal.")
                }
            }
            .navigationTitle("New Goal")
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
                            await createGoal()
                        }
                    }
                    .disabled(name.isEmpty || targetAmount.isEmpty || viewModel.isProcessing)
                }
            }
            .disabled(viewModel.isProcessing)
            .overlay {
                if viewModel.isProcessing {
                    ProgressView()
                }
            }
        }
    }

    // MARK: - Methods

    private func createGoal() async {
        guard let target = Decimal(string: targetAmount) else {
            return
        }

        var transferAmount: Decimal?
        var transferPercentage: Decimal?

        if autoTransferType == .FixedAmount {
            transferAmount = Decimal(string: autoTransferAmount)
        } else if autoTransferType == .Percentage {
            transferPercentage = Decimal(string: autoTransferPercentage)
        }

        let success = await viewModel.createGoal(
            name: name,
            description: description.isEmpty ? nil : description,
            targetAmount: target,
            category: selectedCategory,
            autoTransferType: autoTransferType,
            autoTransferAmount: transferAmount,
            autoTransferPercentage: transferPercentage
        )

        if success {
            dismiss()
        }
    }
}

// MARK: - Preview

#Preview("Add Goal View") {
    AddGoalView(viewModel: SavingsGoalViewModel(
        childId: UUID(),
        isParent: true,
        currentBalance: 100
    ))
}
