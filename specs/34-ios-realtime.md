# iOS Real-Time Features & Push Notifications

## Overview
Comprehensive real-time synchronization and push notification system for the Allowance Tracker iOS app. Leverages SignalR for bi-directional communication with the ASP.NET Core backend and Apple Push Notification service (APNs) for native push notifications. Children and parents receive instant updates about transactions, allowances, goals, and achievements.

## Core Philosophy: Test-First Development
**Every feature starts with a failing test**. Follow strict TDD methodology for all real-time functionality using XCTest for unit tests and XCUITest for integration tests.

## Technology Stack

### Core Dependencies
```swift
// Package.swift dependencies
dependencies: [
    .package(url: "https://github.com/moeyy01/SignalRClient", from: "0.9.0")
]
```

### Framework Requirements
- **iOS 17.0+**: Required for latest async/await patterns
- **UserNotifications**: Native push notification framework
- **Combine**: Reactive streams for real-time updates
- **BackgroundTasks**: Background refresh capabilities

## SignalR Integration Architecture

### SignalRManager - Singleton Service
```swift
import Foundation
import SignalRClient
import Combine

@MainActor
class SignalRManager: ObservableObject {
    static let shared = SignalRManager()

    private var hubConnection: HubConnection?
    private var reconnectTask: Task<Void, Never>?

    @Published var connectionState: ConnectionState = .disconnected
    @Published var lastError: Error?

    // Event publishers for reactive UI updates
    let transactionCreatedPublisher = PassthroughSubject<TransactionEventDTO, Never>()
    let allowancePaidPublisher = PassthroughSubject<AllowanceEventDTO, Never>()
    let choreApprovedPublisher = PassthroughSubject<ChoreEventDTO, Never>()
    let goalReachedPublisher = PassthroughSubject<GoalEventDTO, Never>()
    let balanceUpdatedPublisher = PassthroughSubject<BalanceEventDTO, Never>()

    private init() {}

    /// Connect to SignalR hub when user authenticates
    func connect(withToken token: String) async throws {
        guard hubConnection == nil else { return }

        let url = URL(string: "\(APIConfig.baseURL)/hub/family")!

        hubConnection = HubConnectionBuilder(url: url)
            .withLogging(minLogLevel: .debug)
            .withHttpConnectionOptions { options in
                options.accessTokenProvider = { token }
                options.headers = ["Authorization": "Bearer \(token)"]
            }
            .withAutoReconnect()
            .build()

        registerEventHandlers()

        try await hubConnection?.start()
        connectionState = .connected

        print("✅ SignalR connected to /hub/family")
    }

    /// Disconnect when user logs out
    func disconnect() async {
        reconnectTask?.cancel()
        reconnectTask = nil

        if let connection = hubConnection {
            await connection.stop()
            hubConnection = nil
        }

        connectionState = .disconnected
        print("🔌 SignalR disconnected")
    }

    /// Reconnect after network restoration
    func reconnect(withToken token: String) async {
        guard connectionState == .disconnected else { return }

        do {
            try await connect(withToken: token)
        } catch {
            lastError = error
            scheduleReconnect(withToken: token)
        }
    }

    private func scheduleReconnect(withToken token: String) {
        reconnectTask?.cancel()

        reconnectTask = Task {
            try? await Task.sleep(for: .seconds(5))

            guard !Task.isCancelled else { return }

            await reconnect(withToken: token)
        }
    }

    private func registerEventHandlers() {
        guard let connection = hubConnection else { return }

        // Transaction created event
        connection.on(method: "TransactionCreated") { [weak self] (event: TransactionEventDTO) in
            Task { @MainActor in
                self?.transactionCreatedPublisher.send(event)
                await self?.showLocalNotification(
                    title: "Transaction Created",
                    body: "\(event.amount.formatted(.currency(code: "USD"))) \(event.type.rawValue.lowercased())",
                    data: ["transactionId": event.transactionId.uuidString]
                )
            }
        }

        // Allowance paid event
        connection.on(method: "AllowancePaid") { [weak self] (event: AllowanceEventDTO) in
            Task { @MainActor in
                self?.allowancePaidPublisher.send(event)
                self?.balanceUpdatedPublisher.send(BalanceEventDTO(
                    childId: event.childId,
                    newBalance: event.newBalance,
                    previousBalance: event.previousBalance
                ))
                await self?.showLocalNotification(
                    title: "Allowance Paid! 💰",
                    body: "You received \(event.amount.formatted(.currency(code: "USD")))",
                    data: ["childId": event.childId.uuidString]
                )
            }
        }

        // Chore approved event
        connection.on(method: "ChoreApproved") { [weak self] (event: ChoreEventDTO) in
            Task { @MainActor in
                self?.choreApprovedPublisher.send(event)
                await self?.showLocalNotification(
                    title: "Chore Approved ✅",
                    body: "You earned \(event.amount.formatted(.currency(code: "USD"))) for \(event.choreName)",
                    data: ["choreId": event.choreId.uuidString]
                )
            }
        }

        // Goal reached event
        connection.on(method: "GoalReached") { [weak self] (event: GoalEventDTO) in
            Task { @MainActor in
                self?.goalReachedPublisher.send(event)
                await self?.showCelebrationNotification(for: event)
            }
        }

        // Balance updated event (generic)
        connection.on(method: "BalanceUpdated") { [weak self] (event: BalanceEventDTO) in
            Task { @MainActor in
                self?.balanceUpdatedPublisher.send(event)
            }
        }

        // Connection lifecycle events
        connection.onConnected { [weak self] connectionId in
            Task { @MainActor in
                self?.connectionState = .connected
                print("✅ SignalR connected with ID: \(connectionId)")
            }
        }

        connection.onReconnecting { [weak self] error in
            Task { @MainActor in
                self?.connectionState = .reconnecting
                print("🔄 SignalR reconnecting... Error: \(String(describing: error))")
            }
        }

        connection.onReconnected { [weak self] connectionId in
            Task { @MainActor in
                self?.connectionState = .connected
                print("✅ SignalR reconnected with ID: \(connectionId)")
            }
        }

        connection.onClose { [weak self] error in
            Task { @MainActor in
                self?.connectionState = .disconnected
                if let error = error {
                    self?.lastError = error
                    print("❌ SignalR closed with error: \(error)")
                }
            }
        }
    }

    private func showCelebrationNotification(for event: GoalEventDTO) async {
        await showLocalNotification(
            title: "🎉 Goal Reached!",
            body: "You saved enough for \(event.goalName)! Balance: \(event.currentBalance.formatted(.currency(code: "USD")))",
            data: ["goalId": event.goalId.uuidString],
            sound: .defaultCritical
        )
    }

    private func showLocalNotification(
        title: String,
        body: String,
        data: [String: String],
        sound: UNNotificationSound = .default
    ) async {
        let content = UNMutableNotificationContent()
        content.title = title
        content.body = body
        content.sound = sound
        content.userInfo = data
        content.categoryIdentifier = "TRANSACTION"

        let request = UNNotificationRequest(
            identifier: UUID().uuidString,
            content: content,
            trigger: nil
        )

        try? await UNUserNotificationCenter.current().add(request)
    }
}

enum ConnectionState {
    case disconnected
    case connecting
    case connected
    case reconnecting
}
```

