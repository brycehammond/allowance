# iOS App Specification - Allowance Tracker

## Overview

Native iOS application for Allowance Tracker built with SwiftUI and Swift 5.9+. Provides parents and children with a mobile interface to manage allowances, track transactions, and monitor financial goals.

**Target iOS Version**: iOS 17.0+
**Primary Framework**: SwiftUI
**Architecture**: MVVM (Model-View-ViewModel)
**Networking**: URLSession with async/await
**Real-time**: SignalR Swift client
**Persistence**: SwiftData (iOS 17+)
**Authentication**: JWT with Keychain storage

## Architecture Overview

### MVVM Pattern
```
View (SwiftUI)
    ↓ User Actions
ViewModel (ObservableObject)
    ↓ Business Logic
Service Layer (API, SignalR, Storage)
    ↓ Data Operations
Models (Codable, Identifiable)
```

### Project Structure
```
AllowanceTracker/
├── App/
│   ├── AllowanceTrackerApp.swift      # App entry point
│   └── AppState.swift                 # Global app state
├── Models/
│   ├── User.swift
│   ├── Child.swift
│   ├── Transaction.swift
│   ├── WishListItem.swift
│   └── Analytics/
│       ├── BalancePoint.swift
│       ├── IncomeSpending.swift
│       └── MonthlyComparison.swift
├── ViewModels/
│   ├── AuthViewModel.swift
│   ├── DashboardViewModel.swift
│   ├── TransactionViewModel.swift
│   ├── WishListViewModel.swift
│   └── AnalyticsViewModel.swift
├── Views/
│   ├── Auth/
│   │   ├── LoginView.swift
│   │   └── RegisterView.swift
│   ├── Dashboard/
│   │   ├── DashboardView.swift
│   │   ├── ChildCardView.swift
│   │   └── QuickTransactionSheet.swift
│   ├── Transactions/
│   │   ├── TransactionListView.swift
│   │   └── TransactionDetailView.swift
│   ├── WishList/
│   │   ├── WishListView.swift
│   │   └── WishListItemRow.swift
│   ├── Analytics/
│   │   ├── AnalyticsView.swift
│   │   └── Charts/
│   └── Shared/
│       ├── LoadingView.swift
│       └── ErrorView.swift
├── Services/
│   ├── Network/
│   │   ├── APIService.swift
│   │   ├── AuthInterceptor.swift
│   │   └── Endpoints.swift
│   ├── SignalR/
│   │   └── SignalRService.swift
│   ├── Storage/
│   │   ├── KeychainService.swift
│   │   └── CacheService.swift
│   └── Analytics/
│       └── AnalyticsService.swift
├── Utilities/
│   ├── Extensions/
│   │   ├── Date+Extensions.swift
│   │   ├── Decimal+Extensions.swift
│   │   └── View+Extensions.swift
│   ├── Constants.swift
│   └── Formatters.swift
└── Tests/
    ├── Unit/
    │   ├── ViewModelTests/
    │   └── ServiceTests/
    └── UI/
        └── SnapshotTests/
```

## Core Models

### User Model
```swift
struct User: Codable, Identifiable {
    let id: UUID
    let email: String
    let firstName: String
    let lastName: String
    let role: UserRole
    let familyId: UUID?

    var fullName: String {
        "\(firstName) \(lastName)"
    }
}

enum UserRole: String, Codable {
    case parent = "Parent"
    case child = "Child"
}
```

### Child Model
```swift
struct Child: Codable, Identifiable {
    let id: UUID
    let firstName: String
    let lastName: String
    let weeklyAllowance: Decimal
    let currentBalance: Decimal
    let lastAllowanceDate: Date?

    var fullName: String {
        "\(firstName) \(lastName)"
    }

    var formattedBalance: String {
        currentBalance.currencyFormatted
    }
}
```

### Transaction Model
```swift
struct Transaction: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let amount: Decimal
    let type: TransactionType
    let description: String
    let balanceAfter: Decimal
    let createdAt: Date
    let createdByName: String

    var isCredit: Bool {
        type == .credit
    }

    var formattedAmount: String {
        let prefix = isCredit ? "+" : "-"
        return "\(prefix)\(amount.currencyFormatted)"
    }
}

enum TransactionType: String, Codable {
    case credit = "Credit"
    case debit = "Debit"
}
```

