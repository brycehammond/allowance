import SwiftUI

struct ContentView: View {

    // MARK: - Properties

    @Environment(AuthViewModel.self) private var authViewModel
    @State private var hasCheckedAuth = false

    // MARK: - Body

    var body: some View {
        Group {
            if !hasCheckedAuth {
                // Loading state while checking authentication
                LaunchScreenView()
            } else if authViewModel.requiresBiometricAuth {
                // Biometric authentication required
                BiometricLockView()
            } else if authViewModel.isAuthenticated {
                // Authenticated - show dashboard directly (no tab bar)
                DashboardView()
            } else {
                // Not authenticated - show login
                LoginView()
            }
        }
        .animation(.easeInOut, value: authViewModel.isAuthenticated)
        .animation(.easeInOut, value: authViewModel.requiresBiometricAuth)
        .task {
            // Check authentication status on app launch
            // Use a timeout to ensure we don't get stuck loading
            do {
                try await withTimeout(seconds: 20) {
                    await authViewModel.checkAuthenticationStatus()
                }
            } catch {
                // If timeout or error, just show login screen
                #if DEBUG
                print("Auth check failed or timed out: \(error)")
                #endif
            }
            hasCheckedAuth = true
        }
    }
}

// MARK: - Launch Screen View

/// Simple loading view shown while checking authentication status
struct LaunchScreenView: View {
    var body: some View {
        VStack(spacing: 16) {
            Image(systemName: "dollarsign.circle.fill")
                .font(.system(size: 80))
                .foregroundStyle(Color.green500)

            Text("Earn & Learn")
                .font(.title)
                .fontWeight(.bold)

            ProgressView()
                .padding(.top, 8)
        }
    }
}

// MARK: - Dashboard Placeholder

/// Placeholder view for dashboard (Phase 2 implementation)
struct DashboardPlaceholderView: View {

    @Environment(AuthViewModel.self) private var authViewModel

    var body: some View {
        NavigationStack {
            VStack(spacing: 24) {
                Image(systemName: "checkmark.circle.fill")
                    .font(.system(size: 80))
                    .foregroundStyle(Color.green500)

                Text("Welcome!")
                    .font(.largeTitle)
                    .fontWeight(.bold)

                if let user = authViewModel.currentUser {
                    Text("Hello, \(user.firstName)!")
                        .font(.title3)
                        .foregroundStyle(.secondary)

                    VStack(alignment: .leading, spacing: 8) {
                        InfoRow(label: "Email", value: user.email)
                        InfoRow(label: "Role", value: user.role.rawValue)
                        if let familyId = user.familyId {
                            InfoRow(label: "Family ID", value: familyId.uuidString)
                        }
                    }
                    .padding()
                    .background(Color.backgroundSecondary)
                    .cornerRadius(12)
                }

                Text("Dashboard Coming in Phase 2")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .padding(.top)

                Button {
                    Task {
                        await authViewModel.logout()
                    }
                } label: {
                    Text("Sign Out")
                        .fontWeight(.semibold)
                        .frame(maxWidth: .infinity)
                        .padding()
                        .background(Color.red)
                        .foregroundColor(.white)
                        .cornerRadius(12)
                }
                .padding(.top)
            }
            .padding()
            .navigationTitle("Dashboard")
        }
    }
}

// MARK: - Info Row

struct InfoRow: View {
    let label: String
    let value: String

    var body: some View {
        HStack {
            Text(label)
                .fontWeight(.medium)
            Spacer()
            Text(value)
                .foregroundStyle(.secondary)
        }
    }
}

// MARK: - Preview

#Preview {
    ContentView()
        .environment(AuthViewModel())
}

// MARK: - Timeout Helper

/// Execute an async operation with a timeout
/// - Parameters:
///   - seconds: Maximum time to wait
///   - operation: The async operation to perform
/// - Throws: CancellationError if timeout is reached
private func withTimeout<T>(seconds: TimeInterval, operation: @escaping () async -> T) async throws -> T {
    try await withThrowingTaskGroup(of: T.self) { group in
        group.addTask {
            await operation()
        }

        group.addTask {
            try await Task.sleep(nanoseconds: UInt64(seconds * 1_000_000_000))
            throw CancellationError()
        }

        let result = try await group.next()!
        group.cancelAll()
        return result
    }
}