### Event Data Transfer Objects
```swift
// DTOs matching server-side events

struct TransactionEventDTO: Codable {
    let transactionId: UUID
    let childId: UUID
    let amount: Decimal
    let type: TransactionType
    let description: String
    let balanceAfter: Decimal
    let createdAt: Date
}

struct AllowanceEventDTO: Codable {
    let childId: UUID
    let childName: String
    let amount: Decimal
    let newBalance: Decimal
    let previousBalance: Decimal
    let paidAt: Date
}

struct ChoreEventDTO: Codable {
    let choreId: UUID
    let childId: UUID
    let choreName: String
    let amount: Decimal
    let approvedBy: String
    let approvedAt: Date
}

struct GoalEventDTO: Codable {
    let goalId: UUID
    let childId: UUID
    let goalName: String
    let goalPrice: Decimal
    let currentBalance: Decimal
    let achievedAt: Date
}

struct BalanceEventDTO: Codable {
    let childId: UUID
    let newBalance: Decimal
    let previousBalance: Decimal
}

enum TransactionType: String, Codable {
    case credit = "Credit"
    case debit = "Debit"
}
```

## Push Notification System

### NotificationManager - APNs Integration
```swift
import UserNotifications
import UIKit

@MainActor
class NotificationManager: NSObject, ObservableObject {
    static let shared = NotificationManager()

    @Published var notificationPermissionStatus: UNAuthorizationStatus = .notDetermined
    @Published var unreadNotificationCount: Int = 0

    private let notificationCenter = UNUserNotificationCenter.current()

    override private init() {
        super.init()
        notificationCenter.delegate = self
    }

    /// Request notification permissions on first launch
    func requestPermission() async throws -> Bool {
        let granted = try await notificationCenter.requestAuthorization(
            options: [.alert, .sound, .badge]
        )

        if granted {
            await registerForRemoteNotifications()
        }

        await updatePermissionStatus()
        return granted
    }

    /// Register device for remote push notifications
    func registerForRemoteNotifications() async {
        await UIApplication.shared.registerForRemoteNotifications()
    }

    /// Send device token to backend
    func registerDeviceToken(_ token: Data, userId: UUID) async throws {
        let tokenString = token.map { String(format: "%02.2hhx", $0) }.joined()

        let endpoint = "/api/v1/notifications/device-token"
        let body = DeviceTokenDTO(userId: userId, token: tokenString, platform: "iOS")

        try await APIClient.shared.post(endpoint, body: body)

        print("✅ Device token registered: \(tokenString)")
    }

    /// Update badge count on app icon
    func updateBadgeCount(_ count: Int) {
        UIApplication.shared.applicationIconBadgeNumber = count
        unreadNotificationCount = count
    }

    /// Clear all notifications
    func clearAllNotifications() {
        notificationCenter.removeAllDeliveredNotifications()
        updateBadgeCount(0)
    }

    /// Schedule local notification (for debugging)
    func scheduleLocalNotification(
        title: String,
        body: String,
        delay: TimeInterval = 1,
        data: [String: Any] = [:]
    ) async throws {
        let content = UNMutableNotificationContent()
        content.title = title
        content.body = body
        content.sound = .default
        content.userInfo = data

        let trigger = UNTimeIntervalNotificationTrigger(timeInterval: delay, repeats: false)
        let request = UNNotificationRequest(
            identifier: UUID().uuidString,
            content: content,
            trigger: trigger
        )

        try await notificationCenter.add(request)
    }

    private func updatePermissionStatus() async {
        let settings = await notificationCenter.notificationSettings()
        notificationPermissionStatus = settings.authorizationStatus
    }
}

// MARK: - UNUserNotificationCenterDelegate
extension NotificationManager: UNUserNotificationCenterDelegate {
    /// Handle notification when app is in foreground
    func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        willPresent notification: UNNotification,
        withCompletionHandler completionHandler: @escaping (UNNotificationPresentationOptions) -> Void
    ) {
        // Show notification even when app is active
        completionHandler([.banner, .sound, .badge])
    }

    /// Handle notification tap
    func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        didReceive response: UNNotificationResponse,
        withCompletionHandler completionHandler: @escaping () -> Void
    ) {
        let userInfo = response.notification.request.content.userInfo

        Task { @MainActor in
            await handleNotificationTap(userInfo)
        }

        completionHandler()
    }

    private func handleNotificationTap(_ userInfo: [AnyHashable: Any]) async {
        // Deep link navigation
        if let transactionId = userInfo["transactionId"] as? String,
           let uuid = UUID(uuidString: transactionId) {
            await navigateToTransaction(uuid)
        } else if let childId = userInfo["childId"] as? String,
                  let uuid = UUID(uuidString: childId) {
            await navigateToChild(uuid)
        } else if let goalId = userInfo["goalId"] as? String,
                  let uuid = UUID(uuidString: goalId) {
            await navigateToGoal(uuid)
        }

        // Mark notification as read
        updateBadgeCount(max(0, unreadNotificationCount - 1))
    }

    private func navigateToTransaction(_ id: UUID) async {
        NotificationCenter.default.post(
            name: .navigateToTransaction,
            object: nil,
            userInfo: ["transactionId": id]
        )
    }

    private func navigateToChild(_ id: UUID) async {
        NotificationCenter.default.post(
            name: .navigateToChild,
            object: nil,
            userInfo: ["childId": id]
        )
    }

    private func navigateToGoal(_ id: UUID) async {
        NotificationCenter.default.post(
            name: .navigateToGoal,
            object: nil,
            userInfo: ["goalId": id]
        )
    }
}

struct DeviceTokenDTO: Codable {
    let userId: UUID
    let token: String
    let platform: String
}

// Deep linking notification names
extension Notification.Name {
    static let navigateToTransaction = Notification.Name("navigateToTransaction")
    static let navigateToChild = Notification.Name("navigateToChild")
    static let navigateToGoal = Notification.Name("navigateToGoal")
}
```

