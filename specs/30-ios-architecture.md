# iOS Architecture & Project Structure

## Overview
Detailed architecture specification for the Allowance Tracker iOS application using MVVM pattern with SwiftUI, Combine, and modern Swift concurrency. This document defines the complete project structure, dependency injection strategy, data flow, and architectural patterns.

## Architecture Pattern: MVVM with Combine

### Why MVVM for SwiftUI?
- **Natural fit**: SwiftUI's reactive nature aligns perfectly with MVVM
- **Testability**: Business logic in ViewModels is easily unit tested
- **Separation of concerns**: Views handle UI, ViewModels handle logic
- **Reusability**: ViewModels can be shared across views
- **Scalability**: Clear boundaries as app grows

### MVVM Components

#### Model
Domain objects and data structures (structs/classes)
- Pure Swift types (Codable, Equatable, Identifiable)
- No UI logic or dependencies
- Represent business domain entities

#### View
SwiftUI views (declarative UI)
- Only responsible for rendering UI
- Binds to ViewModel properties
- Forwards user actions to ViewModel
- No business logic

#### ViewModel
Business logic and state management (@ObservableObject)
- Manages view state (@Published properties)
- Handles user interactions
- Coordinates with services/repositories
- Contains zero SwiftUI-specific code

### Data Flow Diagram
```
┌─────────────────────────────────────────────────────────────┐
│                        SwiftUI View                          │
│  ┌────────────────────────────────────────────────────┐    │
│  │  @StateObject var viewModel: LoginViewModel        │    │
│  │                                                      │    │
│  │  TextField("Email", text: $viewModel.email)         │    │
│  │  Button("Login") { await viewModel.login() }        │    │
│  └──────────────────────────┬───────────────────────────┘    │
└─────────────────────────────┼───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                         ViewModel                            │
│  ┌────────────────────────────────────────────────────┐    │
│  │  @MainActor class LoginViewModel: ObservableObject │    │
│  │  @Published var email: String                      │    │
│  │  @Published var isLoading: Bool                    │    │
│  │                                                      │    │
│  │  func login() async {                               │    │
│  │      await authRepository.login(email, password)    │    │
│  │  }                                                   │    │
│  └──────────────────────────┬───────────────────────────┘    │
└─────────────────────────────┼───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        Repository                            │
│  ┌────────────────────────────────────────────────────┐    │
│  │  class AuthRepository {                             │    │
│  │      private let apiClient: APIClient               │    │
│  │      private let keychainService: KeychainService   │    │
│  │                                                      │    │
│  │      func login() -> AuthResponse {                 │    │
│  │          let response = await apiClient.post(...)   │    │
│  │          keychainService.saveToken(response.token)  │    │
│  │          return response                             │    │
│  │      }                                               │    │
│  │  }                                                   │    │
│  └──────────────────────────┬───────────────────────────┘    │
└─────────────────────────────┼───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     Service Layer                            │
│  ┌─────────────────────┐      ┌─────────────────────┐      │
│  │    APIClient        │      │  KeychainService     │      │
│  │  (URLSession)       │      │  (Security)          │      │
│  └─────────────────────┘      └─────────────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

---

## Complete Project Structure

```
AllowanceTracker/
├── AllowanceTracker.xcodeproj
├── AllowanceTracker/
│   ├── App/
│   │   ├── AllowanceTrackerApp.swift
│   │   ├── AppDelegate.swift
│   │   └── Configuration/
│   │       ├── Environment.swift
│   │       ├── AppConfiguration.swift
│   │       └── DependencyContainer.swift
│   │
│   ├── Models/
│   │   ├── Domain/
│   │   │   ├── User.swift
│   │   │   ├── Child.swift
│   │   │   ├── Transaction.swift
│   │   │   ├── Family.swift
│   │   │   ├── TransactionType.swift
│   │   │   ├── UserRole.swift
│   │   │   ├── DashboardSummary.swift
│   │   │   └── AuthCredentials.swift
│   │   │
│   │   ├── API/
│   │   │   ├── Requests/
│   │   │   │   ├── LoginRequest.swift
│   │   │   │   ├── RegisterParentRequest.swift
│   │   │   │   ├── RegisterChildRequest.swift
│   │   │   │   ├── CreateTransactionRequest.swift
│   │   │   │   └── UpdateAllowanceRequest.swift
│   │   │   │
│   │   │   └── Responses/
│   │   │       ├── AuthResponse.swift
│   │   │       ├── ChildResponse.swift
│   │   │       ├── TransactionResponse.swift
│   │   │       ├── FamilyResponse.swift
│   │   │       ├── ParentDashboardResponse.swift
│   │   │       ├── ChildDashboardResponse.swift
│   │   │       └── ErrorResponse.swift
│   │   │
│   │   └── CoreData/
│   │       ├── AllowanceTracker.xcdatamodeld/
│   │       ├── CoreDataModels.swift
│   │       ├── UserEntity+CoreData.swift
│   │       ├── ChildEntity+CoreData.swift
│   │       └── TransactionEntity+CoreData.swift
│   │
│   ├── ViewModels/
│   │   ├── Auth/
│   │   │   ├── LoginViewModel.swift
│   │   │   ├── RegisterParentViewModel.swift
│   │   │   └── RegisterChildViewModel.swift
│   │   │
│   │   ├── Dashboard/
│   │   │   ├── ParentDashboardViewModel.swift
│   │   │   └── ChildDashboardViewModel.swift
│   │   │
│   │   ├── Children/
│   │   │   ├── ChildListViewModel.swift
│   │   │   ├── ChildDetailViewModel.swift
│   │   │   └── AddChildViewModel.swift
│   │   │
│   │   ├── Transactions/
│   │   │   ├── TransactionListViewModel.swift
│   │   │   ├── CreateTransactionViewModel.swift
│   │   │   └── TransactionDetailViewModel.swift
│   │   │
│   │   └── Settings/
│   │       ├── SettingsViewModel.swift
│   │       └── ProfileViewModel.swift
│   │
│   ├── Views/
│   │   ├── Auth/
│   │   │   ├── LoginView.swift
│   │   │   ├── RegisterParentView.swift
│   │   │   └── RegisterChildView.swift
│   │   │
│   │   ├── Dashboard/
│   │   │   ├── ParentDashboardView.swift
│   │   │   ├── ChildDashboardView.swift
│   │   │   └── Components/
│   │   │       ├── BalanceCardView.swift
│   │   │       ├── ChildCardView.swift
│   │   │       ├── TransactionRowView.swift
│   │   │       ├── QuickStatsView.swift
│   │   │       └── AllowanceCountdownView.swift
│   │   │
│   │   ├── Children/
│   │   │   ├── ChildListView.swift
│   │   │   ├── ChildDetailView.swift
│   │   │   └── AddChildView.swift
│   │   │
│   │   ├── Transactions/
│   │   │   ├── TransactionListView.swift
│   │   │   ├── CreateTransactionView.swift
│   │   │   └── TransactionDetailView.swift
│   │   │
│   │   ├── Settings/
│   │   │   ├── SettingsView.swift
│   │   │   └── ProfileView.swift
│   │   │
│   │   ├── Common/
│   │   │   ├── LoadingView.swift
│   │   │   ├── ErrorView.swift
│   │   │   ├── EmptyStateView.swift
│   │   │   └── PrimaryButton.swift
│   │   │
│   │   └── Root/
│   │       ├── RootView.swift
│   │       ├── MainTabView.swift
│   │       └── ContentView.swift
│   │
│   ├── Services/
│   │   ├── Network/
│   │   │   ├── APIClient.swift
│   │   │   ├── APIEndpoint.swift
│   │   │   ├── APIError.swift
│   │   │   ├── RequestBuilder.swift
│   │   │   ├── ResponseDecoder.swift
│   │   │   └── NetworkMonitor.swift
│   │   │
│   │   ├── API/
│   │   │   ├── AuthAPI.swift
│   │   │   ├── FamiliesAPI.swift
│   │   │   ├── ChildrenAPI.swift
│   │   │   ├── TransactionsAPI.swift
│   │   │   └── DashboardAPI.swift
│   │   │
│   │   ├── Storage/
│   │   │   ├── KeychainService.swift
│   │   │   ├── CoreDataService.swift
│   │   │   └── UserDefaultsService.swift
│   │   │
│   │   └── Auth/
│   │       └── AuthenticationService.swift
│   │
│   ├── Repositories/
│   │   ├── AuthRepository.swift
│   │   ├── FamilyRepository.swift
│   │   ├── ChildRepository.swift
│   │   └── TransactionRepository.swift
│   │
│   ├── Utilities/
│   │   ├── Extensions/
│   │   │   ├── String+Validation.swift
│   │   │   ├── Decimal+Currency.swift
│   │   │   ├── Date+Extensions.swift
│   │   │   ├── View+Extensions.swift
│   │   │   └── Color+Theme.swift
│   │   │
│   │   ├── Helpers/
│   │   │   ├── CurrencyFormatter.swift
│   │   │   ├── DateFormatter.swift
│   │   │   ├── Validator.swift
│   │   │   └── HapticFeedback.swift
│   │   │
│   │   └── Constants/
│   │       ├── AppConstants.swift
│   │       ├── APIConstants.swift
│   │       └── ColorConstants.swift
│   │
│   └── Resources/
│       ├── Assets.xcassets/
│       │   ├── AppIcon.appiconset/
│       │   ├── Colors/
│       │   └── Images/
│       ├── Localizable.strings
│       ├── Info.plist
│       └── AllowanceTracker.entitlements
│
├── AllowanceTrackerTests/
│   ├── ViewModels/
│   │   ├── LoginViewModelTests.swift
│   │   ├── ParentDashboardViewModelTests.swift
│   │   ├── ChildDashboardViewModelTests.swift
│   │   ├── TransactionListViewModelTests.swift
│   │   └── CreateTransactionViewModelTests.swift
│   │
│   ├── Services/
│   │   ├── APIClientTests.swift
│   │   ├── AuthAPITests.swift
│   │   ├── KeychainServiceTests.swift
│   │   └── NetworkMonitorTests.swift
│   │
│   ├── Repositories/
│   │   ├── AuthRepositoryTests.swift
│   │   ├── TransactionRepositoryTests.swift
│   │   └── ChildRepositoryTests.swift
│   │
│   ├── Utilities/
│   │   ├── StringValidationTests.swift
│   │   ├── CurrencyFormatterTests.swift
│   │   └── ValidatorTests.swift
│   │
│   └── Mocks/
│       ├── MockAPIClient.swift
│       ├── MockAuthRepository.swift
│       ├── MockTransactionRepository.swift
│       ├── MockKeychainService.swift
│       └── MockNetworkMonitor.swift
│
└── AllowanceTrackerUITests/
    ├── LoginFlowUITests.swift
    ├── TransactionFlowUITests.swift
    └── DashboardUITests.swift
