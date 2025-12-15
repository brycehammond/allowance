import SwiftUI

/// View for adding a new child account (Parent only)
struct AddChildView: View {

    // MARK: - Properties

    @Environment(\.dismiss) private var dismiss
    @State private var viewModel: AddChildViewModel

    // MARK: - Initialization

    init(apiService: APIServiceProtocol = APIService.shared) {
        _viewModel = State(wrappedValue: AddChildViewModel(apiService: apiService))
    }

    // MARK: - Body

    var body: some View {
        NavigationStack {
            Form {
                // Personal Information Section
                Section("Personal Information") {
                    TextField("First Name", text: $viewModel.firstName)
                        .textContentType(.givenName)
                        .autocapitalization(.words)

                    TextField("Last Name", text: $viewModel.lastName)
                        .textContentType(.familyName)
                        .autocapitalization(.words)

                    TextField("Email Address", text: $viewModel.email)
                        .textContentType(.emailAddress)
                        .autocapitalization(.none)
                        .keyboardType(.emailAddress)

                    SecureField("Password", text: $viewModel.password)
                        .textContentType(.newPassword)

                    SecureField("Confirm Password", text: $viewModel.confirmPassword)
                        .textContentType(.newPassword)
                }

                // Allowance Settings Section
                Section("Allowance Settings") {
                    HStack {
                        Text("Weekly Allowance")
                        Spacer()
                        TextField("Amount", text: $viewModel.weeklyAllowance)
                            .keyboardType(.decimalPad)
                            .multilineTextAlignment(.trailing)
                            .frame(width: 100)
                    }

                    HStack {
                        Text("Initial Balance")
                        Spacer()
                        TextField("Amount", text: $viewModel.initialBalance)
                            .keyboardType(.decimalPad)
                            .multilineTextAlignment(.trailing)
                            .frame(width: 100)
                    }

                    Text("Starting balance for the child's spending account (optional)")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                // Savings Account Section
                Section {
                    Toggle("Enable Savings Account", isOn: $viewModel.savingsAccountEnabled)

                    if viewModel.savingsAccountEnabled {
                        HStack {
                            Text("Initial Savings Balance")
                            Spacer()
                            TextField("Amount", text: $viewModel.initialSavingsBalance)
                                .keyboardType(.decimalPad)
                                .multilineTextAlignment(.trailing)
                                .frame(width: 100)
                        }

                        Text("Starting balance for the child's savings account (optional)")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                } header: {
                    Text("Savings Account")
                }

                // Savings Transfer Settings
                if viewModel.savingsAccountEnabled {
                    Section("Automatic Savings Transfer") {
                        Picker("Transfer Type", selection: $viewModel.savingsTransferType) {
                            Text("Percentage").tag(SavingsTransferType.percentage)
                            Text("Fixed Amount").tag(SavingsTransferType.fixedAmount)
                        }
                        .pickerStyle(.segmented)

                        if viewModel.savingsTransferType == .percentage {
                            HStack {
                                Text("Savings Percentage")
                                Spacer()
                                TextField("", text: $viewModel.savingsTransferPercentage)
                                    .keyboardType(.numberPad)
                                    .multilineTextAlignment(.trailing)
                                    .frame(width: 60)
                                Text("%")
                                    .foregroundStyle(.secondary)
                            }
                        } else {
                            HStack {
                                Text("Savings Amount")
                                Spacer()
                                Text("$")
                                    .foregroundStyle(.secondary)
                                TextField("", text: $viewModel.savingsTransferAmount)
                                    .keyboardType(.decimalPad)
                                    .multilineTextAlignment(.trailing)
                                    .frame(width: 80)
                            }
                        }

                        // Preview
                        if let preview = viewModel.weeklyBreakdownPreview {
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
                    }
                }

                // Error Message
                if let errorMessage = viewModel.errorMessage {
                    Section {
                        Text(errorMessage)
                            .foregroundStyle(.red)
                            .font(.callout)
                    }
                }
            }
            .navigationTitle("Add Child")
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
                            await viewModel.createChild()
                            if viewModel.isSuccess {
                                dismiss()
                            }
                        }
                    }
                    .disabled(!viewModel.isFormValid || viewModel.isLoading)
                }
            }
            .overlay {
                if viewModel.isLoading {
                    ProgressView("Creating account...")
                        .padding()
                        .background(.regularMaterial)
                        .clipShape(RoundedRectangle(cornerRadius: 12))
                }
            }
            .disabled(viewModel.isLoading)
        }
    }
}

// MARK: - ViewModel

@MainActor
final class AddChildViewModel: ObservableObject {

    // MARK: - Form Fields

    @Published var firstName = ""
    @Published var lastName = ""
    @Published var email = ""
    @Published var password = ""
    @Published var confirmPassword = ""
    @Published var weeklyAllowance = "10.00"
    @Published var initialBalance = "0.00"
    @Published var savingsAccountEnabled = false
    @Published var initialSavingsBalance = "0.00"
    @Published var savingsTransferType: SavingsTransferType = .percentage
    @Published var savingsTransferPercentage = "20"
    @Published var savingsTransferAmount = "2.00"

    // MARK: - State

    @Published var isLoading = false
    @Published var errorMessage: String?
    @Published var isSuccess = false

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol

    // MARK: - Initialization

    init(apiService: APIServiceProtocol) {
        self.apiService = apiService
    }

    // MARK: - Computed Properties

    var isFormValid: Bool {
        !firstName.isEmpty &&
        !lastName.isEmpty &&
        !email.isEmpty &&
        isValidEmail(email) &&
        !password.isEmpty &&
        password.count >= 6 &&
        password == confirmPassword &&
        (Decimal(string: weeklyAllowance) ?? 0) >= 0
    }

    var weeklyBreakdownPreview: (spending: String, savings: String)? {
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

    func createChild() async {
        guard isFormValid else {
            errorMessage = "Please fill in all required fields correctly"
            return
        }

        guard password == confirmPassword else {
            errorMessage = "Passwords do not match"
            return
        }

        isLoading = true
        errorMessage = nil

        do {
            let request = CreateChildRequest(
                email: email,
                password: password,
                firstName: firstName,
                lastName: lastName,
                weeklyAllowance: Decimal(string: weeklyAllowance) ?? 10,
                savingsAccountEnabled: savingsAccountEnabled,
                savingsTransferType: savingsAccountEnabled ? savingsTransferType : .none,
                savingsTransferPercentage: savingsTransferType == .percentage ? Decimal(string: savingsTransferPercentage) : nil,
                savingsTransferAmount: savingsTransferType == .fixedAmount ? Decimal(string: savingsTransferAmount) : nil,
                initialBalance: Decimal(string: initialBalance),
                initialSavingsBalance: savingsAccountEnabled ? Decimal(string: initialSavingsBalance) : nil
            )

            _ = try await apiService.createChild(request)
            isSuccess = true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to create child account. Please try again."
        }

        isLoading = false
    }

    private func isValidEmail(_ email: String) -> Bool {
        let emailRegex = #"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$"#
        return email.range(of: emailRegex, options: .regularExpression) != nil
    }
}

// MARK: - Preview Provider

#Preview("Add Child") {
    AddChildView()
}