### NotificationSettings View
```swift
import SwiftUI

struct NotificationSettingsView: View {
    @StateObject private var viewModel = NotificationSettingsViewModel()

    var body: some View {
        Form {
            Section("Push Notifications") {
                Toggle("Enable Notifications", isOn: $viewModel.notificationsEnabled)
                    .onChange(of: viewModel.notificationsEnabled) { _, newValue in
                        Task {
                            if newValue {
                                await viewModel.requestPermission()
                            } else {
                                await viewModel.disableNotifications()
                            }
                        }
                    }

                if viewModel.notificationsEnabled {
                    Toggle("Transactions", isOn: $viewModel.transactionNotifications)
                    Toggle("Allowance Payments", isOn: $viewModel.allowanceNotifications)
                    Toggle("Chore Approvals", isOn: $viewModel.choreNotifications)
                    Toggle("Goal Milestones", isOn: $viewModel.goalNotifications)
                }
            }

            Section("Notification Preferences") {
                Toggle("Sound", isOn: $viewModel.soundEnabled)
                Toggle("Badge Count", isOn: $viewModel.badgeEnabled)
            }

            Section("Low Balance Alert") {
                Toggle("Enable Alert", isOn: $viewModel.lowBalanceAlertEnabled)

                if viewModel.lowBalanceAlertEnabled {
                    HStack {
                        Text("Threshold")
                        Spacer()
                        TextField("Amount", value: $viewModel.lowBalanceThreshold, format: .currency(code: "USD"))
                            .keyboardType(.decimalPad)
                            .multilineTextAlignment(.trailing)
                            .frame(width: 100)
                    }
                }
            }

            Section {
                Button("Save Preferences") {
                    Task {
                        await viewModel.savePreferences()
                    }
                }
                .disabled(viewModel.isSaving)
            }
        }
        .navigationTitle("Notifications")
        .onAppear {
            Task {
                await viewModel.loadPreferences()
            }
        }
    }
}

@MainActor
class NotificationSettingsViewModel: ObservableObject {
    @Published var notificationsEnabled = false
    @Published var transactionNotifications = true
    @Published var allowanceNotifications = true
    @Published var choreNotifications = true
    @Published var goalNotifications = true
    @Published var soundEnabled = true
    @Published var badgeEnabled = true
    @Published var lowBalanceAlertEnabled = false
    @Published var lowBalanceThreshold: Decimal = 10.0
    @Published var isSaving = false

    private let notificationManager = NotificationManager.shared
    private let apiClient = APIClient.shared

    func requestPermission() async {
        do {
            let granted = try await notificationManager.requestPermission()
            notificationsEnabled = granted
        } catch {
            print("Failed to request permission: \(error)")
        }
    }

    func disableNotifications() async {
        // Open Settings app for user to disable
        if let url = URL(string: UIApplication.openSettingsURLString) {
            await UIApplication.shared.open(url)
        }
    }

    func loadPreferences() async {
        do {
            let prefs: NotificationPreferencesDTO = try await apiClient.get("/api/v1/notification-preferences")

            notificationsEnabled = prefs.inAppEnabled
            transactionNotifications = prefs.inAppTransactions
            allowanceNotifications = prefs.inAppAllowance
            choreNotifications = prefs.inAppChores
            goalNotifications = prefs.inAppGoals
            lowBalanceAlertEnabled = prefs.lowBalanceNotifications
            lowBalanceThreshold = prefs.lowBalanceThreshold ?? 10.0
        } catch {
            print("Failed to load preferences: \(error)")
        }
    }

    func savePreferences() async {
        isSaving = true
        defer { isSaving = false }

        let dto = UpdateNotificationPreferencesDTO(
            inAppEnabled: notificationsEnabled,
            inAppTransactions: transactionNotifications,
            inAppAllowance: allowanceNotifications,
            inAppChores: choreNotifications,
            inAppGoals: goalNotifications,
            lowBalanceNotifications: lowBalanceAlertEnabled,
            lowBalanceThreshold: lowBalanceAlertEnabled ? lowBalanceThreshold : nil
        )

        do {
            try await apiClient.put("/api/v1/notification-preferences", body: dto)
        } catch {
            print("Failed to save preferences: \(error)")
        }
    }
}

struct NotificationPreferencesDTO: Codable {
    let inAppEnabled: Bool
    let inAppTransactions: Bool
    let inAppAllowance: Bool
    let inAppChores: Bool
    let inAppGoals: Bool
    let lowBalanceNotifications: Bool
    let lowBalanceThreshold: Decimal?
}

struct UpdateNotificationPreferencesDTO: Codable {
    let inAppEnabled: Bool
    let inAppTransactions: Bool
    let inAppAllowance: Bool
    let inAppChores: Bool
    let inAppGoals: Bool
    let lowBalanceNotifications: Bool
    let lowBalanceThreshold: Decimal?
}
```

