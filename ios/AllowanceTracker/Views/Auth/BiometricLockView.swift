import SwiftUI

/// View displayed when biometric authentication is required to unlock the app
struct BiometricLockView: View {

    // MARK: - Properties

    @Environment(AuthViewModel.self) private var authViewModel
    @State private var isAuthenticating = false

    // MARK: - Body

    var body: some View {
        VStack(spacing: 32) {
            Spacer()

            // Lock icon with biometric type indicator
            ZStack {
                Circle()
                    .fill(Color.primary.opacity(0.1))
                    .frame(width: 120, height: 120)

                Image(systemName: authViewModel.biometricType.iconName)
                    .font(.system(size: 48))
                    .foregroundStyle(.primary)
            }

            VStack(spacing: 8) {
                Text("Allowance Tracker")
                    .font(.title)
                    .fontWeight(.bold)

                Text("Unlock with \(authViewModel.biometricType.displayName)")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            // Error message if authentication failed
            if let errorMessage = authViewModel.errorMessage {
                Text(errorMessage)
                    .font(.footnote)
                    .foregroundStyle(.red)
                    .multilineTextAlignment(.center)
                    .padding(.horizontal)
            }

            // Unlock button
            Button {
                Task {
                    await authenticate()
                }
            } label: {
                HStack(spacing: 12) {
                    if isAuthenticating {
                        ProgressView()
                            .tint(.white)
                    } else {
                        Image(systemName: authViewModel.biometricType.iconName)
                    }
                    Text(isAuthenticating ? "Authenticating..." : "Unlock with \(authViewModel.biometricType.displayName)")
                }
                .fontWeight(.semibold)
                .frame(maxWidth: .infinity)
                .padding()
                .background(Color.green500)
                .foregroundColor(.white)
                .cornerRadius(12)
            }
            .disabled(isAuthenticating)
            .padding(.horizontal)

            // Use password option
            Button {
                // Clear the biometric requirement and go to login
                Task {
                    await authViewModel.logout()
                }
            } label: {
                Text("Use Password Instead")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }
            .padding(.bottom, 32)
        }
        .onAppear {
            // Automatically prompt for biometric auth on appear
            Task {
                await authenticate()
            }
        }
    }

    // MARK: - Private Methods

    private func authenticate() async {
        authViewModel.clearError()
        isAuthenticating = true
        defer { isAuthenticating = false }

        _ = await authViewModel.authenticateWithBiometric()
    }
}

// MARK: - Preview

#Preview {
    BiometricLockView()
        .environment(AuthViewModel())
}
