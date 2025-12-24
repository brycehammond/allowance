import SwiftUI

@MainActor
struct LoginView: View {

    // MARK: - Properties

    @Environment(AuthViewModel.self) private var viewModel
    @State private var email = ""
    @State private var password = ""
    @State private var showRegister = false
    @State private var showForgotPassword = false
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass

    // MARK: - Computed Properties

    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    // MARK: - Body

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 24) {
                    // Logo and title
                    logoSection

                    // Login form
                    loginForm

                    // Login button
                    loginButton

                    // Forgot password link
                    forgotPasswordLink

                    // Register link
                    registerLink

                    // Error message
                    if let errorMessage = viewModel.errorMessage {
                        errorSection(message: errorMessage)
                    }
                }
                .padding(.horizontal, isRegularWidth ? 40 : 24)
                .padding(.top, 40)
                .frame(maxWidth: isRegularWidth ? 500 : .infinity)
                .frame(maxWidth: .infinity)
            }
            .navigationTitle("Login")
            .navigationBarTitleDisplayMode(.inline)
            .sheet(isPresented: $showRegister) {
                RegisterView()
                    .environment(viewModel)
            }
            .sheet(isPresented: $showForgotPassword) {
                ForgotPasswordView()
                    .environment(viewModel)
            }
        }
    }

    // MARK: - View Components

    private var logoSection: some View {
        VStack(spacing: 12) {
            Image(systemName: "dollarsign.circle.fill")
                .font(.system(size: 80))
                .foregroundStyle(.green)
                .accessibilityHidden()

            Text("Earn & Learn")
                .font(.scalable(.title, weight: .bold))
                .accessibleHeader("Earn & Learn")

            Text("Track, Save, Learn")
                .font(.scalable(.subheadline))
                .foregroundStyle(.secondary)
                .accessibilityHidden()
        }
        .padding(.bottom, 20)
    }

    private var loginForm: some View {
        VStack(spacing: 16) {
            // Email field
            VStack(alignment: .leading, spacing: 8) {
                Text("Email")
                    .font(.scalable(.subheadline, weight: .medium))

                TextField("Enter your email", text: $email)
                    .textContentType(.emailAddress)
                    .keyboardType(.emailAddress)
                    .autocapitalization(.none)
                    .textFieldStyle(.roundedBorder)
                    .disabled(viewModel.isLoading)
                    .accessibilityLabel("Email address")
                    .accessibilityHint("Enter your email address to sign in")
                    .accessibilityIdentifier(AccessibilityIdentifier.loginEmailField)
            }

            // Password field
            VStack(alignment: .leading, spacing: 8) {
                Text("Password")
                    .font(.scalable(.subheadline, weight: .medium))

                SecureField("Enter your password", text: $password)
                    .textContentType(.password)
                    .textFieldStyle(.roundedBorder)
                    .disabled(viewModel.isLoading)
                    .accessibilityLabel("Password")
                    .accessibilityHint("Enter your password")
                    .accessibilityIdentifier(AccessibilityIdentifier.loginPasswordField)
            }
        }
    }

    private var loginButton: some View {
        Button {
            Task {
                await viewModel.login(email: email, password: password)
            }
        } label: {
            HStack {
                if viewModel.isLoading {
                    ProgressView()
                        .progressViewStyle(.circular)
                        .tint(.white)
                        .accessibilityLabel("Signing in")
                } else {
                    Text("Sign In")
                        .font(.scalable(.body, weight: .semibold))
                }
            }
            .frame(maxWidth: .infinity)
            .padding()
            .background(Color.blue)
            .foregroundColor(.white)
            .cornerRadius(12)
        }
        .disabled(viewModel.isLoading)
        .accessibilityLabel(viewModel.isLoading ? "Signing in" : "Sign in")
        .accessibilityHint(viewModel.isLoading ? "Please wait while signing in" : "Double tap to sign in with your email and password")
        .accessibilityIdentifier(AccessibilityIdentifier.loginButton)
    }

    private var forgotPasswordLink: some View {
        Button {
            showForgotPassword = true
        } label: {
            Text("Forgot your password?")
                .font(.scalable(.subheadline, weight: .medium))
                .foregroundStyle(.blue)
        }
        .disabled(viewModel.isLoading)
        .accessibilityLabel("Forgot password")
        .accessibilityHint("Double tap to reset your password")
    }

    private var registerLink: some View {
        Button {
            showRegister = true
        } label: {
            HStack(spacing: 4) {
                Text("Don't have an account?")
                    .foregroundStyle(.secondary)
                Text("Sign Up")
                    .fontWeight(.semibold)
                    .foregroundStyle(.blue)
            }
            .font(.scalable(.subheadline))
        }
        .disabled(viewModel.isLoading)
        .accessibilityLabel("Don't have an account? Sign up")
        .accessibilityHint("Double tap to create a new account")
        .accessibilityIdentifier(AccessibilityIdentifier.registerButton)
    }

    private func errorSection(message: String) -> some View {
        HStack(spacing: 12) {
            Image(systemName: "exclamationmark.triangle.fill")
                .foregroundStyle(.red)
                .accessibilityHidden()

            Text(message)
                .font(.scalable(.subheadline))
                .foregroundStyle(.red)

            Spacer()

            Button {
                viewModel.clearError()
            } label: {
                Image(systemName: "xmark.circle.fill")
                    .foregroundStyle(.gray)
            }
            .accessibilityLabel("Dismiss error")
            .accessibilityHint("Double tap to dismiss this error message")
        }
        .padding()
        .background(Color.red.opacity(0.1))
        .cornerRadius(12)
        .accessibilityElement(children: .combine)
        .accessibilityLabel("Error: \(message)")
        .onAppear {
            // Announce error to VoiceOver users
            AccessibilityAnnouncement.announce("Error: \(message)")
        }
    }
}

// MARK: - Preview

#Preview {
    LoginView()
        .environment(AuthViewModel())
}