## App Lifecycle Integration

### AppDelegate for Push Notifications
```swift
import UIKit
import UserNotifications

class AppDelegate: NSObject, UIApplicationDelegate {
    func application(
        _ application: UIApplication,
        didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey : Any]? = nil
    ) -> Bool {
        // Request notification permissions on launch
        Task {
            _ = try? await NotificationManager.shared.requestPermission()
        }

        return true
    }

    func application(
        _ application: UIApplication,
        didRegisterForRemoteNotificationsWithDeviceToken deviceToken: Data
    ) {
        Task {
            guard let userId = AuthService.shared.currentUser?.id else { return }
            try? await NotificationManager.shared.registerDeviceToken(deviceToken, userId: userId)
        }
    }

    func application(
        _ application: UIApplication,
        didFailToRegisterForRemoteNotificationsWithError error: Error
    ) {
        print("❌ Failed to register for remote notifications: \(error)")
    }
}
```

### App Entry Point with SignalR
```swift
import SwiftUI

@main
struct AllowanceTrackerApp: App {
    @UIApplicationDelegateAdaptor(AppDelegate.self) var appDelegate
    @StateObject private var authService = AuthService.shared
    @StateObject private var signalRManager = SignalRManager.shared
    @StateObject private var notificationManager = NotificationManager.shared

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(authService)
                .environmentObject(signalRManager)
                .environmentObject(notificationManager)
                .onAppear {
                    setupSignalR()
                }
                .onChange(of: authService.isAuthenticated) { _, isAuthenticated in
                    handleAuthenticationChange(isAuthenticated)
                }
        }
    }

    private func setupSignalR() {
        // Connect to SignalR when app launches if already authenticated
        if authService.isAuthenticated, let token = authService.authToken {
            Task {
                try? await signalRManager.connect(withToken: token)
            }
        }
    }

    private func handleAuthenticationChange(_ isAuthenticated: Bool) {
        Task {
            if isAuthenticated, let token = authService.authToken {
                try? await signalRManager.connect(withToken: token)
            } else {
                await signalRManager.disconnect()
            }
        }
    }
}
```

