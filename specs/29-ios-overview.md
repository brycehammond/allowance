# iOS App Overview - Allowance Tracker Native iOS Application

## Project Vision
A native iOS application for the Allowance Tracker system, providing families with a beautiful, intuitive mobile experience for managing children's allowances, tracking transactions, and teaching financial responsibility. Built with SwiftUI for modern iOS and iPadOS devices.

## MVP Scope - What We're Building
Focus on core functionality that delivers immediate value through native iOS features:

### For Parents
- Create and manage family account
- Add/remove children from family
- Create credit/debit transactions
- View real-time balance updates
- Monitor transaction history
- Manage weekly allowance settings
- View family dashboard with analytics
- Access child-specific dashboards

### For Children
- View current balance and allowance
- See transaction history (read-only)
- Track progress toward savings goals
- Monitor weekly allowance schedule
- View personalized dashboard

### Authentication & Security
- JWT-based authentication with existing API
- Secure token storage in Keychain
- Biometric authentication (Face ID/Touch ID)
- Automatic session management
- Secure communication over HTTPS

## Technical Stack (Modern & Native)

### Core Technologies
- **Language**: Swift 5.9+ (latest stable)
- **UI Framework**: SwiftUI (iOS 17+, iPadOS 17+)
- **Architecture**: MVVM (Model-View-ViewModel)
- **Reactive Framework**: Combine (Apple's reactive framework)
- **Networking**: URLSession (native, no Alamofire for MVP)
- **Local Storage**: Core Data (offline caching)
- **Secure Storage**: Keychain (for JWT tokens)
- **Testing**: XCTest + Quick/Nimble (unit & integration tests)
- **CI/CD**: Xcode Cloud or GitHub Actions

### Swift Package Dependencies (Minimal for MVP)
```swift
dependencies: [
    // Keychain wrapper for secure token storage
    .package(url: "https://github.com/kishikawakatsumi/KeychainAccess.git", from: "4.2.2")
]
```

### Post-MVP Dependencies (Future)
```swift
// Add these after MVP is stable
dependencies: [
    // Image loading and caching
    .package(url: "https://github.com/onevcat/Kingfisher.git", from: "7.10.0"),

    // SignalR client for real-time updates
    .package(url: "https://github.com/moozzyk/SignalR-Client-Swift.git", from: "0.9.0"),

    // Charts for analytics (if not using Swift Charts)
    .package(url: "https://github.com/danielgindi/Charts.git", from: "5.0.0")
]
```

### Why These Choices?

#### SwiftUI Over UIKit
- Modern, declarative UI development
- Automatic support for dark mode, dynamic type, accessibility
- Less code, faster development
- Built-in animations and transitions
- Cross-platform (iOS, iPadOS, watchOS future)

#### MVVM Architecture
- Clean separation of concerns
- Testable business logic in ViewModels
- SwiftUI's natural fit with MVVM
- Easy to mock for testing
- Scalable as app grows

#### Combine Over RxSwift/ReactiveSwift
- Native Apple framework (no dependencies)
- Deep integration with SwiftUI
- Better performance than third-party solutions
- Official support and documentation
- Future-proof

#### URLSession Over Alamofire
- Native networking (no dependencies)
- async/await support (Swift 5.5+)
- Excellent performance
- Well-documented
- Good enough for our API needs

#### Core Data Over Realm
- Native persistence framework
- Zero dependencies
- Excellent SwiftUI integration (@FetchRequest)
- iCloud sync support (future)
- Well-tested and mature

## Simple Project Structure

```
AllowanceTracker/
├── App/
│   ├── AllowanceTrackerApp.swift          # App entry point
│   ├── AppDelegate.swift                   # App lifecycle
│   └── Configuration/
│       ├── Environment.swift               # API base URLs
│       └── AppConfiguration.swift          # App settings
├── Models/
│   ├── Domain/
│   │   ├── User.swift                     # User model
│   │   ├── Child.swift                    # Child model
│   │   ├── Transaction.swift              # Transaction model
│   │   ├── Family.swift                   # Family model
│   │   └── TransactionType.swift          # Enums
│   ├── API/
│   │   ├── AuthResponse.swift             # API response models
│   │   ├── ChildResponse.swift
│   │   ├── TransactionResponse.swift
│   │   └── DashboardResponse.swift
│   └── CoreData/
│       ├── AllowanceTracker.xcdatamodeld  # Core Data model
│       ├── UserEntity+CoreData.swift
│       ├── ChildEntity+CoreData.swift
│       └── TransactionEntity+CoreData.swift
├── ViewModels/
│   ├── Auth/
│   │   ├── LoginViewModel.swift           # Login logic
│   │   └── RegisterViewModel.swift        # Registration logic
│   ├── Dashboard/
│   │   ├── ParentDashboardViewModel.swift
│   │   └── ChildDashboardViewModel.swift
│   ├── Children/
│   │   └── ChildListViewModel.swift
│   └── Transactions/
│       ├── TransactionListViewModel.swift
│       └── CreateTransactionViewModel.swift
├── Views/
│   ├── Auth/
│   │   ├── LoginView.swift                # Login screen
│   │   ├── RegisterParentView.swift       # Parent registration
│   │   └── RegisterChildView.swift        # Child registration
│   ├── Dashboard/
│   │   ├── ParentDashboardView.swift      # Parent home screen
│   │   ├── ChildDashboardView.swift       # Child home screen
│   │   └── Components/
│   │       ├── BalanceCardView.swift
│   │       ├── ChildCardView.swift
│   │       └── TransactionRowView.swift
│   ├── Children/
│   │   ├── ChildListView.swift            # List of children
│   │   └── ChildDetailView.swift          # Child details
│   ├── Transactions/
│   │   ├── TransactionListView.swift      # Transaction history
│   │   └── CreateTransactionView.swift    # Add transaction
│   └── Settings/
│       ├── SettingsView.swift             # App settings
│       └── ProfileView.swift              # User profile
├── Services/
│   ├── Network/
│   │   ├── APIClient.swift                # Core networking
│   │   ├── APIEndpoint.swift              # Endpoint definitions
│   │   ├── APIError.swift                 # Error types
│   │   └── RequestBuilder.swift           # Request construction
│   ├── API/
│   │   ├── AuthAPI.swift                  # Auth endpoints
│   │   ├── FamiliesAPI.swift              # Family endpoints
│   │   ├── ChildrenAPI.swift              # Children endpoints
│   │   ├── TransactionsAPI.swift          # Transaction endpoints
│   │   └── DashboardAPI.swift             # Dashboard endpoints
│   ├── Storage/
│   │   ├── KeychainService.swift          # Token storage
│   │   ├── CoreDataService.swift          # Offline persistence
│   │   └── UserDefaultsService.swift      # App preferences
│   └── Auth/
│       └── AuthenticationService.swift    # Auth state management
├── Repositories/
│   ├── AuthRepository.swift               # Auth data layer
│   ├── FamilyRepository.swift             # Family data layer
│   ├── ChildRepository.swift              # Child data layer
│   └── TransactionRepository.swift        # Transaction data layer
├── Utilities/
│   ├── Extensions/
│   │   ├── String+Validation.swift        # String helpers
│   │   ├── Decimal+Currency.swift         # Money formatting
│   │   └── Date+Extensions.swift          # Date helpers
│   ├── Helpers/
│   │   ├── CurrencyFormatter.swift        # Money display
│   │   └── DateFormatter.swift            # Date display
│   └── Constants/
│       └── AppConstants.swift             # App-wide constants
├── Resources/
│   ├── Assets.xcassets/                   # Images, colors
│   ├── Localizable.strings                # Localization
│   └── Info.plist                         # App configuration
└── AllowanceTrackerTests/
    ├── ViewModels/
    │   ├── LoginViewModelTests.swift
    │   ├── DashboardViewModelTests.swift
    │   └── TransactionViewModelTests.swift
    ├── Services/
    │   ├── APIClientTests.swift
    │   └── AuthServiceTests.swift
    ├── Repositories/
    │   └── TransactionRepositoryTests.swift
    └── Mocks/
        ├── MockAPIClient.swift
        └── MockAuthService.swift
```

## Core Features (Detailed MVP)

### Phase 1: Authentication & Core Setup (Week 1-2)
**Goal**: User can authenticate and see basic family info

#### Features
- **Login Screen**
  - Email/password authentication
  - "Remember Me" toggle (uses Keychain)
  - Password validation
  - Error handling with user-friendly messages
  - Loading states

- **Parent Registration**
  - Create parent account
  - Set up family name
  - Email/password validation
  - Automatic family creation
  - Navigate to dashboard on success

- **Child Registration** (Parent only)
  - Add child to existing family
  - Set initial weekly allowance
  - Create child account credentials
  - Automatic family association

- **Session Management**
  - JWT token storage in Keychain
  - Automatic token refresh on 401
  - Biometric authentication (Face ID/Touch ID)
  - Auto-logout on token expiration
  - Persistent login state

#### Technical Implementation
- `AuthenticationService` manages auth state
- `KeychainService` for secure token storage
- `@EnvironmentObject` for global auth state
- SwiftUI navigation based on auth state
- Combine publishers for reactive auth changes

#### Success Metrics
- Users can register/login successfully
- Tokens stored securely in Keychain
- Session persists across app launches
- Biometric auth works correctly
- All auth tests passing (15+ tests)

---

### Phase 2: Dashboard & Transactions (Week 3-4)
**Goal**: View balances, create transactions, see history

#### Features
- **Parent Dashboard**
  - Family overview card (total children, total balance)
  - List of all children with balances
  - Quick action: Add transaction
  - Quick action: Pay weekly allowance
  - Recent transaction feed (last 10)
  - Pull-to-refresh
  - Loading and empty states

- **Child Dashboard**
  - Large balance display
  - Weekly allowance info
  - Next allowance date countdown
  - Recent transactions (last 10)
  - Monthly stats (earned, spent, net)
  - Pull-to-refresh
  - Motivational messages

- **Transaction List**
  - Paginated transaction history
  - Filter by child (parent only)
  - Credit/debit color coding
  - Balance after each transaction
  - Created by & timestamp
  - Infinite scroll loading
  - Empty state for no transactions

- **Create Transaction** (Parent only)
  - Select child from dropdown
  - Amount input with currency formatting
  - Type selector (Credit/Debit)
  - Description text field
  - Insufficient funds validation
  - Success feedback with animation
  - Real-time balance preview

#### Technical Implementation
- `ParentDashboardViewModel` aggregates family data
- `ChildDashboardViewModel` loads child-specific data
- `TransactionListViewModel` handles pagination
- `CreateTransactionViewModel` manages form state
- Combine for reactive balance updates
- SwiftUI animations for feedback

#### API Integration
- `GET /api/v1/dashboard/parent`
- `GET /api/v1/dashboard/child`
- `GET /api/v1/transactions/children/{childId}`
- `POST /api/v1/transactions`

#### Success Metrics
- Dashboard loads in <1 second
- Transactions appear immediately after creation
- Pagination works smoothly (20 per page)
- Form validation prevents errors
- All transaction tests passing (20+ tests)

---

### Phase 3: Categories, Charts & Goals (Week 5-6)
**Goal**: Visualize spending, categorize transactions, track goals

#### Features
- **Transaction Categories**
  - Assign category to transaction
  - Category icons and colors
  - Category filter in transaction list
  - Category spending summary

- **Charts & Analytics**
  - Balance over time (line chart)
  - Spending by category (pie chart)
  - Earned vs. Spent comparison (bar chart)
  - Weekly/monthly toggle
  - Export chart as image

- **Savings Goals**
  - Create savings goal with target amount
  - Track progress with progress bar
  - Estimated completion date
  - Goal achievement notifications
  - Goal history

#### Technical Implementation
- Use Swift Charts (iOS 16+) for native charts
- `TransactionCategoryViewModel` manages categories
- `ChartsViewModel` computes chart data
- `SavingsGoalViewModel` tracks goal progress
- Core Data caching for offline viewing

#### API Integration
- `GET /api/v1/transactions/categories`
- `GET /api/v1/analytics/spending`
- `GET /api/v1/goals`
- `POST /api/v1/goals`
- `PUT /api/v1/goals/{id}/progress`

#### Success Metrics
- Charts render smoothly
- Category filtering works correctly
- Goals update in real-time
- Analytics tests passing (15+ tests)

---

### Phase 4: Real-time Updates & Offline Support (Week 7-8)
**Goal**: Real-time sync and offline functionality

#### Features
- **Real-time Updates**
  - SignalR connection to API
  - Live balance updates when transactions created
  - Push notifications for allowance payments
  - Family member activity feed

- **Offline Mode**
  - Core Data caching of all data
  - View cached transactions offline
  - Queue transactions for sync
  - Sync indicator when online
  - Conflict resolution

- **Network Status**
  - Connection status indicator
  - Automatic retry on failure
  - Offline mode banner
  - Manual sync button

#### Technical Implementation
- SignalR-Client-Swift package
- `NetworkMonitor` with NWPathMonitor
- Core Data as cache layer
- Background sync queue
- Combine for real-time updates

#### Success Metrics
- Real-time updates arrive within 1 second
- Offline mode works without crashes
- Sync completes successfully when online
- Network status visible to user

---

### Phase 5: Chores, Settings & Polish (Week 9-10)
**Goal**: Complete feature set, polish UX, App Store ready

#### Features
- **Chores System**
  - Create chores with value
  - Assign chores to children
  - Mark chores complete (child)
  - Approve chores for payment (parent)
  - Chore history

- **Settings**
  - Edit profile (name, email)
  - Change password
  - Enable/disable Face ID
  - Notification preferences
  - Family settings (parent only)
  - Logout

- **UI Polish**
  - Haptic feedback
  - Smooth animations
  - Error recovery flows
  - Accessibility labels
  - VoiceOver support
  - Dynamic Type support
  - Dark mode refinement

#### Technical Implementation
- `ChoresViewModel` manages chore state
- `SettingsViewModel` handles preferences
- Accessibility modifiers throughout
- Haptic feedback via UIImpactFeedbackGenerator
- A/B tested animations

#### Success Metrics
- All features working end-to-end
- >80% test coverage
- Zero critical bugs
- Accessibility score >90%
- App Store submission ready

---

## Data Models

### User (Domain Model)
```swift
import Foundation

struct User: Identifiable, Codable, Equatable {
    let id: UUID
    let email: String
    let firstName: String
    let lastName: String
    let role: UserRole
    let familyId: UUID?

    var fullName: String {
        "\(firstName) \(lastName)"
    }

    var isParent: Bool {
        role == .parent
    }

    var isChild: Bool {
        role == .child
    }
}

enum UserRole: String, Codable {
    case parent = "Parent"
    case child = "Child"
}
```

### Child (Domain Model)
```swift
import Foundation

struct Child: Identifiable, Codable, Equatable {
    let childId: UUID
    let userId: UUID
    let firstName: String
    let lastName: String
    let email: String
    let currentBalance: Decimal
    let weeklyAllowance: Decimal
    let lastAllowanceDate: Date?
    let nextAllowanceDate: Date?

    var fullName: String {
        "\(firstName) \(lastName)"
    }

    var formattedBalance: String {
        CurrencyFormatter.format(currentBalance)
    }

    var daysUntilAllowance: Int? {
        guard let nextDate = nextAllowanceDate else { return nil }
        return Calendar.current.dateComponents([.day], from: Date(), to: nextDate).day
    }
}
```

### Transaction (Domain Model)
```swift
import Foundation

struct Transaction: Identifiable, Codable, Equatable {
    let id: UUID
    let childId: UUID
    let amount: Decimal
    let type: TransactionType
    let description: String
    let balanceAfter: Decimal
    let createdBy: UUID
    let createdByName: String
    let createdAt: Date

    var isCredit: Bool {
        type == .credit
    }

    var isDebit: Bool {
        type == .debit
    }

    var formattedAmount: String {
        let formatted = CurrencyFormatter.format(amount)
        return isCredit ? "+\(formatted)" : "-\(formatted)"
    }

    var amountColor: Color {
        isCredit ? .green : .red
    }
}

enum TransactionType: String, Codable {
    case credit = "Credit"
    case debit = "Debit"
}
```

### Family (Domain Model)
```swift
import Foundation

struct Family: Identifiable, Codable, Equatable {
    let id: UUID
    let name: String
    let createdAt: Date
    let memberCount: Int
    let childrenCount: Int
}
```

### AuthResponse (API Response Model)
```swift
import Foundation

struct AuthResponse: Codable {
    let userId: UUID
    let email: String
    let firstName: String
    let lastName: String
    let role: UserRole
    let familyId: UUID?
    let familyName: String?
    let token: String
    let expiresAt: Date
}
```

---

## Development Approach

### Test-Driven Development (TDD) - MANDATORY

#### TDD Cycle for iOS
1. **RED**: Write failing XCTest
2. **GREEN**: Implement minimum code to pass
3. **REFACTOR**: Clean up while tests stay green
4. **REPEAT**: Next test

#### Example: Login ViewModel Test First
```swift
import XCTest
import Combine
@testable import AllowanceTracker

final class LoginViewModelTests: XCTestCase {
    var sut: LoginViewModel!
    var mockAuthService: MockAuthService!
    var cancellables: Set<AnyCancellable>!

    override func setUp() {
        super.setUp()
        mockAuthService = MockAuthService()
        sut = LoginViewModel(authService: mockAuthService)
        cancellables = []
    }

    override func tearDown() {
        sut = nil
        mockAuthService = nil
        cancellables = nil
        super.tearDown()
    }

    // RED: Write test first
    func testLoginSuccess_SetsIsAuthenticatedTrue() async throws {
        // Arrange
        mockAuthService.loginResult = .success(AuthResponse.mockParent())
        sut.email = "parent@example.com"
        sut.password = "password123"

        // Act
        await sut.login()

        // Assert
        XCTAssertTrue(sut.isAuthenticated)
        XCTAssertNil(sut.errorMessage)
        XCTAssertFalse(sut.isLoading)
    }

    func testLoginFailure_SetsErrorMessage() async throws {
        // Arrange
        mockAuthService.loginResult = .failure(.unauthorized)
        sut.email = "wrong@example.com"
        sut.password = "wrongpass"

        // Act
        await sut.login()

        // Assert
        XCTAssertFalse(sut.isAuthenticated)
        XCTAssertEqual(sut.errorMessage, "Invalid email or password")
        XCTAssertFalse(sut.isLoading)
    }

    func testLogin_WithEmptyEmail_ShowsValidationError() {
        // Arrange
        sut.email = ""
        sut.password = "password123"

        // Act
        let isValid = sut.validateForm()

        // Assert
        XCTAssertFalse(isValid)
        XCTAssertEqual(sut.errorMessage, "Email is required")
    }
}
```

### Running Tests
```bash
# Run all tests
xcodebuild test -scheme AllowanceTracker -destination 'platform=iOS Simulator,name=iPhone 15 Pro'

# Run specific test file
xcodebuild test -scheme AllowanceTracker -only-testing:AllowanceTrackerTests/LoginViewModelTests

# Run tests with coverage
xcodebuild test -scheme AllowanceTracker -enableCodeCoverage YES

# View coverage report
open DerivedData/AllowanceTracker/Logs/Test/*.xcresult
```

### Critical Test Coverage (>80% Required)
Must have comprehensive tests for:
- Authentication flow (login, register, logout)
- Transaction creation and validation
- Balance calculations
- Network error handling
- Token refresh logic
- Offline sync operations
- Form validation
- Navigation flows

---

## Performance Goals

### Target Metrics
- **Cold app launch**: <2 seconds
- **API requests**: <500ms (90th percentile)
- **Screen transitions**: 60 FPS (smooth)
- **Transaction creation**: <300ms
- **Offline mode**: Instant data access
- **Memory usage**: <100MB typical
- **Battery drain**: <5% per hour active use

### Optimization Strategies
- Use `LazyVStack` for long lists
- Implement pagination (20 items per page)
- Cache API responses in Core Data
- Use async image loading with placeholders
- Minimize ViewBuilder complexity
- Profile with Instruments regularly
- Use `task` modifier for async operations
- Debounce search/filter inputs

---

## Security Considerations

### Authentication Security
- JWT tokens stored in Keychain only (never UserDefaults)
- Biometric authentication with fallback to password
- Automatic logout after 7 days of inactivity
- Certificate pinning for API requests (future)
- Encrypted Core Data store

### Data Protection
- All API communication over HTTPS only
- Sensitive data cleared on logout
- Background screenshot protection
- Pasteboard security for sensitive data
- Keychain items use kSecAttrAccessibleWhenUnlocked

### Authorization Rules
- Parents can access all family data
- Children can only access their own data
- Transaction creation requires parent role
- API validates all permissions server-side
- Client-side checks for UX only (not security)

---

## Accessibility & Localization

### Accessibility Requirements (WCAG 2.1 Level AA)
- VoiceOver support for all screens
- Dynamic Type support (all text scales)
- Minimum tap target size: 44x44 points
- Sufficient color contrast (4.5:1 minimum)
- Descriptive accessibility labels
- Haptic feedback for important actions
- Reduce Motion support
- Screen Reader tested

### Localization (MVP: English only)
```swift
// Post-MVP: Add Spanish, French, German, Chinese
enum LocalizedString: String {
    case loginTitle = "login.title"
    case balanceLabel = "balance.label"
    case transactionCreated = "transaction.created"

    var localized: String {
        NSLocalizedString(rawValue, comment: "")
    }
}
```

---

## Deployment

### App Store Configuration
```
App Name: Allowance Tracker
Bundle ID: com.allowancetracker.ios
Version: 1.0.0 (Build 1)
Category: Finance
Age Rating: 4+
Price: Free
In-App Purchases: None (MVP)
```

### TestFlight Beta Distribution
- Internal testing: Development team
- External testing: 20-30 family beta testers
- Collect feedback via TestFlight forms
- Iterate based on real user feedback

### CI/CD Pipeline (Xcode Cloud)
```yaml
# xcode-cloud.yml
version: 1.0
workflows:
  - name: Test & Build
    triggers:
      - branch: main
        action: push
    steps:
      - test:
          platform: iOS
          device: iPhone 15 Pro
          os: iOS 17.0
      - build:
          platform: iOS
          configuration: Release
      - archive:
          upload_to_testflight: true
```

---

## Risk Mitigation

### Technical Risks
| Risk | Impact | Mitigation |
|------|--------|------------|
| API changes break app | High | Version API endpoints, comprehensive tests |
| Token expiration issues | Medium | Implement robust refresh logic |
| Offline sync conflicts | Medium | Last-write-wins with conflict UI |
| App Store rejection | High | Follow HIG strictly, test on devices |
| Performance on older devices | Medium | Test on iPhone 12, optimize |

### User Experience Risks
| Risk | Impact | Mitigation |
|------|--------|------------|
| Complex parent flow | Medium | User testing, simplify onboarding |
| Children confused by UI | High | Child-friendly design, icons |
| Lost authentication | High | Biometric + "Remember Me" |

---

## Success Criteria

### MVP Must-Have (Launch Blockers)
- ✅ Users can register and login
- ✅ Parents can add children to family
- ✅ Parents can create transactions
- ✅ Balances update correctly
- ✅ Transaction history displays properly
- ✅ Weekly allowance info shown
- ✅ App passes App Store review
- ✅ >80% test coverage
- ✅ Crash-free rate >99.5%
- ✅ Accessibility score >85%

### Nice-to-Have (Post-MVP v1.1)
- Real-time updates via SignalR
- Offline mode with sync
- Transaction categories
- Charts and analytics
- Savings goals
- Chores system
- Push notifications
- iPad optimization
- Widget support
- Apple Watch companion app

---

## Development Timeline (10 Weeks Total)

### Week 1-2: Foundation & Authentication (Phase 1)
- Project setup with SwiftUI
- MVVM architecture skeleton
- APIClient with URLSession
- Keychain service
- Login/Register screens
- Auth tests (15+ tests)

### Week 3-4: Core Features (Phase 2)
- Parent/Child dashboards
- Transaction list
- Create transaction
- Balance display
- Transaction tests (20+ tests)

### Week 5-6: Enhanced Features (Phase 3)
- Transaction categories
- Charts with Swift Charts
- Savings goals
- Analytics tests (15+ tests)

### Week 7-8: Real-time & Offline (Phase 4)
- SignalR integration
- Core Data caching
- Offline sync
- Network monitoring

### Week 9-10: Polish & Launch (Phase 5)
- Chores system
- Settings & profile
- UI polish & animations
- Accessibility audit
- App Store submission
- TestFlight distribution

---

## Next Steps After MVP

### Version 1.1 Features
1. Push notifications for transactions
2. iPad-optimized layouts
3. Home Screen widgets
4. Siri shortcuts integration
5. Export transaction reports (PDF/CSV)

### Version 1.2 Features
1. Family sharing for subscription
2. Multiple currency support
3. Spanish localization
4. Achievement badges system
5. Photo attachments for transactions

### Version 2.0 Features
1. Apple Watch app
2. iMessage app for quick payments
3. Subscription tiers (premium features)
4. iCloud sync between devices
5. macOS Catalyst app

---

## Key Files Reference

### Entry Points
- `AllowanceTrackerApp.swift` - App lifecycle and dependency injection
- `Environment.swift` - API configuration (dev/staging/prod)

### Core Services
- `APIClient.swift` - All networking logic
- `AuthenticationService.swift` - Auth state management
- `KeychainService.swift` - Secure token storage
- `CoreDataService.swift` - Offline persistence

### Main Views
- `LoginView.swift` - User authentication
- `ParentDashboardView.swift` - Parent home screen
- `ChildDashboardView.swift` - Child home screen
- `CreateTransactionView.swift` - Transaction creation

### ViewModels
- `LoginViewModel.swift` - Login business logic
- `ParentDashboardViewModel.swift` - Dashboard data
- `TransactionListViewModel.swift` - Transaction pagination

---

## Best Practices

### SwiftUI Guidelines
1. **Single Responsibility**: Each view does one thing
2. **Extract Subviews**: Keep View body under 15 lines
3. **Prefer Composition**: Build complex UIs from small views
4. **State Management**: Use @State, @StateObject, @ObservedObject correctly
5. **Environment Objects**: Share auth state, API client globally

### Swift Coding Standards
1. **Use Swift 5.9+ syntax**: async/await, Actors, @Observable
2. **Prefer value types**: Use structs for models
3. **Protocol-oriented**: Create protocols for testability
4. **Guard statements**: Early exits for cleaner code
5. **SwiftLint**: Enforce consistent style

### MVVM Pattern
```swift
// ViewModel: Business logic and state
@MainActor
final class LoginViewModel: ObservableObject {
    @Published var email = ""
    @Published var password = ""
    @Published var isLoading = false
    @Published var errorMessage: String?
    @Published var isAuthenticated = false

    private let authService: AuthenticationServiceProtocol

    init(authService: AuthenticationServiceProtocol = AuthenticationService.shared) {
        self.authService = authService
    }

    func login() async {
        isLoading = true
        errorMessage = nil

        do {
            try await authService.login(email: email, password: password)
            isAuthenticated = true
        } catch {
            errorMessage = error.localizedDescription
        }

        isLoading = false
    }
}

// View: UI only, no business logic
struct LoginView: View {
    @StateObject private var viewModel = LoginViewModel()

    var body: some View {
        VStack(spacing: 20) {
            TextField("Email", text: $viewModel.email)
                .textFieldStyle(.roundedBorder)
                .autocapitalization(.none)
                .keyboardType(.emailAddress)

            SecureField("Password", text: $viewModel.password)
                .textFieldStyle(.roundedBorder)

            if let error = viewModel.errorMessage {
                Text(error)
                    .foregroundColor(.red)
                    .font(.caption)
            }

            Button("Login") {
                Task {
                    await viewModel.login()
                }
            }
            .disabled(viewModel.isLoading)
        }
        .padding()
    }
}
```

---

## Resources

### Apple Documentation
- [SwiftUI Documentation](https://developer.apple.com/documentation/swiftui)
- [Combine Framework](https://developer.apple.com/documentation/combine)
- [Human Interface Guidelines](https://developer.apple.com/design/human-interface-guidelines)
- [App Store Review Guidelines](https://developer.apple.com/app-store/review/guidelines/)

### Swift & iOS
- [Swift.org](https://swift.org/)
- [Swift by Sundell](https://www.swiftbysundell.com/)
- [Hacking with Swift](https://www.hackingwithswift.com/)
- [Point-Free](https://www.pointfree.co/)

### Testing
- [XCTest Documentation](https://developer.apple.com/documentation/xctest)
- [Quick/Nimble](https://github.com/Quick/Quick)
- [iOS Unit Testing by Example](https://qualitycoding.org/)

---

## Summary

This iOS app specification provides a comprehensive roadmap for building a production-ready native iOS application for the Allowance Tracker system. Key highlights:

- **Modern Stack**: SwiftUI, Combine, MVVM, async/await
- **Minimal Dependencies**: URLSession, Core Data, Keychain (native)
- **TDD Approach**: >80% test coverage requirement
- **10-Week Timeline**: MVP ready for App Store submission
- **Security First**: Keychain storage, biometric auth, HTTPS only
- **Accessibility**: VoiceOver, Dynamic Type, WCAG 2.1 compliant
- **Scalable**: Clean architecture ready for future features

The app integrates seamlessly with the existing ASP.NET Core API, providing families with a beautiful, intuitive mobile experience for managing allowances and teaching financial responsibility.

**Ready for TDD implementation and App Store launch! 🚀**