```

---

## Core Data Schema (Offline Caching)

### Core Data Model Definition
```swift
// AllowanceTracker.xcdatamodeld

// UserEntity
entity UserEntity {
    attribute id: UUID (required, indexed)
    attribute email: String (required)
    attribute firstName: String (required)
    attribute lastName: String (required)
    attribute role: String (required)
    attribute familyId: UUID (optional)
    attribute lastSyncedAt: Date (optional)

    relationship childProfile: ChildEntity? (optional, cascade delete)
}

// ChildEntity
entity ChildEntity {
    attribute id: UUID (required, indexed)
    attribute userId: UUID (required, indexed)
    attribute firstName: String (required)
    attribute lastName: String (required)
    attribute currentBalance: Decimal (required)
    attribute weeklyAllowance: Decimal (required)
    attribute lastAllowanceDate: Date (optional)
    attribute nextAllowanceDate: Date (optional)
    attribute lastSyncedAt: Date (optional)

    relationship user: UserEntity (required)
    relationship transactions: Set<TransactionEntity> (optional, cascade delete)
}

// TransactionEntity
entity TransactionEntity {
    attribute id: UUID (required, indexed)
    attribute childId: UUID (required, indexed)
    attribute amount: Decimal (required)
    attribute type: String (required)
    attribute transactionDescription: String (required)
    attribute balanceAfter: Decimal (required)
    attribute createdBy: UUID (required)
    attribute createdByName: String (required)
    attribute createdAt: Date (required, indexed)
    attribute lastSyncedAt: Date (optional)

    relationship child: ChildEntity (required)
}
```

### Core Data Swift Code
```swift
// Models/CoreData/CoreDataModels.swift
import CoreData