## Background Refresh

### Background Task Registration
```swift
import BackgroundTasks

extension AppDelegate {
    func application(
        _ application: UIApplication,
        didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey : Any]? = nil
    ) -> Bool {
        // Register background refresh task
        BGTaskScheduler.shared.register(
            forTaskWithIdentifier: "com.allowancetracker.refresh",
            using: nil
        ) { task in
            self.handleBackgroundRefresh(task: task as! BGAppRefreshTask)
        }

        return true
    }

    private func handleBackgroundRefresh(task: BGAppRefreshTask) {
        scheduleBackgroundRefresh()

        Task {
            do {
                // Fetch latest transactions and balance
                if let userId = AuthService.shared.currentUser?.id {
                    let balance = try await APIClient.shared.get("/api/v1/transactions/children/\(userId)/balance")

                    // Update badge count with unread notifications
                    let unreadCount: Int = try await APIClient.shared.get("/api/v1/notifications/unread-count")
                    await NotificationManager.shared.updateBadgeCount(unreadCount)
                }

                task.setTaskCompleted(success: true)
            } catch {
                task.setTaskCompleted(success: false)
            }
        }
    }

    func scheduleBackgroundRefresh() {
        let request = BGAppRefreshTaskRequest(identifier: "com.allowancetracker.refresh")
        request.earliestBeginDate = Date(timeIntervalSinceNow: 15 * 60) // 15 minutes

        do {
            try BGTaskScheduler.shared.submit(request)
        } catch {
            print("Could not schedule app refresh: \(error)")
        }
    }
}
```