### WishListItem Model
```swift
struct WishListItem: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let name: String
    let price: Decimal
    let url: String?
    let notes: String?
    let isPurchased: Bool
    let purchasedAt: Date?
    let createdAt: Date
    let canAfford: Bool

    var progressPercentage: Double {
        // Calculate based on child's balance vs. price
        0.0 // Placeholder
    }
}
```

### Analytics Models
```swift
struct BalancePoint: Codable, Identifiable {
    let id = UUID()
    let date: Date
    let balance: Decimal
    let transactionDescription: String?
}

struct IncomeSpendingSummary: Codable {
    let totalIncome: Decimal
    let totalSpending: Decimal
    let netSavings: Decimal
    let incomeTransactionCount: Int
    let spendingTransactionCount: Int
    let savingsRate: Decimal
}

struct MonthlyComparison: Codable, Identifiable {
    let id = UUID()
    let year: Int
    let month: Int
    let monthName: String
    let income: Decimal
    let spending: Decimal
    let netSavings: Decimal
    let endingBalance: Decimal
}
```

## API Service Layer

### Base API Service
```swift
protocol APIServiceProtocol {
    func request<T: Decodable>(
        endpoint: Endpoint,
        method: HTTPMethod,
        body: Encodable?
    ) async throws -> T
}

final class APIService: APIServiceProtocol {
    private let baseURL: URL
    private let session: URLSession
    private let keychainService: KeychainService

    init(
        baseURL: URL = Constants.API.baseURL,
        session: URLSession = .shared,
        keychainService: KeychainService = .shared
    ) {
        self.baseURL = baseURL
        self.session = session
        self.keychainService = keychainService
    }

    func request<T: Decodable>(
        endpoint: Endpoint,
        method: HTTPMethod = .get,
        body: Encodable? = nil
    ) async throws -> T {
        var request = URLRequest(url: baseURL.appendingPathComponent(endpoint.path))
        request.httpMethod = method.rawValue
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        // Add JWT token if available
        if let token = try? keychainService.getToken() {
            request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        }

        // Add body if present
        if let body = body {
            request.httpBody = try JSONEncoder().encode(body)
        }

        let (data, response) = try await session.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.invalidResponse
        }

        guard (200...299).contains(httpResponse.statusCode) else {
            throw APIError.httpError(httpResponse.statusCode)
        }

        return try JSONDecoder.default.decode(T.self, from: data)
    }
}
```

### Endpoints
```swift
enum Endpoint {
    // Auth
    case login
    case register
    case currentUser

    // Children
    case children
    case child(UUID)
    case childTransactions(UUID)

    // Transactions
    case createTransaction

    // WishList
    case wishList(childId: UUID)
    case wishListItem(UUID)
    case purchaseItem(UUID)

    // Analytics
    case balanceHistory(childId: UUID, days: Int)
    case incomeVsSpending(childId: UUID)
    case spendingBreakdown(childId: UUID)
    case monthlyComparison(childId: UUID, months: Int)

    var path: String {
        switch self {
        case .login:
            return "/api/v1/auth/login"
        case .register:
            return "/api/v1/auth/register"
        case .currentUser:
            return "/api/v1/auth/me"
        case .children:
            return "/api/v1/children"
        case .child(let id):
            return "/api/v1/children/\(id.uuidString)"
        case .childTransactions(let id):
            return "/api/v1/children/\(id.uuidString)/transactions"
        case .createTransaction:
            return "/api/v1/transactions"
        case .wishList(let childId):
            return "/api/v1/wishlist/children/\(childId.uuidString)"
        case .wishListItem(let id):
            return "/api/v1/wishlist/\(id.uuidString)"
        case .purchaseItem(let id):
            return "/api/v1/wishlist/\(id.uuidString)/purchase"
        case .balanceHistory(let childId, let days):
            return "/api/v1/analytics/children/\(childId.uuidString)/balance-history?days=\(days)"
        case .incomeVsSpending(let childId):
            return "/api/v1/analytics/children/\(childId.uuidString)/income-spending"
        case .spendingBreakdown(let childId):
            return "/api/v1/analytics/children/\(childId.uuidString)/spending-breakdown"
        case .monthlyComparison(let childId, let months):
            return "/api/v1/analytics/children/\(childId.uuidString)/monthly-comparison?months=\(months)"
        }
    }
}

enum HTTPMethod: String {
    case get = "GET"
    case post = "POST"
    case put = "PUT"
    case delete = "DELETE"
}
```

