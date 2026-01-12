import SwiftUI

/// View for managing notification preferences
struct NotificationSettingsView: View {

    // MARK: - State

    @State private var preferences: NotificationPreferences?
    @State private var isLoading = true
    @State private var isSaving = false
    @State private var showError = false
    @State private var errorMessage = ""
    @State private var showSuccess = false

    // Quiet hours
    @State private var quietHoursEnabled = false
    @State private var quietHoursStart = Date()
    @State private var quietHoursEnd = Date()

    // Modified preferences
    @State private var modifiedPreferences: [NotificationType: NotificationPreferenceItem] = [:]

    // Push notification status
    @State private var pushNotificationsEnabled = false

    private let apiService: APIServiceProtocol

    // MARK: - Initialization

    init(apiService: APIServiceProtocol = APIService()) {
        self.apiService = apiService
    }

    // MARK: - Body

    var body: some View {
        List {
            // Push Notification Permission Section
            Section {
                HStack {
                    Label("Push Notifications", systemImage: "bell.badge.fill")
                    Spacer()
                    if pushNotificationsEnabled {
                        Text("Enabled")
                            .foregroundStyle(.secondary)
                    } else {
                        Button("Enable") {
                            Task {
                                await requestPushPermission()
                            }
                        }
                        .buttonStyle(.bordered)
                    }
                }
            } header: {
                Text("Device Settings")
            } footer: {
                Text("Enable push notifications to receive alerts on this device.")
            }

            // Quiet Hours Section
            Section {
                Toggle(isOn: $quietHoursEnabled) {
                    Label("Quiet Hours", systemImage: "moon.fill")
                }

                if quietHoursEnabled {
                    DatePicker(
                        "Start Time",
                        selection: $quietHoursStart,
                        displayedComponents: .hourAndMinute
                    )

                    DatePicker(
                        "End Time",
                        selection: $quietHoursEnd,
                        displayedComponents: .hourAndMinute
                    )
                }
            } header: {
                Text("Do Not Disturb")
            } footer: {
                Text("Push notifications will be silenced during quiet hours.")
            }

            // Notification Categories
            if let prefs = preferences {
                ForEach(NotificationCategory.allCases, id: \.self) { category in
                    let categoryPrefs = getPreferencesForCategory(category, from: prefs)
                    if !categoryPrefs.isEmpty {
                        Section {
                            ForEach(categoryPrefs, id: \.notificationType) { pref in
                                NotificationPreferenceRow(
                                    preference: getCurrentPreference(for: pref.notificationType, original: pref),
                                    onInAppToggle: { enabled in
                                        updatePreference(pref.notificationType, channel: .inApp, enabled: enabled, original: pref)
                                    },
                                    onPushToggle: { enabled in
                                        updatePreference(pref.notificationType, channel: .push, enabled: enabled, original: pref)
                                    }
                                )
                            }
                        } header: {
                            Text(category.displayName)
                        } footer: {
                            Text(category.description)
                        }
                    }
                }
            }
        }
        .navigationTitle("Notifications")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .confirmationAction) {
                if hasChanges {
                    Button("Save") {
                        Task {
                            await saveChanges()
                        }
                    }
                    .disabled(isSaving)
                }
            }
        }
        .overlay {
            if isLoading {
                ProgressView()
            }
        }
        .alert("Error", isPresented: $showError) {
            Button("OK") { }
        } message: {
            Text(errorMessage)
        }
        .alert("Saved", isPresented: $showSuccess) {
            Button("OK") { }
        } message: {
            Text("Your notification settings have been saved.")
        }
        .task {
            await loadData()
        }
    }

    // MARK: - Private Methods

    private func loadData() async {
        isLoading = true
        defer { isLoading = false }

        // Check push notification status
        await PushNotificationService.shared.checkAuthorizationStatus()
        pushNotificationsEnabled = PushNotificationService.shared.isAuthorized

        // Load preferences
        do {
            let prefs = try await apiService.getNotificationPreferences()
            preferences = prefs
            quietHoursEnabled = prefs.quietHoursEnabled

            // Parse quiet hours times
            if let startStr = prefs.quietHoursStart {
                quietHoursStart = parseTime(startStr) ?? Date()
            }
            if let endStr = prefs.quietHoursEnd {
                quietHoursEnd = parseTime(endStr) ?? Date()
            }
        } catch {
            errorMessage = "Failed to load notification settings"
            showError = true
        }
    }

    private func requestPushPermission() async {
        let granted = await PushNotificationService.shared.requestAuthorization()
        pushNotificationsEnabled = granted

        if !granted {
            errorMessage = "Please enable notifications in Settings"
            showError = true
        }
    }

    private func getPreferencesForCategory(_ category: NotificationCategory, from prefs: NotificationPreferences) -> [NotificationPreferenceItem] {
        prefs.preferences.filter { category.notificationTypes.contains($0.notificationType) }
    }

    private func getCurrentPreference(for type: NotificationType, original: NotificationPreferenceItem) -> NotificationPreferenceItem {
        modifiedPreferences[type] ?? original
    }

    private func updatePreference(_ type: NotificationType, channel: NotificationChannel, enabled: Bool, original: NotificationPreferenceItem) {
        var current = modifiedPreferences[type] ?? original

        switch channel {
        case .inApp:
            current = NotificationPreferenceItem(
                notificationType: current.notificationType,
                typeName: current.typeName,
                category: current.category,
                inAppEnabled: enabled,
                pushEnabled: current.pushEnabled,
                emailEnabled: current.emailEnabled
            )
        case .push:
            current = NotificationPreferenceItem(
                notificationType: current.notificationType,
                typeName: current.typeName,
                category: current.category,
                inAppEnabled: current.inAppEnabled,
                pushEnabled: enabled,
                emailEnabled: current.emailEnabled
            )
        default:
            break
        }

        modifiedPreferences[type] = current
    }

    private var hasChanges: Bool {
        !modifiedPreferences.isEmpty ||
        quietHoursEnabled != (preferences?.quietHoursEnabled ?? false) ||
        (quietHoursEnabled && (
            formatTime(quietHoursStart) != preferences?.quietHoursStart ||
            formatTime(quietHoursEnd) != preferences?.quietHoursEnd
        ))
    }

    private func saveChanges() async {
        isSaving = true
        defer { isSaving = false }

        do {
            // Save preferences if modified
            if !modifiedPreferences.isEmpty {
                let updates = modifiedPreferences.values.map { pref in
                    UpdateNotificationPreferenceRequest(
                        notificationType: pref.notificationType,
                        inAppEnabled: pref.inAppEnabled,
                        pushEnabled: pref.pushEnabled,
                        emailEnabled: pref.emailEnabled
                    )
                }
                _ = try await apiService.updateNotificationPreferences(
                    UpdateNotificationPreferencesRequest(preferences: Array(updates))
                )
            }

            // Save quiet hours
            _ = try await apiService.updateQuietHours(
                UpdateQuietHoursRequest(
                    enabled: quietHoursEnabled,
                    startTime: quietHoursEnabled ? formatTime(quietHoursStart) : nil,
                    endTime: quietHoursEnabled ? formatTime(quietHoursEnd) : nil
                )
            )

            // Clear modified and reload
            modifiedPreferences = [:]
            await loadData()
            showSuccess = true

        } catch {
            errorMessage = "Failed to save settings"
            showError = true
        }
    }

    private func parseTime(_ timeStr: String) -> Date? {
        let formatter = DateFormatter()
        formatter.dateFormat = "HH:mm"
        return formatter.date(from: String(timeStr.prefix(5)))
    }

    private func formatTime(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "HH:mm"
        return formatter.string(from: date)
    }
}

