import AuthenticationServices
import GoogleSignInSwift
import SwiftUI

@MainActor
struct LoginView: View {

    // MARK: - Properties

    @Environment(AuthViewModel.self) private var viewModel
    @State private var email = ""
    @State private var password = ""
    @State private var showRegister = false
    @State private var showForgotPassword = false
    @State private var familyNameInput = ""
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

                    // Social sign-in divider and buttons
                    socialSignInDivider
                    socialSignInButtons

                    // Forgot password link
                    forgotPasswordLink

                    // Register link
                    registerLink

                    // Terms and Privacy links
                    termsAndPrivacySection

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
            .sheet(isPresented: $showRegister) {
                RegisterView()
                    .environment(viewModel)
            }
            .sheet(isPresented: $showForgotPassword) {
                ForgotPasswordView()
                    .environment(viewModel)
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
                        familyNameInput = ""
                    }
                }
            } message: {
                Text("Please enter a name for your family to complete registration.")
            }
        }
    }

    // MARK: - View Components

    private var logoSection: some View {
        VStack(spacing: 12) {
            Image(systemName: "dollarsign.circle.fill")
                .font(.system(size: 80))
                .foregroundStyle(Color.green600)
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
            .background(Color.green600)
            .foregroundColor(.white)
            .cornerRadius(12)
        }
        .disabled(viewModel.isLoading)
        .accessibilityLabel(viewModel.isLoading ? "Signing in" : "Sign in")
        .accessibilityHint(viewModel.isLoading ? "Please wait while signing in" : "Double tap to sign in with your email and password")
        .accessibilityIdentifier(AccessibilityIdentifier.loginButton)
    }

    private var socialSignInDivider: some View {
        HStack {
            Rectangle()
                .fill(Color.secondary.opacity(0.3))
                .frame(height: 1)

            Text("or")
                .font(.scalable(.subheadline))
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
                }
            })
            .frame(height: 50)
            .cornerRadius(12)
            .disabled(viewModel.isLoading)
            .accessibilityLabel("Sign in with Google")
        }
    }

    private var forgotPasswordLink: some View {
        Button {
            showForgotPassword = true
        } label: {
            Text("Forgot your password?")
                .font(.scalable(.subheadline, weight: .medium))
                .foregroundStyle(Color.green600)
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
                    .foregroundStyle(Color.green600)
            }
            .font(.scalable(.subheadline))
        }
        .tint(Color.green600)
        .disabled(viewModel.isLoading)
        .accessibilityLabel("Don't have an account? Sign up")
        .accessibilityHint("Double tap to create a new account")
        .accessibilityIdentifier(AccessibilityIdentifier.registerButton)
    }

    private var termsAndPrivacySection: some View {
        VStack(spacing: 4) {
            Text("By signing in, you agree to our")
                .foregroundStyle(.secondary)

            HStack(spacing: 4) {
                Link("Terms of Service", destination: URL(string: "https://www.earnandlearn.app/terms")!)
                    .foregroundStyle(Color.green600)

                Text("and")
                    .foregroundStyle(.secondary)

                Link("Privacy Policy", destination: URL(string: "https://www.earnandlearn.app/privacy")!)
                    .foregroundStyle(Color.green600)
            }
        }
        .font(.scalable(.caption))
        .multilineTextAlignment(.center)
        .padding(.top, 8)
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
