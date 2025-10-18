import SwiftUI

/// View for changing user password
struct ChangePasswordView: View {

    // MARK: - Properties

    @EnvironmentObject private var authViewModel: AuthViewModel
    @Environment(\.dismiss) private var dismiss

    @State private var currentPassword = ""
    @State private var newPassword = ""
    @State private var confirmPassword = ""
    @State private var showingSuccessAlert = false

    @FocusState private var focusedField: Field?

    private enum Field {
        case currentPassword, newPassword, confirmPassword
    }

    // MARK: - Body

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    SecureField("Current Password", text: $currentPassword)
                        .textContentType(.password)
                        .focused($focusedField, equals: .currentPassword)
                        .submitLabel(.next)
                        .onSubmit { focusedField = .newPassword }
                } header: {
                    Text("Current Password")
                } footer: {
                    Text("Enter your current password to verify your identity")
                }

                Section {
                    SecureField("New Password", text: $newPassword)
                        .textContentType(.newPassword)
                        .focused($focusedField, equals: .newPassword)
                        .submitLabel(.next)
                        .onSubmit { focusedField = .confirmPassword }

                    SecureField("Confirm New Password", text: $confirmPassword)
                        .textContentType(.newPassword)
                        .focused($focusedField, equals: .confirmPassword)
                        .submitLabel(.done)
                        .onSubmit { handleChangePassword() }
                } header: {
                    Text("New Password")
                } footer: {
                    Text("Password must be at least 6 characters long")
                }

                if let errorMessage = authViewModel.errorMessage {
                    Section {
                        Text(errorMessage)
                            .foregroundStyle(.red)
                            .font(.caption)
                    }
                }

                Section {
                    Button {
                        handleChangePassword()
                    } label: {
                        if authViewModel.isLoading {
                            HStack {
                                Spacer()
                                ProgressView()
                                    .tint(.white)
                                Spacer()
                            }
                        } else {
                            Text("Change Password")
                                .frame(maxWidth: .infinity)
                                .fontWeight(.semibold)
                        }
                    }
                    .disabled(authViewModel.isLoading || !isFormValid)
                    .listRowBackground(
                        (authViewModel.isLoading || !isFormValid)
                        ? DesignSystem.Colors.primary.opacity(0.5)
                        : DesignSystem.Colors.primary
                    )
                    .foregroundStyle(.white)
                }
            }
            .navigationTitle("Change Password")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        dismiss()
                    }
                }
            }
            .alert("Success", isPresented: $showingSuccessAlert) {
                Button("OK") {
                    dismiss()
                }
            } message: {
                Text("Your password has been changed successfully.")
            }
        }
    }

    // MARK: - Computed Properties

    private var isFormValid: Bool {
        !currentPassword.isEmpty &&
        !newPassword.isEmpty &&
        !confirmPassword.isEmpty &&
        newPassword.count >= 6
    }

    // MARK: - Methods

    private func handleChangePassword() {
        Task {
            let success = await authViewModel.changePassword(
                currentPassword: currentPassword,
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
    ChangePasswordView()
        .environmentObject(AuthViewModel())
}
