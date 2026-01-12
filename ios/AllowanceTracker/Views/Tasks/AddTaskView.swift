import SwiftUI

/// View for adding a new task
@MainActor
struct AddTaskView: View {

    // MARK: - Properties

    @Bindable var viewModel: TaskViewModel
    @Environment(\.dismiss) private var dismiss

    @State private var title = ""
    @State private var description = ""
    @State private var rewardAmount = ""
    @State private var isRecurring = false
    @State private var recurrenceType: RecurrenceType = .Weekly
    @State private var recurrenceDay: Weekday = .monday
    @State private var recurrenceDayOfMonth = 1

    // MARK: - Body

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Task Title", text: $title)

                    TextField("Description (optional)", text: $description, axis: .vertical)
                        .lineLimit(2...4)
                }

                Section {
                    HStack {
                        Text("$")
                        TextField("Reward Amount", text: $rewardAmount)
                            .keyboardType(.decimalPad)
                    }
                } header: {
                    Text("Reward")
                }

                Section {
                    Toggle("Recurring Task", isOn: $isRecurring)

                    if isRecurring {
                        Picker("Frequency", selection: $recurrenceType) {
                            ForEach(RecurrenceType.allCases, id: \.self) { type in
                                Text(type.displayName).tag(type)
                            }
                        }

                        if recurrenceType == .Weekly {
                            Picker("Day of Week", selection: $recurrenceDay) {
                                ForEach(Weekday.allCases, id: \.self) { day in
                                    Text(day.rawValue).tag(day)
                                }
                            }
                        }

                        if recurrenceType == .Monthly {
                            Stepper("Day \(recurrenceDayOfMonth)", value: $recurrenceDayOfMonth, in: 1...28)
                        }
                    }
                } header: {
                    Text("Schedule")
                } footer: {
                    if isRecurring {
                        Text(recurrenceDescription)
                    }
                }
            }
            .navigationTitle("Add Task")
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
                            await createTask()
                        }
                    }
                    .disabled(!isValid || viewModel.isProcessing)
                }
            }
            .overlay {
                if viewModel.isProcessing {
                    ProgressView()
                        .padding()
                        .background(.regularMaterial)
                        .clipShape(RoundedRectangle(cornerRadius: 8))
                }
            }
            .alert("Error", isPresented: .constant(viewModel.errorMessage != nil)) {
                Button("OK") {
                    viewModel.clearError()
                }
            } message: {
                if let error = viewModel.errorMessage {
                    Text(error)
                }
            }
        }
    }

    // MARK: - Computed Properties

    private var isValid: Bool {
        !title.isEmpty && (Decimal(string: rewardAmount) ?? 0) > 0
    }

    private var recurrenceDescription: String {
        switch recurrenceType {
        case .Daily:
            return "Task will appear every day"
        case .Weekly:
            return "Task will appear every \(recurrenceDay.rawValue)"
        case .Monthly:
            return "Task will appear on day \(recurrenceDayOfMonth) of each month"
        }
    }

    // MARK: - Methods

    private func createTask() async {
        let amount = Decimal(string: rewardAmount) ?? 0

        let success = await viewModel.createTask(
            title: title,
            description: description.isEmpty ? nil : description,
            rewardAmount: amount,
            isRecurring: isRecurring,
            recurrenceType: isRecurring ? recurrenceType : nil,
            recurrenceDay: isRecurring && recurrenceType == .Weekly ? recurrenceDay : nil,
            recurrenceDayOfMonth: isRecurring && recurrenceType == .Monthly ? recurrenceDayOfMonth : nil
        )

        if success {
            dismiss()
        }
    }
}

// MARK: - Preview

#Preview {
    AddTaskView(viewModel: TaskViewModel(childId: UUID(), isParent: true))
}
