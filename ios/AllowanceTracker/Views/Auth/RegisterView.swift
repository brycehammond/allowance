import SwiftUI

@MainActor
struct RegisterView: View {

    // MARK: - Properties

    @Environment(\.dismiss) private var dismiss
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @State private var viewModel = AuthViewModel()

    @State private var email = ""
    @State private var password = ""
    @State private var confirmPassword = ""
    @State private var firstName = ""
    @State private var lastName = ""
    @State private var selectedRole: UserRole = .parent

    // MARK: - Computed Properties

    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    // MARK: - Body

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 24) {
                    // Header
                    headerSection

                    // Registration form
                    registrationForm

                    // Register button
                    registerButton

                    // Error message
                    if let errorMessage = viewModel.errorMessage {
                        errorSection(message: errorMessage)
                    }
                }
                .padding(.horizontal, isRegularWidth ? 40 : 24)
                .padding(.top, 20)
                .frame(maxWidth: isRegularWidth ? 500 : .infinity)
                .frame(maxWidth: .infinity)
            }
            .navigationTitle("Create Account")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .topBarLeading) {
                    Button("Cancel") {
                        dismiss()
                    }
                    .disabled(viewModel.isLoading)
                }
            }
        }
    }

    // MARK: - View Components

    private var headerSection: some View {
        VStack(spacing: 8) {
            Image(systemName: "person.circle.fill")
                .font(.system(size: 60))
                .foregroundStyle(.blue)

            Text("Join Earn & Learn")
                .font(.title3)
                .fontWeight(.semibold)

            Text("Start managing your family's finances")
                .font(.subheadline)
                .foregroundStyle(.secondary)
        }
        .padding(.bottom, 12)
    }

    private var registrationForm: some View {
        VStack(spacing: 16) {
            // Name fields
            HStack(spacing: 12) {
                VStack(alignment: .leading, spacing: 8) {
                    Text("First Name")
                        .font(.subheadline)
                        .fontWeight(.medium)

                    TextField("First", text: $firstName)
                        .textContentType(.givenName)
                        .textFieldStyle(.roundedBorder)
                        .disabled(viewModel.isLoading)
                        .accessibilityIdentifier(AccessibilityIdentifier.registerFirstNameField)
                }

                VStack(alignment: .leading, spacing: 8) {
                    Text("Last Name")
                        .font(.subheadline)
                        .fontWeight(.medium)

                    TextField("Last", text: $lastName)
                        .textContentType(.familyName)
                        .textFieldStyle(.roundedBorder)
                        .disabled(viewModel.isLoading)
                        .accessibilityIdentifier(AccessibilityIdentifier.registerLastNameField)
                }
            }

            // Email field
            VStack(alignment: .leading, spacing: 8) {
                Text("Email")
                    .font(.subheadline)
                    .fontWeight(.medium)

                TextField("your.email@example.com", text: $email)
                    .textContentType(.emailAddress)
                    .keyboardType(.emailAddress)
                    .autocapitalization(.none)
                    .textFieldStyle(.roundedBorder)
                    .disabled(viewModel.isLoading)
                    .accessibilityIdentifier(AccessibilityIdentifier.registerEmailField)
            }

            // Password fields
            VStack(alignment: .leading, spacing: 8) {
                Text("Password")
                    .font(.subheadline)
                    .fontWeight(.medium)

                SecureField("At least 6 characters", text: $password)
                    .textContentType(.newPassword)
                    .textFieldStyle(.roundedBorder)
                    .disabled(viewModel.isLoading)
                    .accessibilityIdentifier(AccessibilityIdentifier.registerPasswordField)
            }

            VStack(alignment: .leading, spacing: 8) {
                Text("Confirm Password")
                    .font(.subheadline)
                    .fontWeight(.medium)

                SecureField("Re-enter password", text: $confirmPassword)
                    .textContentType(.newPassword)
                    .textFieldStyle(.roundedBorder)
                    .disabled(viewModel.isLoading)
                    .accessibilityIdentifier(AccessibilityIdentifier.registerConfirmPasswordField)

                if !confirmPassword.isEmpty && password != confirmPassword {
                    Text("Passwords do not match")
                        .font(.caption)
                        .foregroundStyle(.red)
                }
            }

            // Role picker
            VStack(alignment: .leading, spacing: 8) {
                Text("Account Type")
                    .font(.subheadline)
                    .fontWeight(.medium)

                Picker("Role", selection: $selectedRole) {
                    Text("Parent").tag(UserRole.parent)
                    Text("Child").tag(UserRole.child)
                }
                .pickerStyle(.segmented)
                .disabled(viewModel.isLoading)
                .accessibilityIdentifier(AccessibilityIdentifier.registerRolePicker)
            }
        }
    }

    private var registerButton: some View {
        Button {
            guard password == confirmPassword else {
                viewModel.errorMessage = "Passwords do not match."
                return
            }

            Task {
                await viewModel.register(
                    email: email,
                    password: password,
                    firstName: firstName,
                    lastName: lastName,
                    role: selectedRole
                )

                // Dismiss on success
                if viewModel.isAuthenticated {
                    dismiss()
                }
            }
        } label: {
            HStack {
                if viewModel.isLoading {
                    ProgressView()
                        .progressViewStyle(.circular)
                        .tint(.white)
                } else {
                    Text("Create Account")
                        .fontWeight(.semibold)
                }
            }
            .frame(maxWidth: .infinity)
            .padding()
            .background(Color.blue)
            .foregroundColor(.white)
            .cornerRadius(12)
        }
        .disabled(viewModel.isLoading || password != confirmPassword)
        .accessibilityIdentifier(AccessibilityIdentifier.registerSubmitButton)
    }

    private func errorSection(message: String) -> some View {
        HStack(spacing: 12) {
            Image(systemName: "exclamationmark.triangle.fill")
                .foregroundStyle(.red)

            Text(message)
                .font(.subheadline)
                .foregroundStyle(.red)

            Spacer()

            Button {
                viewModel.clearError()
            } label: {
                Image(systemName: "xmark.circle.fill")
                    .foregroundStyle(.gray)
            }
        }
        .padding()
        .background(Color.red.opacity(0.1))
        .cornerRadius(12)
    }
}

// MARK: - Preview

#Preview {
    RegisterView()
}
