import SwiftUI

/// Profile and settings view
struct ProfileView: View {

    // MARK: - Properties

    @EnvironmentObject private var authViewModel: AuthViewModel
    @State private var showingLogoutConfirmation = false

    // MARK: - Body

    var body: some View {
        NavigationStack {
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

                        if let familyId = user.familyId {
                            ProfileInfoRow(
                                label: "Family ID",
                                value: familyId.uuidString,
                                icon: "house.fill"
                            )
                        }
                    }
                }

                // Settings Section
                Section("Settings") {
                    NavigationLink {
                        ChangePasswordView()
                    } label: {
                        Label("Change Password", systemImage: "key.fill")
                    }

                    NavigationLink {
                        NotificationsSettingsView()
                    } label: {
                        Label("Notifications", systemImage: "bell.fill")
                    }

                    NavigationLink {
                        AppearanceSettingsView()
                    } label: {
                        Label("Appearance", systemImage: "paintbrush.fill")
                    }

                    NavigationLink {
                        AboutView()
                    } label: {
                        Label("About", systemImage: "info.circle.fill")
                    }
                }

                // Account Actions Section
                Section {
                    Button(role: .destructive) {
                        showingLogoutConfirmation = true
                    } label: {
                        Label("Sign Out", systemImage: "arrow.right.square")
                    }
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
                Link(destination: URL(string: "https://allowancetracker.com")!) {
                    Label("Website", systemImage: "globe")
                }

                Link(destination: URL(string: "mailto:support@allowancetracker.com")!) {
                    Label("Email Support", systemImage: "envelope.fill")
                }
            }

            Section {
                NavigationLink {
                    PrivacyPolicyView()
                } label: {
                    Label("Privacy Policy", systemImage: "hand.raised.fill")
                }

                NavigationLink {
                    TermsOfServiceView()
                } label: {
                    Label("Terms of Service", systemImage: "doc.text.fill")
                }
            }
        }
        .navigationTitle("About")
        .navigationBarTitleDisplayMode(.inline)
    }
}

/// Placeholder for privacy policy
struct PrivacyPolicyView: View {
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                Text("Privacy Policy")
                    .font(.title)
                    .fontWeight(.bold)

                Text("Last updated: \(Date().formatted(date: .long, time: .omitted))")
                    .font(.caption)
                    .foregroundStyle(.secondary)

                Text("""
                Your privacy is important to us. This privacy policy explains how Allowance Tracker collects, uses, and protects your personal information.

                **Information We Collect**
                - Account information (name, email)
                - Transaction data
                - Usage information

                **How We Use Your Information**
                - To provide and improve our services
                - To communicate with you
                - To ensure security

                **Data Security**
                We implement industry-standard security measures to protect your data.

                **Your Rights**
                You have the right to access, update, or delete your personal information.

                For questions, contact us at privacy@allowancetracker.com
                """)
                .font(.body)
            }
            .padding()
        }
        .navigationTitle("Privacy Policy")
        .navigationBarTitleDisplayMode(.inline)
    }
}

/// Placeholder for terms of service
struct TermsOfServiceView: View {
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                Text("Terms of Service")
                    .font(.title)
                    .fontWeight(.bold)

                Text("Last updated: \(Date().formatted(date: .long, time: .omitted))")
                    .font(.caption)
                    .foregroundStyle(.secondary)

                Text("""
                **1. Acceptance of Terms**
                By using Allowance Tracker, you agree to these terms of service.

                **2. User Accounts**
                You are responsible for maintaining the security of your account.

                **3. Parental Consent**
                Parents must provide consent for children under 13 to use this service.

                **4. Service Usage**
                - Use the service only for lawful purposes
                - Do not attempt to compromise security
                - Respect other users' privacy

                **5. Limitation of Liability**
                Allowance Tracker is provided "as is" without warranties.

                **6. Changes to Terms**
                We may update these terms at any time. Continued use constitutes acceptance.

                For questions, contact us at legal@allowancetracker.com
                """)
                .font(.body)
            }
            .padding()
        }
        .navigationTitle("Terms of Service")
        .navigationBarTitleDisplayMode(.inline)
    }
}

// MARK: - Preview Provider

#Preview("Profile View") {
    ProfileView()
        .environmentObject({
            let vm = AuthViewModel()
            vm.currentUser = User(
                id: UUID(),
                email: "parent@test.com",
                firstName: "John",
                lastName: "Doe",
                role: .parent,
                familyId: UUID()
            )
            vm.isAuthenticated = true
            return vm
        }())
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