## Authentication Flow

### AuthViewModel
```swift
@MainActor
final class AuthViewModel: ObservableObject {
    @Published var currentUser: User?
    @Published var isAuthenticated = false
    @Published var isLoading = false
    @Published var errorMessage: String?

    private let apiService: APIService
    private let keychainService: KeychainService

    init(
        apiService: APIService = .shared,
        keychainService: KeychainService = .shared
    ) {
        self.apiService = apiService
        self.keychainService = keychainService

        // Check for existing session
        Task {
            await checkAuthentication()
        }
    }

    func login(email: String, password: String) async {
        isLoading = true
        errorMessage = nil

        do {
            let request = LoginRequest(email: email, password: password)
            let response: AuthResponse = try await apiService.request(
                endpoint: .login,
                method: .post,
                body: request
            )

            // Store token
            try keychainService.saveToken(response.token)

            // Store user
            currentUser = response.user
            isAuthenticated = true

        } catch {
            errorMessage = error.localizedDescription
        }

        isLoading = false
    }

    func register(
        email: String,
        password: String,
        firstName: String,
        lastName: String,
        role: UserRole
    ) async {
        isLoading = true
        errorMessage = nil

        do {
            let request = RegisterRequest(
                email: email,
                password: password,
                firstName: firstName,
                lastName: lastName,
                role: role
            )

            let response: AuthResponse = try await apiService.request(
                endpoint: .register,
                method: .post,
                body: request
            )

            try keychainService.saveToken(response.token)
            currentUser = response.user
            isAuthenticated = true

        } catch {
            errorMessage = error.localizedDescription
        }

        isLoading = false
    }

    func logout() {
        try? keychainService.deleteToken()
        currentUser = nil
        isAuthenticated = false
    }

    private func checkAuthentication() async {
        guard let token = try? keychainService.getToken() else {
            return
        }

        do {
            let user: User = try await apiService.request(
                endpoint: .currentUser,
                method: .get,
                body: nil as String?
            )
            currentUser = user
            isAuthenticated = true
        } catch {
            try? keychainService.deleteToken()
        }
    }
}
```

### Keychain Service
```swift
final class KeychainService {
    static let shared = KeychainService()

    private let tokenKey = "com.allowancetracker.jwt"

    func saveToken(_ token: String) throws {
        let data = Data(token.utf8)

        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrAccount as String: tokenKey,
            kSecValueData as String: data
        ]

        // Delete existing item
        SecItemDelete(query as CFDictionary)

        // Add new item
        let status = SecItemAdd(query as CFDictionary, nil)

        guard status == errSecSuccess else {
            throw KeychainError.saveFailed(status)
        }
    }

    func getToken() throws -> String {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrAccount as String: tokenKey,
            kSecReturnData as String: true
        ]

        var result: AnyObject?
        let status = SecItemCopyMatching(query as CFDictionary, &result)

        guard status == errSecSuccess,
              let data = result as? Data,
              let token = String(data: data, encoding: .utf8) else {
            throw KeychainError.notFound
        }

        return token
    }

    func deleteToken() throws {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrAccount as String: tokenKey
        ]

        let status = SecItemDelete(query as CFDictionary)

        guard status == errSecSuccess || status == errSecItemNotFound else {
            throw KeychainError.deleteFailed(status)
        }
    }
}
```

## Real-time Updates with SignalR