extension UserEntity {
    static func fetchRequest() -> NSFetchRequest<UserEntity> {
        return NSFetchRequest<UserEntity>(entityName: "UserEntity")
    }

    static func findOrCreate(id: UUID, in context: NSManagedObjectContext) -> UserEntity {
        let request = fetchRequest()
        request.predicate = NSPredicate(format: "id == %@", id as CVarArg)
        request.fetchLimit = 1

        if let existing = try? context.fetch(request).first {
            return existing
        }

        let new = UserEntity(context: context)
        new.id = id
        return new
    }

    func toUser() -> User {
        User(
            id: id!,
            email: email!,
            firstName: firstName!,
            lastName: lastName!,
            role: UserRole(rawValue: role!) ?? .parent,
            familyId: familyId
        )
    }

    func update(from user: User) {
        self.id = user.id
        self.email = user.email
        self.firstName = user.firstName
        self.lastName = user.lastName
        self.role = user.role.rawValue
        self.familyId = user.familyId
        self.lastSyncedAt = Date()
    }
}

extension ChildEntity {
    static func fetchRequest() -> NSFetchRequest<ChildEntity> {
        return NSFetchRequest<ChildEntity>(entityName: "ChildEntity")
    }

    func toChild() -> Child {
        Child(
            childId: id!,
            userId: userId!,
            firstName: firstName!,
            lastName: lastName!,
            email: user?.email ?? "",
            currentBalance: currentBalance! as Decimal,
            weeklyAllowance: weeklyAllowance! as Decimal,
            lastAllowanceDate: lastAllowanceDate,
            nextAllowanceDate: nextAllowanceDate
        )
    }
}