// MARK: - Notification Preference Row

struct NotificationPreferenceRow: View {
    let preference: NotificationPreferenceItem
    let onInAppToggle: (Bool) -> Void
    let onPushToggle: (Bool) -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(preference.typeName)
                .font(.subheadline)

            HStack(spacing: 16) {
                Toggle(isOn: Binding(
                    get: { preference.inAppEnabled },
                    set: { onInAppToggle($0) }
                )) {
                    Label("In-App", systemImage: "bell.fill")
                        .font(.caption)
                }
                .toggleStyle(.button)
                .buttonStyle(.bordered)
                .tint(preference.inAppEnabled ? .blue : .gray)

                Toggle(isOn: Binding(
                    get: { preference.pushEnabled },
                    set: { onPushToggle($0) }
                )) {
                    Label("Push", systemImage: "iphone")
                        .font(.caption)
                }
                .toggleStyle(.button)
                .buttonStyle(.bordered)
                .tint(preference.pushEnabled ? .blue : .gray)
            }
        }
        .padding(.vertical, 4)
    }
}

// MARK: - Notification Category

enum NotificationCategory: CaseIterable {
    case balanceTransactions
    case allowance
    case goalsSavings
    case choresTasks
    case budget
    case achievements
    case family
    case reports

    var displayName: String {
        switch self {
        case .balanceTransactions: return "Balance & Transactions"
        case .allowance: return "Allowance"
        case .goalsSavings: return "Goals & Savings"
        case .choresTasks: return "Chores & Tasks"
        case .budget: return "Budget"
        case .achievements: return "Achievements"
        case .family: return "Family"
        case .reports: return "Reports"
        }
    }

    var description: String {
        switch self {
        case .balanceTransactions: return "Alerts about balance changes and transactions"
        case .allowance: return "Notifications about allowance deposits"
        case .goalsSavings: return "Updates on savings goals progress"
        case .choresTasks: return "Task assignments and approvals"
        case .budget: return "Budget warnings and alerts"
        case .achievements: return "Badge unlocks and streaks"
        case .family: return "Family invites and additions"
        case .reports: return "Weekly and monthly summaries"
        }
    }

    var notificationTypes: [NotificationType] {
        switch self {
        case .balanceTransactions:
            return [.balanceAlert, .lowBalanceWarning, .transactionCreated]
        case .allowance:
            return [.allowanceDeposit, .allowancePaused, .allowanceResumed]
        case .goalsSavings:
            return [.goalProgress, .goalMilestone, .goalCompleted, .parentMatchAdded]
        case .choresTasks:
            return [.taskAssigned, .taskReminder, .taskCompleted, .approvalRequired, .taskApproved, .taskRejected]
        case .budget:
            return [.budgetWarning, .budgetExceeded]
        case .achievements:
            return [.achievementUnlocked, .streakUpdate]
        case .family:
            return [.familyInvite, .childAdded, .giftReceived]
        case .reports:
            return [.weeklySummary, .monthlySummary]
        }
    }
}

// MARK: - Preview

#Preview {
    NavigationStack {
        NotificationSettingsView()
    }
}