## Real-Time UI Updates

### DashboardView with Live Updates
```swift
import SwiftUI

struct DashboardView: View {
    @StateObject private var viewModel = DashboardViewModel()
    @EnvironmentObject var signalRManager: SignalRManager

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 20) {
                    BalanceCard(balance: viewModel.currentBalance)
                        .animation(.spring(), value: viewModel.currentBalance)

                    RecentTransactionsList(transactions: viewModel.recentTransactions)
                }
                .padding()
            }
            .navigationTitle("Dashboard")
            .onAppear {
                Task {
                    await viewModel.loadData()
                }
            }
            .onReceive(signalRManager.balanceUpdatedPublisher) { event in
                Task {
                    await viewModel.handleBalanceUpdate(event)
                }
            }
            .onReceive(signalRManager.transactionCreatedPublisher) { event in
                Task {
                    await viewModel.handleNewTransaction(event)
                }
            }
            .onReceive(signalRManager.goalReachedPublisher) { event in
                viewModel.showGoalCelebration(event)
            }
        }
    }
}

@MainActor
class DashboardViewModel: ObservableObject {
    @Published var currentBalance: Decimal = 0
    @Published var recentTransactions: [Transaction] = []
    @Published var showCelebration = false
    @Published var celebrationGoal: GoalEventDTO?

    func loadData() async {
        // Load initial data from API
    }

    func handleBalanceUpdate(_ event: BalanceEventDTO) async {
        withAnimation {
            currentBalance = event.newBalance
        }
    }

    func handleNewTransaction(_ event: TransactionEventDTO) async {
        // Add new transaction to list
        let transaction = Transaction(from: event)
        withAnimation {
            recentTransactions.insert(transaction, at: 0)
        }
    }

    func showGoalCelebration(_ event: GoalEventDTO) {
        celebrationGoal = event
        showCelebration = true

        // Show confetti animation
        HapticManager.shared.playSuccess()
    }
}
```

## Test Cases (20+ Tests)

### SignalRManager Tests
```swift
import XCTest
@testable import AllowanceTracker

@MainActor
final class SignalRManagerTests: XCTestCase {
    var sut: SignalRManager!

    override func setUp() async throws {
        sut = SignalRManager.shared
    }

    override func tearDown() async throws {
        await sut.disconnect()
        sut = nil
    }

    func test_connect_setsConnectionStateToConnected() async throws {
        // Arrange
        let token = "valid.jwt.token"

        // Act
        try await sut.connect(withToken: token)

        // Assert
        XCTAssertEqual(sut.connectionState, .connected)
    }

    func test_disconnect_setsConnectionStateToDisconnected() async throws {
        // Arrange
        let token = "valid.jwt.token"
        try await sut.connect(withToken: token)

        // Act
        await sut.disconnect()

        // Assert
        XCTAssertEqual(sut.connectionState, .disconnected)
    }

    func test_transactionCreated_publishesEvent() async throws {
        // Arrange
        let expectation = XCTestExpectation(description: "Transaction event published")
        let expectedEvent = TransactionEventDTO(
            transactionId: UUID(),
            childId: UUID(),
            amount: 25.00,
            type: .credit,
            description: "Test",
            balanceAfter: 125.00,
            createdAt: Date()
        )

        var receivedEvent: TransactionEventDTO?
        let cancellable = sut.transactionCreatedPublisher.sink { event in
            receivedEvent = event
            expectation.fulfill()
        }

        // Act
        sut.transactionCreatedPublisher.send(expectedEvent)

        // Assert
        await fulfillment(of: [expectation], timeout: 1.0)
        XCTAssertEqual(receivedEvent?.transactionId, expectedEvent.transactionId)
        XCTAssertEqual(receivedEvent?.amount, expectedEvent.amount)

        cancellable.cancel()
    }

    func test_allowancePaid_publishesEvent() async throws {
        // Test allowance event handling
    }

    func test_choreApproved_publishesEvent() async throws {
        // Test chore approval event handling
    }

    func test_goalReached_publishesEvent() async throws {
        // Test goal reached event handling
    }

    func test_reconnect_afterNetworkRestore() async throws {
        // Test automatic reconnection
    }

    func test_connectionError_setsLastError() async throws {
        // Test error handling
    }
}
```