extension TransactionEntity {
    static func fetchRequest() -> NSFetchRequest<TransactionEntity> {
        return NSFetchRequest<TransactionEntity>(entityName: "TransactionEntity")
    }

    func toTransaction() -> Transaction {
        Transaction(
            id: id!,
            childId: childId!,
            amount: amount! as Decimal,
            type: TransactionType(rawValue: type!) ?? .credit,
            description: transactionDescription!,
            balanceAfter: balanceAfter! as Decimal,
            createdBy: createdBy!,
            createdByName: createdByName!,
            createdAt: createdAt!
        )
    }
}
```

---

## Dependency Injection Container

### DependencyContainer.swift
```swift
import Foundation
import Combine

@MainActor
final class DependencyContainer: ObservableObject {
    // Singleton instance
    static let shared = DependencyContainer()

    // Published auth state for reactive updates
    @Published private(set) var isAuthenticated = false

    // Services (singletons)
    let apiClient: APIClient
    let keychainService: KeychainService
    let coreDataService: CoreDataService
    let userDefaultsService: UserDefaultsService
    let networkMonitor: NetworkMonitor
    let authenticationService: AuthenticationService

    // Repositories
    private(set) lazy var authRepository: AuthRepository = {
        AuthRepository(
            apiClient: apiClient,
            keychainService: keychainService,
            coreDataService: coreDataService
        )
    }()

    private(set) lazy var familyRepository: FamilyRepository = {
        FamilyRepository(
            apiClient: apiClient,
            coreDataService: coreDataService
        )
    }()

    private(set) lazy var childRepository: ChildRepository = {
        ChildRepository(
            apiClient: apiClient,
            coreDataService: coreDataService
        )
    }()

    private(set) lazy var transactionRepository: TransactionRepository = {
        TransactionRepository(
            apiClient: apiClient,
            coreDataService: coreDataService
        )
    }()

    private var cancellables = Set<AnyCancellable>()

    private init() {
        // Initialize services
        self.keychainService = KeychainService()
        self.coreDataService = CoreDataService.shared
        self.userDefaultsService = UserDefaultsService()
        self.networkMonitor = NetworkMonitor()

        // Initialize API client with configuration
        let configuration = AppConfiguration.current
        self.apiClient = APIClient(
            baseURL: configuration.apiBaseURL,
            keychainService: keychainService
        )

        // Initialize authentication service
        self.authenticationService = AuthenticationService(
            authRepository: authRepository,
            keychainService: keychainService
        )

        // Observe authentication state changes
        authenticationService.$isAuthenticated
            .sink { [weak self] isAuth in
                self?.isAuthenticated = isAuth
            }
            .store(in: &cancellables)
    }

    // Factory method for creating ViewModels with dependencies
    func makeLoginViewModel() -> LoginViewModel {
        LoginViewModel(authRepository: authRepository)
    }

    func makeParentDashboardViewModel() -> ParentDashboardViewModel {
        ParentDashboardViewModel(
            childRepository: childRepository,
            transactionRepository: transactionRepository
        )
    }

    func makeChildDashboardViewModel() -> ChildDashboardViewModel {
        ChildDashboardViewModel(
            authenticationService: authenticationService,
            transactionRepository: transactionRepository
        )
    }

