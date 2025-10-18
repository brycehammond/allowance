import SwiftUI

/// View for resetting password with token from email
struct ResetPasswordView: View {

    // MARK: - Properties

    @EnvironmentObject private var authViewModel: AuthViewModel
    @Environment(\.dismiss) private var dismiss

    let email: String
    let token: String

    @State private var newPassword = ""
    @State private var confirmPassword = ""
    @State private var showingSuccessAlert = false

    @FocusState private var focusedField: Field?

    private enum Field {
        case newPassword, confirmPassword
    }

    // MARK: - Body

    var body: some View {
        NavigationStack {
            VStack(spacing: 24) {
                // Header
                VStack(spacing: 12) {
                    Image(systemName: "lock.rotation")
                        .font(.system(size: 64))
                        .foregroundStyle(DesignSystem.Colors.primary)
                        .padding(.top, 40)

                    Text("Reset Password")
                        .font(.title)
                        .fontWeight(.bold)

                    Text("Enter your new password below.")
                        .font(.body)
                        .foregroundStyle(.secondary)
                        .multilineTextAlignment(.center)
                        .padding(.horizontal)
                }

                // Form
                VStack(spacing: 16) {
                    VStack(alignment: .leading, spacing: 8) {
                        Text("Email")
                            .font(.caption)
                            .foregroundStyle(.secondary)

                        Text(email)
                            .padding()
                            .frame(maxWidth: .infinity, alignment: .leading)
                            .background(Color(.systemGray6))
                            .cornerRadius(10)
                    }

                    VStack(alignment: .leading, spacing: 8) {
                        Text("New Password")
                            .font(.caption)
                            .foregroundStyle(.secondary)

                        SecureField("New Password", text: $newPassword)
                            .textContentType(.newPassword)
                            .focused($focusedField, equals: .newPassword)
                            .submitLabel(.next)
                            .onSubmit { focusedField = .confirmPassword }
                            .padding()
                            .background(Color(.systemGray6))
                            .cornerRadius(10)
                    }

                    VStack(alignment: .leading, spacing: 8) {
                        Text("Confirm Password")
                            .font(.caption)
                            .foregroundStyle(.secondary)

                        SecureField("Confirm Password", text: $confirmPassword)
                            .textContentType(.newPassword)
                            .focused($focusedField, equals: .confirmPassword)
                            .submitLabel(.done)
                            .onSubmit { handleResetPassword() }
                            .padding()
                            .background(Color(.systemGray6))
                            .cornerRadius(10)
                    }

                    Text("Password must be at least 6 characters long")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .frame(maxWidth: .infinity, alignment: .leading)

                    if let errorMessage = authViewModel.errorMessage {
                        Text(errorMessage)
                            .font(.caption)
                            .foregroundStyle(.red)
                            .frame(maxWidth: .infinity, alignment: .leading)
                    }

                    Button {
                        handleResetPassword()
                    } label: {
                        if authViewModel.isLoading {
                            ProgressView()
                                .tint(.white)
                                .frame(maxWidth: .infinity)
                                .frame(height: 50)
                        } else {
                            Text("Reset Password")
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
                }
                .padding(.horizontal)

                Spacer()
            }
            .navigationBarHidden(true)
            .onAppear {
                focusedField = .newPassword
            }
            .alert("Password Reset", isPresented: $showingSuccessAlert) {
                Button("OK") {
                    dismiss()
                }
            } message: {
                Text("Your password has been reset successfully. You can now log in with your new password.")
            }
        }
    }

    // MARK: - Computed Properties

    private var isFormValid: Bool {
        !newPassword.isEmpty &&
        !confirmPassword.isEmpty &&
        newPassword.count >= 6
    }

    // MARK: - Methods

    private func handleResetPassword() {
        focusedField = nil

        Task {
            let success = await authViewModel.resetPassword(
                email: email,
                token: token,
                newPassword: newPassword,
                confirmPassword: confirmPassword
            )

            if success {
                showingSuccessAlert = true
            }
        }
    }
}

// MARK: - Preview Provider

#Preview {
    ResetPasswordView(
        email: "user@example.com",
        token: "sample-token"
    )
    .environmentObject(AuthViewModel())
}
