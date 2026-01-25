import SwiftUI

/// Profile and settings view
@MainActor
struct ProfileView: View {

    // MARK: - Properties

    @Environment(AuthViewModel.self) private var authViewModel
    @State private var showingLogoutConfirmation = false
    @State private var showingDeleteAccountConfirmation = false
    @State private var showingAddChild = false
    @State private var showingInviteParent = false

    private var isParent: Bool {
        authViewModel.currentUser?.role == .parent
    }

    // MARK: - Body

    var body: some View {
        List {
                // User Information Section
                if let user = authViewModel.currentUser {
                    Section("Account") {
                        ProfileInfoRow(
                            label: "Name",
                            value: user.fullName,
                            icon: "person.fill"
                        )

                        ProfileInfoRow(
                            label: "Email",
                            value: user.email,
                            icon: "envelope.fill"
                        )

                        ProfileInfoRow(
                            label: "Role",
                            value: user.role.rawValue,
                            icon: user.role == .parent ? "person.2.fill" : "person.fill"
                        )
                    }
                }

                // Family Management Section (Parent only)
                if isParent {
                    Section("Family Management") {
                        Button {
                            showingAddChild = true
                        } label: {
                            Label("Add Child", systemImage: "person.badge.plus")
                        }

                        Button {
                            showingInviteParent = true
                        } label: {
                            Label("Invite Co-Parent", systemImage: "person.2.badge.gearshape")
                        }
                    }
                }

                // Security Section
                if authViewModel.isBiometricAvailable {
                    Section("Security") {
                        BiometricToggleRow()
                    }
                }

                // Settings Section
                Section("Settings") {
                    NavigationLink {
                        ChangePasswordView()
                    } label: {
                        Label("Change Password", systemImage: "key.fill")
                    }
                    .accessibilityIdentifier(AccessibilityIdentifier.changePasswordButton)

                    NavigationLink {
                        NotificationsSettingsView()
                    } label: {
                        Label("Notifications", systemImage: "bell.fill")
                    }
                    .accessibilityIdentifier(AccessibilityIdentifier.notificationsButton)

                    NavigationLink {
                        AboutView()
                    } label: {
                        Label("About", systemImage: "info.circle.fill")
                    }
                    .accessibilityIdentifier(AccessibilityIdentifier.aboutButton)
                }

                // Account Actions Section
                Section {
                    Button(role: .destructive) {
                        showingLogoutConfirmation = true
                    } label: {
                        Label("Sign Out", systemImage: "arrow.right.square")
                    }
                    .accessibilityIdentifier(AccessibilityIdentifier.signOutButton)

                    Button(role: .destructive) {
                        showingDeleteAccountConfirmation = true
                    } label: {
                        Label("Delete Account", systemImage: "trash")
                            .foregroundStyle(.red)
                    }
                    .accessibilityIdentifier(AccessibilityIdentifier.deleteAccountButton)
                }
            }
        .navigationTitle("Profile")
        .confirmationDialog(
            "Sign Out",
            isPresented: $showingLogoutConfirmation,
            titleVisibility: .visible
        ) {
            Button("Sign Out", role: .destructive) {
                Task {
                    await authViewModel.logout()
                }
            }
            Button("Cancel", role: .cancel) {}
        } message: {
            Text("Are you sure you want to sign out?")
        }
        .confirmationDialog(
            "Delete Account?",
            isPresented: $showingDeleteAccountConfirmation,
            titleVisibility: .visible
        ) {
            Button("Delete Account", role: .destructive) {
                Task {
                    await authViewModel.deleteAccount()
                }
            }
            Button("Cancel", role: .cancel) {}
        } message: {
            Text("This will permanently delete your account and all associated data. This action cannot be undone.")
        }
        .sheet(isPresented: $showingAddChild) {
            AddChildView()
        }
        .sheet(isPresented: $showingInviteParent) {
            InviteParentView()
        }
    }
}

// MARK: - Biometric Toggle Row

/// Toggle row for enabling/disabling biometric authentication
struct BiometricToggleRow: View {
    @Environment(AuthViewModel.self) private var authViewModel
    @State private var isEnabled: Bool = false

