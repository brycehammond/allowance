import AuthenticationServices
import GoogleSignInSwift
import SwiftUI

@MainActor
struct RegisterView: View {

    // MARK: - Properties

    @Environment(\.dismiss) private var dismiss
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @Environment(AuthViewModel.self) private var viewModel

    @State private var email = ""
    @State private var password = ""
    @State private var confirmPassword = ""
    @State private var firstName = ""
    @State private var lastName = ""
    @State private var familyNameInput = ""

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

                    // Social sign-in divider and buttons
                    socialSignInDivider
                    socialSignInButtons

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
            .alert("Family Name Required", isPresented: Binding(
                get: { viewModel.showFamilyNamePrompt },
                set: { viewModel.showFamilyNamePrompt = $0 }
            )) {
                TextField("Family Name", text: $familyNameInput)
                Button("Cancel", role: .cancel) {
                    viewModel.showFamilyNamePrompt = false
                    viewModel.pendingExternalLogin = nil
                    familyNameInput = ""
                }
                Button("Continue") {
                    Task {
                        await viewModel.completePendingExternalLogin(familyName: familyNameInput)
                        if viewModel.isAuthenticated {
                            familyNameInput = ""
                            dismiss()
                        }
                    }
                }
            } message: {
                Text("Please enter a name for your family to complete registration.")
            }
        }
    }

    // MARK: - View Components

    private var headerSection: some View {
        VStack(spacing: 8) {
            Image(systemName: "person.circle.fill")
                .font(.system(size: 60))
                .foregroundStyle(Color.green600)

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
                    role: .parent
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
            .background(Color.green600)
            .foregroundColor(.white)
            .cornerRadius(12)
        }
        .disabled(viewModel.isLoading || password != confirmPassword)
        .accessibilityIdentifier(AccessibilityIdentifier.registerSubmitButton)
    }

    private var socialSignInDivider: some View {
        HStack {
            Rectangle()
                .fill(Color.secondary.opacity(0.3))
                .frame(height: 1)

            Text("or")
                .font(.subheadline)
                .foregroundStyle(.secondary)

            Rectangle()
                .fill(Color.secondary.opacity(0.3))
                .frame(height: 1)
        }
    }

    private var socialSignInButtons: some View {
        VStack(spacing: 12) {
            // Apple Sign In
            AppleSignInButton { result in
                switch result {
                case .success(let authorization):
                    Task {
                        await viewModel.signInWithApple(authorization: authorization)
                        if viewModel.isAuthenticated {
                            dismiss()
                        }
                    }
                case .failure(let error):
                    if (error as NSError).code != ASAuthorizationError.canceled.rawValue {
                        viewModel.errorMessage = "Apple Sign In failed. Please try again."
                    }
                }
            }
            .disabled(viewModel.isLoading)

            // Google Sign In
            GoogleSignInButton(scheme: .light, style: .wide, action: {
                Task {
                    await viewModel.signInWithGoogle()
                    if viewModel.isAuthenticated {
                        dismiss()
                    }
                }
            })
            .frame(height: 50)
            .cornerRadius(12)
            .disabled(viewModel.isLoading)
            .accessibilityLabel("Sign in with Google")
        }
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
        .environment(AuthViewModel())
}