    func makeTransactionListViewModel(childId: UUID) -> TransactionListViewModel {
        TransactionListViewModel(
            childId: childId,
            transactionRepository: transactionRepository
        )
    }

    func makeCreateTransactionViewModel() -> CreateTransactionViewModel {
        CreateTransactionViewModel(
            childRepository: childRepository,
            transactionRepository: transactionRepository
        )
    }
}
```

### Using Dependency Container in App
```swift
// App/AllowanceTrackerApp.swift
import SwiftUI

@main
struct AllowanceTrackerApp: App {
    @StateObject private var container = DependencyContainer.shared

    var body: some Scene {
        WindowGroup {
            RootView()
                .environmentObject(container)
                .environmentObject(container.authenticationService)
                .environmentObject(container.networkMonitor)
        }
    }
}
```

---

## Navigation Architecture

### NavigationStack with Deep Linking
```swift
// Views/Root/RootView.swift
import SwiftUI

struct RootView: View {
    @EnvironmentObject private var authService: AuthenticationService
    @State private var navigationPath = NavigationPath()

    var body: some View {
        Group {
            if authService.isAuthenticated {
                MainTabView()
                    .transition(.opacity)
            } else {
                LoginView()
                    .transition(.opacity)
            }
        }
        .animation(.easeInOut, value: authService.isAuthenticated)
    }
}

// Views/Root/MainTabView.swift
struct MainTabView: View {
    @EnvironmentObject private var authService: AuthenticationService

    var body: some View {
        TabView {
            NavigationStack {
                if authService.currentUser?.isParent == true {
                    ParentDashboardView()
                } else {
                    ChildDashboardView()
                }
            }
            .tabItem {
                Label("Dashboard", systemImage: "house.fill")
            }

            NavigationStack {
                TransactionListView()
            }
            .tabItem {
                Label("Transactions", systemImage: "list.bullet")
            }

            NavigationStack {
                SettingsView()
            }
            .tabItem {
                Label("Settings", systemImage: "gear")
            }
        }
    }
}
```

---

## State Management with Combine

### Example: LoginViewModel
```swift
// ViewModels/Auth/LoginViewModel.swift
import Foundation
import Combine

@MainActor
final class LoginViewModel: ObservableObject {
    // MARK: - Published Properties
    @Published var email: String = ""
    @Published var password: String = ""
    @Published var rememberMe: Bool = true
    @Published var isLoading: Bool = false
    @Published var errorMessage: String?
    @Published var isAuthenticated: Bool = false

    // MARK: - Dependencies
    private let authRepository: AuthRepository

    // MARK: - Private Properties
    private var cancellables = Set<AnyCancellable>()

    // MARK: - Computed Properties
    var canSubmit: Bool {
        !email.isEmpty && !password.isEmpty && !isLoading
    }

    // MARK: - Initialization
    init(authRepository: AuthRepository) {
        self.authRepository = authRepository
        setupBindings()
    }

    // MARK: - Setup
    private func setupBindings() {
        // Clear error when user types
        Publishers.CombineLatest($email, $password)
            .sink { [weak self] _, _ in
                self?.errorMessage = nil
            }
            .store(in: &cancellables)
    }

    // MARK: - Actions
    func login() async {
        guard validateInput() else { return }

        isLoading = true
        errorMessage = nil

        do {
            let request = LoginRequest(
                email: email,
                password: password,
                rememberMe: rememberMe
            )

            let response = try await authRepository.login(request: request)
            isAuthenticated = true

            // Haptic feedback on success
            HapticFeedback.success()
        } catch {
            errorMessage = mapError(error)
            HapticFeedback.error()
        }

        isLoading = false
    }

    func loginWithBiometrics() async {
        // Implement biometric authentication
        isLoading = true
        do {
            try await authRepository.loginWithBiometrics()
            isAuthenticated = true
            HapticFeedback.success()
        } catch {
            errorMessage = mapError(error)
            HapticFeedback.error()
        }
        isLoading = false
    }

    // MARK: - Validation
    private func validateInput() -> Bool {
        if email.isEmpty {
            errorMessage = "Email is required"
            return false
        }

        if !Validator.isValidEmail(email) {
            errorMessage = "Invalid email format"
            return false
        }

        if password.isEmpty {
            errorMessage = "Password is required"
            return false
        }

        if password.count < 6 {
            errorMessage = "Password must be at least 6 characters"
            return false
        }

        return true
    }