### SignalR Service
```swift
import SignalRClient

final class SignalRService: ObservableObject {
    @Published var isConnected = false

    private var hubConnection: HubConnection?
    private let keychainService: KeychainService

    init(keychainService: KeychainService = .shared) {
        self.keychainService = keychainService
    }

    func connect(familyId: UUID) async throws {
        guard let token = try? keychainService.getToken() else {
            throw SignalRError.noToken
        }

        hubConnection = HubConnectionBuilder(url: Constants.SignalR.hubURL)
            .withLogging(logLevel: .debug)
            .withHttpConnectionOptions { options in
                options.accessTokenProvider = { token }
            }
            .withAutoReconnect()
            .build()

        // Register event handlers
        hubConnection?.on(method: "TransactionCreated") { [weak self] (transactionId: String) in
            await self?.handleTransactionCreated(transactionId: transactionId)
        }

        hubConnection?.on(method: "BalanceUpdated") { [weak self] (childId: String, newBalance: Decimal) in
            await self?.handleBalanceUpdated(childId: childId, balance: newBalance)
        }

        try await hubConnection?.start()
        isConnected = true
    }

    func disconnect() async {
        await hubConnection?.stop()
        isConnected = false
    }

    private func handleTransactionCreated(transactionId: String) async {
        // Notify observers to refresh transaction list
        NotificationCenter.default.post(
            name: .transactionCreated,
            object: nil,
            userInfo: ["transactionId": transactionId]
        )
    }

    private func handleBalanceUpdated(childId: String, balance: Decimal) async {
        // Notify observers to refresh balances
        NotificationCenter.default.post(
            name: .balanceUpdated,
            object: nil,
            userInfo: ["childId": childId, "balance": balance]
        )
    }
}
```

## UI Screens

### 1. Login/Register Flow

#### LoginView
```swift
struct LoginView: View {
    @StateObject private var viewModel = AuthViewModel()
    @State private var email = ""
    @State private var password = ""

    var body: some View {
        NavigationStack {
            VStack(spacing: 20) {
                Image(systemName: "dollarsign.circle.fill")
                    .font(.system(size: 80))
                    .foregroundStyle(.blue)

                Text("Allowance Tracker")
                    .font(.largeTitle)
                    .fontWeight(.bold)

                TextField("Email", text: $email)
                    .textFieldStyle(.roundedBorder)
                    .autocapitalization(.none)
                    .keyboardType(.emailAddress)

                SecureField("Password", text: $password)
                    .textFieldStyle(.roundedBorder)

                if let error = viewModel.errorMessage {
                    Text(error)
                        .foregroundStyle(.red)
                        .font(.caption)
                }

                Button("Login") {
                    Task {
                        await viewModel.login(email: email, password: password)
                    }
                }
                .buttonStyle(.borderedProminent)
                .disabled(viewModel.isLoading)

                NavigationLink("Don't have an account? Register") {
                    RegisterView()
                }
                .font(.caption)
            }
            .padding()
            .disabled(viewModel.isLoading)
            .overlay {
                if viewModel.isLoading {
                    ProgressView()
                }
            }
        }
    }
}
```

### 2. Dashboard (Parent View)

#### DashboardView
```swift
struct DashboardView: View {
    @StateObject private var viewModel = DashboardViewModel()
    @EnvironmentObject var authViewModel: AuthViewModel

    var body: some View {
        NavigationStack {
            ScrollView {
                if viewModel.isLoading {
                    ProgressView()
                } else if viewModel.children.isEmpty {
                    ContentUnavailableView(
                        "No Children Yet",
                        systemImage: "person.2.slash",
                        description: Text("Add a child to get started")
                    )
                } else {
                    LazyVGrid(columns: [
                        GridItem(.flexible()),
                        GridItem(.flexible())
                    ], spacing: 16) {
                        ForEach(viewModel.children) { child in
                            ChildCardView(child: child)
                        }
                    }
                    .padding()
                }
            }
            .navigationTitle("Family Dashboard")
            .toolbar {
                ToolbarItem(placement: .primaryAction) {
                    Button {
                        viewModel.showAddChild = true
                    } label: {
                        Image(systemName: "person.badge.plus")
                    }
                }

                ToolbarItem(placement: .secondaryAction) {
                    Button("Logout") {
                        authViewModel.logout()
                    }
                }
            }
            .sheet(isPresented: $viewModel.showAddChild) {
                AddChildSheet()
            }
            .refreshable {
                await viewModel.refresh()
            }
        }
        .task {
            await viewModel.loadChildren()
        }
    }
}
```