    var body: some View {
        Toggle(isOn: $isEnabled) {
            Label(
                authViewModel.biometricType.displayName,
                systemImage: authViewModel.biometricType.iconName
            )
        }
        .tint(DesignSystem.Colors.primary)
        .onAppear {
            isEnabled = authViewModel.isBiometricEnabled
        }
        .onChange(of: isEnabled) { _, newValue in
            authViewModel.setBiometricEnabled(newValue)
        }
    }
}

// MARK: - Profile Info Row

/// Reusable row for displaying user information with an icon
struct ProfileInfoRow: View {
    let label: String
    let value: String
    let icon: String

    var body: some View {
        HStack(spacing: 12) {
            Image(systemName: icon)
                .font(.body)
                .foregroundStyle(DesignSystem.Colors.primary)
                .frame(width: 24)

            VStack(alignment: .leading, spacing: 2) {
                Text(label)
                    .font(.caption)
                    .foregroundStyle(.secondary)

                Text(value)
                    .font(.body)
            }
        }
        .padding(.vertical, 4)
    }
}

// MARK: - Placeholder Settings Views

/// Placeholder for notifications settings
struct NotificationsSettingsView: View {
    @State private var transactionNotifications = true
    @State private var allowanceNotifications = true
    @State private var goalNotifications = true

    var body: some View {
        Form {
            Section {
                Toggle("Transaction Alerts", isOn: $transactionNotifications)
                Toggle("Allowance Payments", isOn: $allowanceNotifications)
                Toggle("Goal Milestones", isOn: $goalNotifications)
            } header: {
                Text("Notifications")
            } footer: {
                Text("Get notified about important events")
            }
        }
        .navigationTitle("Notifications")
        .navigationBarTitleDisplayMode(.inline)
    }
}

/// Placeholder for appearance settings
struct AppearanceSettingsView: View {
    @State private var selectedAppearance = "System"

    let appearances = ["Light", "Dark", "System"]

    var body: some View {
        Form {
            Section {
                Picker("Theme", selection: $selectedAppearance) {
                    ForEach(appearances, id: \.self) { appearance in
                        Text(appearance).tag(appearance)
                    }
                }
                .pickerStyle(.segmented)
            } header: {
                Text("Appearance")
            } footer: {
                Text("Choose your preferred color theme")
            }
        }
        .navigationTitle("Appearance")
        .navigationBarTitleDisplayMode(.inline)
    }
}

/// Placeholder for about view
struct AboutView: View {
    var body: some View {
        Form {
            Section("App Information") {
                ProfileInfoRow(label: "Version", value: Configuration.appVersion, icon: "app.fill")
                ProfileInfoRow(label: "Build", value: Configuration.buildNumber, icon: "hammer.fill")

                #if DEBUG
                ProfileInfoRow(label: "Environment", value: Configuration.environment.rawValue, icon: "server.rack")
                ProfileInfoRow(label: "API URL", value: Configuration.apiBaseURL.host ?? "Unknown", icon: "network")
                #endif
            }

            Section("Support") {
                Link(destination: URL(string: "https://earnandlearn.app")!) {
                    Label("Website", systemImage: "globe")
                }
            }

            Section {
                Link(destination: URL(string: "https://earnandlearn.app/privacy")!) {
                    Label("Privacy Policy", systemImage: "hand.raised.fill")
                }

                Link(destination: URL(string: "https://earnandlearn.app/terms")!) {
                    Label("Terms of Service", systemImage: "doc.text.fill")
                }
            }
        }
        .navigationTitle("About")
        .navigationBarTitleDisplayMode(.inline)
    }
}


// MARK: - Preview Provider

#Preview("Profile View") {
    let authViewModel = AuthViewModel()
    authViewModel.currentUser = User(
        id: UUID(),
        email: "parent@test.com",
        firstName: "John",
        lastName: "Doe",
        role: .parent,
        familyId: UUID()
    )
    authViewModel.isAuthenticated = true

    return ProfileView()
        .environment(authViewModel)
}

#Preview("Notifications Settings") {
    NavigationStack {
        NotificationsSettingsView()
    }
}

#Preview("About View") {
    NavigationStack {
        AboutView()
    }
}