    // MARK: - Error Mapping
    private func mapError(_ error: Error) -> String {
        if let apiError = error as? APIError {
            switch apiError {
            case .unauthorized:
                return "Invalid email or password"
            case .networkError:
                return "Network connection lost. Please try again."
            case .serverError(let message):
                return message ?? "Server error occurred"
            default:
                return "An unexpected error occurred"
            }
        }
        return error.localizedDescription
    }
}
```

### Example: ParentDashboardViewModel
```swift
// ViewModels/Dashboard/ParentDashboardViewModel.swift
import Foundation
import Combine

@MainActor
final class ParentDashboardViewModel: ObservableObject {
    // MARK: - Published Properties
    @Published var children: [Child] = []
    @Published var recentTransactions: [Transaction] = []
    @Published var familySummary: FamilySummary?
    @Published var isLoading: Bool = false
    @Published var isRefreshing: Bool = false
    @Published var errorMessage: String?

    // MARK: - Dependencies
    private let childRepository: ChildRepository
    private let transactionRepository: TransactionRepository

    private var cancellables = Set<AnyCancellable>()

    // MARK: - Computed Properties
    var totalFamilyBalance: Decimal {
        children.reduce(0) { $0 + $1.currentBalance }
    }

    var totalWeeklyAllowance: Decimal {
        children.reduce(0) { $0 + $1.weeklyAllowance }
    }

    // MARK: - Initialization
    init(
        childRepository: ChildRepository,
        transactionRepository: TransactionRepository
    ) {
        self.childRepository = childRepository
        self.transactionRepository = transactionRepository
    }

    // MARK: - Actions
    func loadDashboard() async {
        isLoading = true
        errorMessage = nil

        await withTaskGroup(of: Void.self) { group in
            group.addTask { await self.loadChildren() }
            group.addTask { await self.loadRecentTransactions() }
        }

        isLoading = false
    }

    func refresh() async {
        isRefreshing = true
        await loadDashboard()
        isRefreshing = false
    }

    private func loadChildren() async {
        do {
            children = try await childRepository.fetchChildren()
        } catch {
            errorMessage = "Failed to load children: \(error.localizedDescription)"
        }
    }

    private func loadRecentTransactions() async {
        do {
            recentTransactions = try await transactionRepository.fetchRecentTransactions(limit: 10)
        } catch {
            errorMessage = "Failed to load transactions: \(error.localizedDescription)"
        }
    }

    func payAllowance(for child: Child) async {
        do {
            try await childRepository.payWeeklyAllowance(childId: child.childId)
            await loadDashboard() // Reload to show updated balance
            HapticFeedback.success()
        } catch {
            errorMessage = "Failed to pay allowance: \(error.localizedDescription)"
            HapticFeedback.error()
        }
    }
}

struct FamilySummary {
    let totalChildren: Int
    let totalBalance: Decimal
    let totalWeeklyAllowance: Decimal
}
```

---

## Network Layer Architecture

### APIClient.swift (Core Networking)
```swift
// Services/Network/APIClient.swift
import Foundation

protocol APIClientProtocol {
    func request<T: Decodable>(_ endpoint: APIEndpoint) async throws -> T
    func requestWithoutResponse(_ endpoint: APIEndpoint) async throws
}

final class APIClient: APIClientProtocol {
    private let baseURL: URL
    private let session: URLSession
    private let keychainService: KeychainService
    private let decoder: JSONDecoder
    private let encoder: JSONEncoder

    init(
        baseURL: URL,
        keychainService: KeychainService,
        session: URLSession = .shared
    ) {
        self.baseURL = baseURL
        self.keychainService = keychainService
        self.session = session

        // Configure decoder for API date format
        self.decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601

        // Configure encoder
        self.encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
    }

    func request<T: Decodable>(_ endpoint: APIEndpoint) async throws -> T {
        let urlRequest = try buildRequest(endpoint)

        let (data, response) = try await session.data(for: urlRequest)

        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.invalidResponse
        }

        try handleStatusCode(httpResponse.statusCode, data: data)