#### ChildCardView
```swift
struct ChildCardView: View {
    let child: Child
    @State private var showTransactions = false
    @State private var showQuickTransaction = false

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Image(systemName: "person.circle.fill")
                    .font(.title)
                    .foregroundStyle(.blue)

                VStack(alignment: .leading) {
                    Text(child.fullName)
                        .font(.headline)

                    Text("Weekly: \(child.weeklyAllowance.currencyFormatted)")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Spacer()
            }

            Divider()

            HStack {
                Text("Balance")
                    .font(.caption)
                    .foregroundStyle(.secondary)

                Spacer()

                Text(child.formattedBalance)
                    .font(.title2)
                    .fontWeight(.bold)
                    .foregroundStyle(.green)
            }

            HStack(spacing: 8) {
                Button {
                    showQuickTransaction = true
                } label: {
                    Label("Add Money", systemImage: "plus.circle")
                        .font(.caption)
                }
                .buttonStyle(.borderedProminent)
                .controlSize(.small)

                Button {
                    showTransactions = true
                } label: {
                    Label("History", systemImage: "list.bullet")
                        .font(.caption)
                }
                .buttonStyle(.bordered)
                .controlSize(.small)
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(12)
        .shadow(radius: 2)
        .sheet(isPresented: $showTransactions) {
            TransactionListView(childId: child.id)
        }
        .sheet(isPresented: $showQuickTransaction) {
            QuickTransactionSheet(child: child)
        }
    }
}
```

### 3. Transaction List

#### TransactionListView
```swift
struct TransactionListView: View {
    let childId: UUID
    @StateObject private var viewModel: TransactionViewModel
    @Environment(\.dismiss) private var dismiss

    init(childId: UUID) {
        self.childId = childId
        _viewModel = StateObject(wrappedValue: TransactionViewModel(childId: childId))
    }

    var body: some View {
        NavigationStack {
            List {
                ForEach(viewModel.transactions) { transaction in
                    TransactionRow(transaction: transaction)
                }
            }
            .navigationTitle("Transactions")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Done") {
                        dismiss()
                    }
                }
            }
            .refreshable {
                await viewModel.refresh()
            }
            .overlay {
                if viewModel.isLoading {
                    ProgressView()
                } else if viewModel.transactions.isEmpty {
                    ContentUnavailableView(
                        "No Transactions",
                        systemImage: "list.bullet.rectangle",
                        description: Text("Transactions will appear here")
                    )
                }
            }
        }
        .task {
            await viewModel.loadTransactions()
        }
    }
}

struct TransactionRow: View {
    let transaction: Transaction

    var body: some View {
        HStack {
            Image(systemName: transaction.isCredit ? "arrow.down.circle.fill" : "arrow.up.circle.fill")
                .foregroundStyle(transaction.isCredit ? .green : .red)

            VStack(alignment: .leading) {
                Text(transaction.description)
                    .font(.headline)

                Text(transaction.createdAt, style: .date)
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            VStack(alignment: .trailing) {
                Text(transaction.formattedAmount)
                    .font(.headline)
                    .foregroundStyle(transaction.isCredit ? .green : .red)

                Text("Balance: \(transaction.balanceAfter.currencyFormatted)")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
    }
}
```

### 4. Analytics Dashboard

#### AnalyticsView
```swift
struct AnalyticsView: View {
    @StateObject private var viewModel = AnalyticsViewModel()
    let childId: UUID

    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                // Income vs Spending Card
                if let summary = viewModel.incomeSpending {
                    IncomeSpendingCard(summary: summary)
                }

                // Balance History Chart
                if !viewModel.balanceHistory.isEmpty {
                    BalanceHistoryChart(points: viewModel.balanceHistory)
                }

                // Spending Breakdown
                if !viewModel.spendingBreakdown.isEmpty {
                    SpendingBreakdownChart(breakdown: viewModel.spendingBreakdown)
                }

                // Monthly Comparison
                if !viewModel.monthlyComparison.isEmpty {
                    MonthlyComparisonList(months: viewModel.monthlyComparison)
                }
            }
            .padding()
        }
        .navigationTitle("Analytics")
        .task {
            await viewModel.loadAnalytics(for: childId)
        }
        .refreshable {
            await viewModel.refresh(for: childId)
        }
    }
}
```

### 5. Wish List

