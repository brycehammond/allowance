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

    // Form fields
    @State private var weeklyAllowance: String = ""
    @State private var selectedAllowanceDay: Weekday? = nil
    @State private var useScheduledDay: Bool = false

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
                Section("Allowance Settings") {
                    // Weekly allowance amount
                    HStack {
                        Text("Weekly Allowance")
                        Spacer()
                        TextField("Amount", text: $weeklyAllowance)
                            .keyboardType(.decimalPad)
                            .multilineTextAlignment(.trailing)
                            .frame(width: 100)
                    }

                    // Allowance day toggle
                    Toggle("Schedule Specific Day", isOn: $useScheduledDay)
                        .onChange(of: useScheduledDay) { oldValue, newValue in
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

                        Text("Allowance will be paid every \(selectedAllowanceDay?.rawValue ?? ""). If disabled, allowance is paid 7 days after the last payment.")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    } else {
                        Text("Allowance will be paid 7 days after the last payment (rolling window).")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }

                // Current status section
                Section("Current Status") {
                    HStack {
                        Text("Current Balance")
                        Spacer()
                        Text(child.formattedBalance)
                            .fontDesign(.monospaced)
                            .fontWeight(.semibold)
                    }

                    if let lastDate = child.lastAllowanceDate {
                        HStack {
                            Text("Last Allowance")
                            Spacer()
                            Text(lastDate.formattedDisplay)
                                .foregroundStyle(.secondary)
                        }
                    }

                    HStack {
                        Text("Current Schedule")
                        Spacer()
                        Text(child.allowanceDayDisplay)
                            .foregroundStyle(.secondary)
                    }
                }

                // Save button
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
        }
        .navigationTitle("Child Settings")
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

    // MARK: - Computed Properties

    private var isFormValid: Bool {
        guard let amount = Decimal(string: weeklyAllowance) else {
            return false
        }
        return amount >= 0 && amount <= 10000
    }

    // MARK: - Methods

    private func loadChild() async {
        isLoading = true
        errorMessage = nil

        do {
            let loadedChild = try await apiService.getChild(id: childId)
            child = loadedChild

            // Initialize form fields
            weeklyAllowance = String(describing: loadedChild.weeklyAllowance)
            selectedAllowanceDay = loadedChild.allowanceDay
            useScheduledDay = loadedChild.allowanceDay != nil
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
                savingsAccountEnabled: false,
                savingsTransferType: .none,
                savingsTransferPercentage: nil,
                savingsTransferAmount: nil,
                allowanceDay: useScheduledDay ? selectedAllowanceDay : nil
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