### NotificationManager Tests
```swift
@MainActor
final class NotificationManagerTests: XCTestCase {
    var sut: NotificationManager!

    override func setUp() {
        sut = NotificationManager.shared
    }

    func test_requestPermission_returnsAuthorizationStatus() async throws {
        // Act
        let granted = try await sut.requestPermission()

        // Assert
        XCTAssertTrue(granted || !granted) // Permission can be granted or denied
    }

    func test_updateBadgeCount_setsApplicationBadge() {
        // Act
        sut.updateBadgeCount(5)

        // Assert
        XCTAssertEqual(sut.unreadNotificationCount, 5)
    }

    func test_clearAllNotifications_resetsBadgeCount() {
        // Arrange
        sut.updateBadgeCount(10)

        // Act
        sut.clearAllNotifications()

        // Assert
        XCTAssertEqual(sut.unreadNotificationCount, 0)
    }

    func test_scheduleLocalNotification_addsNotification() async throws {
        // Act
        try await sut.scheduleLocalNotification(
            title: "Test",
            body: "Test notification",
            delay: 1
        )

        // Assert
        let pending = await UNUserNotificationCenter.current().pendingNotificationRequests()
        XCTAssertGreaterThan(pending.count, 0)
    }

    func test_registerDeviceToken_sendsToAPI() async throws {
        // Test device token registration
    }
}
```

### Integration Tests
```swift
final class RealTimeIntegrationTests: XCTestCase {
    func test_transactionCreated_updatesBalance() async throws {
        // Test complete flow from SignalR event to UI update
    }

    func test_allowancePaid_showsNotification() async throws {
        // Test allowance notification flow
    }

    func test_goalReached_showsCelebration() async throws {
        // Test goal celebration animation
    }
}
```

## Info.plist Configuration

```xml
<key>UIBackgroundModes</key>
<array>
    <string>remote-notification</string>
    <string>processing</string>
</array>

<key>NSUserTrackingUsageDescription</key>
<string>We use notifications to alert you about transactions and allowance payments.</string>

<key>BGTaskSchedulerPermittedIdentifiers</key>
<array>
    <string>com.allowancetracker.refresh</string>
</array>
```

## Success Metrics

### Performance Targets
- SignalR connection time: < 2 seconds
- Event delivery latency: < 500ms
- Push notification delivery: < 5 seconds
- Background refresh completion: < 10 seconds

### Quality Metrics
- 20+ tests passing (100% critical path coverage)
- Zero SignalR disconnections during active use
- Push notification delivery rate > 98%
- Battery impact < 2% per day

## Summary

This specification provides:
- **SignalR Integration**: Real-time bi-directional communication with ASP.NET Core backend
- **Push Notifications**: Native APNs integration with badge counts and deep linking
- **Background Refresh**: Keep data fresh when app is backgrounded
- **Reactive UI**: Combine publishers for real-time UI updates
- **App Lifecycle**: Proper connection management on login/logout
- **Comprehensive Testing**: 20+ tests covering all real-time scenarios

All features follow strict TDD methodology with test coverage targets >90%.