#### WishListView
```swift
struct WishListView: View {
    @StateObject private var viewModel: WishListViewModel
    let childId: UUID

    init(childId: UUID) {
        self.childId = childId
        _viewModel = StateObject(wrappedValue: WishListViewModel(childId: childId))
    }

    var body: some View {
        List {
            ForEach(viewModel.items) { item in
                WishListItemRow(item: item) {
                    await viewModel.togglePurchase(item)
                }
            }
            .onDelete { indexSet in
                Task {
                    await viewModel.deleteItems(at: indexSet)
                }
            }
        }
        .navigationTitle("Wish List")
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                Button {
                    viewModel.showAddItem = true
                } label: {
                    Image(systemName: "plus")
                }
            }
        }
        .sheet(isPresented: $viewModel.showAddItem) {
            AddWishListItemSheet(childId: childId)
        }
        .task {
            await viewModel.loadItems()
        }
        .refreshable {
            await viewModel.refresh()
        }
    }
}

struct WishListItemRow: View {
    let item: WishListItem
    let onTogglePurchase: () async -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Text(item.name)
                    .font(.headline)
                    .strikethrough(item.isPurchased)

                Spacer()

                Text(item.price.currencyFormatted)
                    .font(.headline)
                    .foregroundStyle(.blue)
            }

            if item.canAfford {
                Label("Can afford!", systemImage: "checkmark.circle.fill")
                    .font(.caption)
                    .foregroundStyle(.green)
            } else {
                ProgressView(value: item.progressPercentage) {
                    Text("\(Int(item.progressPercentage * 100))% saved")
                        .font(.caption)
                }
            }

            if let notes = item.notes {
                Text(notes)
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .swipeActions(edge: .trailing, allowsFullSwipe: false) {
            Button(item.isPurchased ? "Mark Unpurchased" : "Mark Purchased") {
                Task {
                    await onTogglePurchase()
                }
            }
            .tint(item.isPurchased ? .orange : .green)
        }
    }
}
```

## Offline Support & Caching

### Cache Strategy
```swift
actor CacheService {
    private var childrenCache: [Child] = []
    private var transactionsCache: [UUID: [Transaction]] = [:]
    private var lastSync: Date?

    func getCachedChildren() -> [Child] {
        childrenCache
    }

    func cacheChildren(_ children: [Child]) {
        childrenCache = children
        lastSync = Date()
    }

    func getCachedTransactions(for childId: UUID) -> [Transaction] {
        transactionsCache[childId] ?? []
    }

    func cacheTransactions(_ transactions: [Transaction], for childId: UUID) {
        transactionsCache[childId] = transactions
    }

    func clearCache() {
        childrenCache = []
        transactionsCache = [:]
        lastSync = nil
    }

    func needsRefresh(maxAge: TimeInterval = 300) -> Bool {
        guard let lastSync = lastSync else { return true }
        return Date().timeIntervalSince(lastSync) > maxAge
    }
}
```

## Testing Strategy

### Unit Tests
```swift
final class AuthViewModelTests: XCTestCase {
    var sut: AuthViewModel!
    var mockAPIService: MockAPIService!
    var mockKeychainService: MockKeychainService!

    override func setUp() {
        super.setUp()
        mockAPIService = MockAPIService()
        mockKeychainService = MockKeychainService()
        sut = AuthViewModel(
            apiService: mockAPIService,
            keychainService: mockKeychainService
        )
    }

    func testLoginSuccess() async {
        // Given
        let email = "test@example.com"
        let password = "password"
        let expectedUser = User(
            id: UUID(),
            email: email,
            firstName: "Test",
            lastName: "User",
            role: .parent,
            familyId: UUID()
        )

        mockAPIService.mockResponse = AuthResponse(
            token: "mock-token",
            expiresAt: Date().addingTimeInterval(86400),
            user: expectedUser
        )

        // When
        await sut.login(email: email, password: password)

        // Then
        XCTAssertTrue(sut.isAuthenticated)
        XCTAssertEqual(sut.currentUser?.email, email)
        XCTAssertNil(sut.errorMessage)
    }

    func testLoginFailure() async {
        // Given
        mockAPIService.shouldFail = true

        // When
        await sut.login(email: "test@example.com", password: "wrong")

        // Then
        XCTAssertFalse(sut.isAuthenticated)
        XCTAssertNil(sut.currentUser)
        XCTAssertNotNil(sut.errorMessage)
    }
}
```

### UI Tests with ViewInspector
```swift
import ViewInspector

final class LoginViewTests: XCTestCase {
    func testLoginButtonDisabledWhenLoading() throws {
        // Given
        let viewModel = AuthViewModel()
        let view = LoginView(viewModel: viewModel)
        viewModel.isLoading = true

        // When
        let button = try view.inspect().find(button: "Login")

        // Then
        XCTAssertTrue(try button.isDisabled())
    }
}
```