        do {
            return try decoder.decode(T.self, from: data)
        } catch {
            throw APIError.decodingError(error)
        }
    }

    func requestWithoutResponse(_ endpoint: APIEndpoint) async throws {
        let urlRequest = try buildRequest(endpoint)

        let (data, response) = try await session.data(for: urlRequest)

        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.invalidResponse
        }

        try handleStatusCode(httpResponse.statusCode, data: data)
    }

    // MARK: - Private Methods
    private func buildRequest(_ endpoint: APIEndpoint) throws -> URLRequest {
        let url = baseURL.appendingPathComponent(endpoint.path)
        var request = URLRequest(url: url)
        request.httpMethod = endpoint.method.rawValue
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        // Add authorization header if token exists
        if let token = keychainService.getToken() {
            request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        }

        // Add body if present
        if let body = endpoint.body {
            request.httpBody = try encoder.encode(body)
        }

        // Add query parameters
        if !endpoint.queryParameters.isEmpty {
            var components = URLComponents(url: url, resolvingAgainstBaseURL: false)
            components?.queryItems = endpoint.queryParameters.map {
                URLQueryItem(name: $0.key, value: "\($0.value)")
            }
            request.url = components?.url
        }

        return request
    }

    private func handleStatusCode(_ statusCode: Int, data: Data) throws {
        switch statusCode {
        case 200...299:
            return
        case 401:
            // Try to refresh token
            try await refreshTokenIfNeeded()
            throw APIError.unauthorized
        case 400:
            let errorResponse = try? decoder.decode(ErrorResponse.self, from: data)
            throw APIError.badRequest(errorResponse?.error.message)
        case 403:
            throw APIError.forbidden
        case 404:
            throw APIError.notFound
        case 409:
            let errorResponse = try? decoder.decode(ErrorResponse.self, from: data)
            throw APIError.conflict(errorResponse?.error.message)
        case 500...599:
            let errorResponse = try? decoder.decode(ErrorResponse.self, from: data)
            throw APIError.serverError(errorResponse?.error.message)
        default:
            throw APIError.unknown(statusCode)
        }
    }

    private func refreshTokenIfNeeded() async throws {
        // Implement token refresh logic
        // For now, just clear token and require re-login
        keychainService.deleteToken()
    }
}

// MARK: - APIEndpoint
struct APIEndpoint {
    let path: String
    let method: HTTPMethod
    let body: Encodable?
    let queryParameters: [String: Any]

    init(
        path: String,
        method: HTTPMethod,
        body: Encodable? = nil,
        queryParameters: [String: Any] = [:]
    ) {
        self.path = path
        self.method = method
        self.body = body
        self.queryParameters = queryParameters
    }
}

enum HTTPMethod: String {
    case GET, POST, PUT, PATCH, DELETE
}

// MARK: - APIError
enum APIError: Error, LocalizedError {
    case invalidURL
    case invalidResponse
    case networkError(Error)
    case unauthorized
    case forbidden
    case notFound
    case badRequest(String?)
    case conflict(String?)
    case serverError(String?)
    case decodingError(Error)
    case unknown(Int)

    var errorDescription: String? {
        switch self {
        case .invalidURL:
            return "Invalid URL"
        case .invalidResponse:
            return "Invalid server response"
        case .networkError(let error):
            return "Network error: \(error.localizedDescription)"
        case .unauthorized:
            return "Authentication required"
        case .forbidden:
            return "Access forbidden"
        case .notFound:
            return "Resource not found"
        case .badRequest(let message):
            return message ?? "Bad request"
        case .conflict(let message):
            return message ?? "Resource conflict"
        case .serverError(let message):
            return message ?? "Server error"
        case .decodingError(let error):
            return "Failed to decode response: \(error.localizedDescription)"
        case .unknown(let code):
            return "Unknown error (status code: \(code))"
        }
    }
}
```

### AuthAPI.swift (Endpoint Definitions)
```swift
// Services/API/AuthAPI.swift
import Foundation

struct AuthAPI {
    private let apiClient: APIClient

    init(apiClient: APIClient) {
        self.apiClient = apiClient
    }

    func login(request: LoginRequest) async throws -> AuthResponse {
        let endpoint = APIEndpoint(
            path: "api/v1/auth/login",
            method: .POST,
            body: request
        )
        return try await apiClient.request(endpoint)
    }

    func registerParent(request: RegisterParentRequest) async throws -> AuthResponse {
        let endpoint = APIEndpoint(
            path: "api/v1/auth/register/parent",
            method: .POST,
            body: request
        )
        return try await apiClient.request(endpoint)
    }

