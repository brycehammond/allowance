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
            await authViewModel.checkAuthenticationStatus()
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
                .foregroundStyle(Color.accentColor)

            Text("Allowance Tracker")
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
                    .foregroundStyle(.green)

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