### Snapshot Tests
```swift
import SnapshotTesting

final class ChildCardSnapshotTests: XCTestCase {
    func testChildCardAppearance() {
        let child = Child(
            id: UUID(),
            firstName: "Alice",
            lastName: "Johnson",
            weeklyAllowance: 10.00,
            currentBalance: 25.50,
            lastAllowanceDate: nil
        )

        let view = ChildCardView(child: child)
            .frame(width: 180, height: 200)

        assertSnapshot(matching: view, as: .image)
    }
}
```

## Performance Considerations

### Image Caching
```swift
actor ImageCache {
    private var cache: [URL: UIImage] = [:]

    func image(for url: URL) -> UIImage? {
        cache[url]
    }

    func setImage(_ image: UIImage, for url: URL) {
        cache[url] = image
    }
}
```

### Lazy Loading
- Use `LazyVStack` and `LazyVGrid` for long lists
- Implement pagination for transaction history
- Load analytics charts on demand

### Background Refresh
```swift
import BackgroundTasks

final class BackgroundRefreshManager {
    static let shared = BackgroundRefreshManager()

    func scheduleAppRefresh() {
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

## Accessibility

### VoiceOver Support
```swift
extension ChildCardView {
    var accessibilityLabel: String {
        "\(child.fullName), balance \(child.formattedBalance), weekly allowance \(child.weeklyAllowance.currencyFormatted)"
    }
}
```

### Dynamic Type
- Use `.font(.headline)` instead of fixed font sizes
- Support all dynamic type sizes
- Test with largest accessibility sizes

### Color Contrast
- Ensure WCAG AA compliance for all text
- Provide alternative indicators beyond color

## App Store Requirements

### Privacy Manifest
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN">
<plist version="1.0">
<dict>
    <key>NSPrivacyTracking</key>
    <false/>
    <key>NSPrivacyCollectedDataTypes</key>
    <array>
        <dict>
            <key>NSPrivacyCollectedDataType</key>
            <string>NSPrivacyCollectedDataTypeFinancialInfo</string>
            <key>NSPrivacyCollectedDataTypeLinked</key>
            <true/>
            <key>NSPrivacyCollectedDataTypePurposes</key>
            <array>
                <string>NSPrivacyCollectedDataTypePurposeAppFunctionality</string>
            </array>
        </dict>
    </array>
</dict>
</plist>
```

### Required Descriptions
- Privacy - Face ID Usage: "Secure login with biometric authentication"
- Privacy - Notification Usage: "Notify you of transaction updates"

## Implementation Phases

### Phase 1: Foundation (Week 1)
- [ ] Project setup with SwiftUI
- [ ] Models and DTOs
- [ ] API Service layer
- [ ] Authentication flow
- [ ] Keychain integration

### Phase 2: Core Features (Week 2)
- [ ] Dashboard view
- [ ] Child card component
- [ ] Transaction list
- [ ] Quick transaction sheet
- [ ] SignalR real-time updates

### Phase 3: Advanced Features (Week 3)
- [ ] Analytics dashboard with charts
- [ ] Wish list management
- [ ] Offline support & caching
- [ ] Background refresh

### Phase 4: Polish & Testing (Week 4)
- [ ] Unit tests (>80% coverage)
- [ ] UI tests
- [ ] Snapshot tests
- [ ] Accessibility audit
- [ ] Performance optimization
- [ ] App Store submission

## Dependencies

```swift
// Package.swift
dependencies: [
    .package(url: "https://github.com/signalr/SignalR-Swift", from: "0.3.0"),
    .package(url: "https://github.com/pointfreeco/swift-snapshot-testing", from: "1.15.0"),
    .package(url: "https://github.com/nalexn/ViewInspector", from: "0.9.0")
]
```

## Success Metrics

### Performance
- App launch: <2 seconds
- API response handling: <100ms
- List scrolling: 60 FPS
- Memory usage: <100MB

### Quality
- Test coverage: >80%
- Zero crashes in production
- App Store rating: >4.5 stars
- Accessibility score: 100%

## Next Steps

1. Set up Xcode project
2. Implement authentication flow (TDD)
3. Build dashboard UI
4. Integrate SignalR
5. Add analytics features
6. Polish and test
7. Submit to App Store