    func registerChild(request: RegisterChildRequest) async throws -> ChildResponse {
        let endpoint = APIEndpoint(
            path: "api/v1/auth/register/child",
            method: .POST,
            body: request
        )
        return try await apiClient.request(endpoint)
    }

    func getCurrentUser() async throws -> User {
        let endpoint = APIEndpoint(
            path: "api/v1/auth/me",
            method: .GET
        )
        return try await apiClient.request(endpoint)
    }
}
```

---

## Testing Strategy

### Test Structure (30+ Comprehensive Tests)
```swift
// AllowanceTrackerTests/ViewModels/LoginViewModelTests.swift
import XCTest
import Combine
@testable import AllowanceTracker

@MainActor
final class LoginViewModelTests: XCTestCase {
    var sut: LoginViewModel!
    var mockAuthRepository: MockAuthRepository!
    var cancellables: Set<AnyCancellable>!

    override func setUp() {
        super.setUp()
        mockAuthRepository = MockAuthRepository()
        sut = LoginViewModel(authRepository: mockAuthRepository)
        cancellables = []
    }

    override func tearDown() {
        sut = nil
        mockAuthRepository = nil
        cancellables = nil
        super.tearDown()
    }

    // MARK: - Test Login Success
    func testLoginSuccess_UpdatesIsAuthenticatedToTrue() async {
        // Arrange
        mockAuthRepository.loginResult = .success(AuthResponse.mockParent())
        sut.email = "parent@example.com"
        sut.password = "password123"

        // Act
        await sut.login()

        // Assert
        XCTAssertTrue(sut.isAuthenticated)
        XCTAssertNil(sut.errorMessage)
        XCTAssertFalse(sut.isLoading)
    }

    // MARK: - Test Login Failure
    func testLoginFailure_SetsErrorMessage() async {
        // Arrange
        mockAuthRepository.loginResult = .failure(.unauthorized)
        sut.email = "wrong@example.com"
        sut.password = "wrongpassword"

        // Act
        await sut.login()

        // Assert
        XCTAssertFalse(sut.isAuthenticated)
        XCTAssertEqual(sut.errorMessage, "Invalid email or password")
        XCTAssertFalse(sut.isLoading)
    }

    // MARK: - Test Validation
    func testLogin_WithEmptyEmail_ShowsError() {
        // Arrange
        sut.email = ""
        sut.password = "password123"

        // Act
        let canSubmit = sut.canSubmit

        // Assert
        XCTAssertFalse(canSubmit)
    }

    func testLogin_WithInvalidEmail_ShowsError() async {
        // Arrange
        sut.email = "invalidemail"
        sut.password = "password123"

        // Act
        await sut.login()

        // Assert
        XCTAssertEqual(sut.errorMessage, "Invalid email format")
    }

    // MARK: - Test Loading State
    func testLogin_SetsIsLoadingDuringRequest() async {
        // Arrange
        mockAuthRepository.loginDelay = 0.5
        sut.email = "parent@example.com"
        sut.password = "password123"

        // Act
        Task {
            await sut.login()
        }

        // Assert
        try? await Task.sleep(nanoseconds: 100_000_000) // 0.1 seconds
        XCTAssertTrue(sut.isLoading)
    }

    // MARK: - Test Error Clearing
    func testEmailChange_ClearsErrorMessage() {
        // Arrange
        sut.errorMessage = "Some error"

        // Act
        sut.email = "newemail@example.com"

        // Wait for Combine publisher
        let expectation = XCTestExpectation(description: "Error cleared")
        sut.$errorMessage
            .dropFirst()
            .sink { error in
                if error == nil {
                    expectation.fulfill()
                }
            }
            .store(in: &cancellables)

        wait(for: [expectation], timeout: 1.0)

        // Assert
        XCTAssertNil(sut.errorMessage)
    }
}
```

---

## Summary

This architecture specification provides:

- **MVVM Pattern**: Clear separation of concerns with testable ViewModels
- **Dependency Injection**: Centralized container for managing dependencies
- **Networking Layer**: Type-safe API client with async/await
- **Core Data**: Offline caching with entity relationships
- **Navigation**: SwiftUI NavigationStack with deep linking support
- **State Management**: Combine publishers for reactive updates
- **Testing**: Comprehensive test structure with mocks

**Total Test Coverage Target: >80% (30+ tests per major component)**

Ready for TDD implementation with clean, maintainable, and scalable architecture!
