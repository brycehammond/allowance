import SwiftUI

/// View for requesting password reset email
struct ForgotPasswordView: View {

    // MARK: - Properties

    @EnvironmentObject private var authViewModel: AuthViewModel
    @Environment(\.dismiss) private var dismiss

    @State private var email = ""
    @State private var showingSuccessAlert = false

    @FocusState private var emailFieldFocused: Bool

    // MARK: - Body

    var body: some View {
        NavigationStack {
            VStack(spacing: 24) {
                // Header
                VStack(spacing: 12) {
                    Image(systemName: "envelope.fill")
                        .font(.system(size: 64))
                        .foregroundStyle(DesignSystem.Colors.primary)
                        .padding(.top, 40)

                    Text("Forgot Password?")
                        .font(.title)
                        .fontWeight(.bold)

                    Text("Enter your email address and we'll send you a link to reset your password.")
                        .font(.body)
                        .foregroundStyle(.secondary)
                        .multilineTextAlignment(.center)
                        .padding(.horizontal)
                }

                // Form
                VStack(spacing: 16) {
                    VStack(alignment: .leading, spacing: 8) {
                        Text("Email Address")
                            .font(.caption)
                            .foregroundStyle(.secondary)

                        TextField("Email", text: $email)
                            .textContentType(.emailAddress)
                            .keyboardType(.emailAddress)
                            .autocapitalization(.none)
                            .focused($emailFieldFocused)
                            .submitLabel(.send)
                            .onSubmit { handleForgotPassword() }
                            .padding()
                            .background(Color(.systemGray6))
                            .cornerRadius(10)
                    }

                    if let errorMessage = authViewModel.errorMessage {
                        Text(errorMessage)
                            .font(.caption)
                            .foregroundStyle(.red)
                            .frame(maxWidth: .infinity, alignment: .leading)
                    }

                    Button {
                        handleForgotPassword()
                    } label: {
                        if authViewModel.isLoading {
                            ProgressView()
                                .tint(.white)
                                .frame(maxWidth: .infinity)
                                .frame(height: 50)
                        } else {
                            Text("Send Reset Link")
                                .fontWeight(.semibold)
                                .frame(maxWidth: .infinity)
                                .frame(height: 50)
                        }
                    }
                    .disabled(authViewModel.isLoading || !isFormValid)
                    .background(
                        (authViewModel.isLoading || !isFormValid)
                        ? DesignSystem.Colors.primary.opacity(0.5)
                        : DesignSystem.Colors.primary
                    )
                    .foregroundStyle(.white)
                    .cornerRadius(10)
                    .padding(.top, 8)

                    Button {
                        dismiss()
                    } label: {
                        HStack(spacing: 4) {
                            Image(systemName: "arrow.left")
                                .font(.caption)
                            Text("Back to Login")
                        }
                        .font(.body)
                        .foregroundStyle(DesignSystem.Colors.primary)
                    }
                    .padding(.top, 8)
                }
                .padding(.horizontal)

                Spacer()
            }
            .navigationBarHidden(true)
            .onAppear {
                emailFieldFocused = true
            }
            .alert("Email Sent", isPresented: $showingSuccessAlert) {
                Button("OK") {
                    dismiss()
                }
            } message: {
                Text("If your email is registered, you will receive a password reset link shortly.")
            }
        }
    }

    // MARK: - Computed Properties

    private var isFormValid: Bool {
        !email.isEmpty && email.contains("@")
    }

    // MARK: - Methods

    private func handleForgotPassword() {
        emailFieldFocused = false

        Task {
            let success = await authViewModel.forgotPassword(email: email)

            if success {
                showingSuccessAlert = true
            }
        }
    }
}

// MARK: - Preview Provider

#Preview {
    ForgotPasswordView()
        .environmentObject(AuthViewModel())
}
