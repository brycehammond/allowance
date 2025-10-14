# iOS Testing Strategy - Test-Driven Development for Swift

## Overview
Comprehensive testing approach for the Allowance Tracker iOS application following strict Test-Driven Development (TDD) methodology. Covers unit testing, UI testing, integration testing, and performance testing using XCTest, XCUITest, and modern Swift testing frameworks.

## Core Philosophy: Test-First Development
**Every feature starts with a failing test**. We follow strict TDD methodology:
1. **Red**: Write a failing test that defines desired behavior
2. **Green**: Write minimum code to make the test pass
3. **Refactor**: Improve code while keeping tests green
4. **Repeat**: Continue for next feature

## TDD Principles for iOS

### The Three Laws of TDD (Uncle Bob)
1. **You may not write production code until you have written a failing unit test**
2. **You may not write more of a unit test than is sufficient to fail**
3. **You may not write more production code than is sufficient to pass the currently failing test**

### Red-Green-Refactor Cycle
```
┌─────┐      ┌───────┐      ┌──────────┐
│ RED │ ───> │ GREEN │ ───> │ REFACTOR │
└─────┘      └───────┘      └──────────┘
   ↑                              │
   └──────────────────────────────┘
```

## Testing Stack

### Core Testing Frameworks
- **XCTest**: Apple's native testing framework (unit & integration tests)
- **XCUITest**: UI automation testing framework
- **Swift Testing** (iOS 17+): Modern Swift-native testing with `@Test` macro
- **Combine Testing**: Test async publishers and subscribers
- **ViewInspector**: SwiftUI view testing (third-party)

### Test Dependencies
```swift
// Package.swift
dependencies: [
    .package(url: "https://github.com/nalexn/ViewInspector", from: "0.9.0")
]
```

## Project Structure

```
AllowanceTrackerTests/
├── UnitTests/
│   ├── ViewModels/
│   │   ├── DashboardViewModelTests.swift
│   │   ├── AuthViewModelTests.swift
│   │   └── TransactionViewModelTests.swift
│   ├── Services/
│   │   ├── APIClientTests.swift
│   │   ├── AuthServiceTests.swift
│   │   ├── KeychainServiceTests.swift
│   │   └── SignalRManagerTests.swift
│   ├── Repositories/
│   │   ├── TransactionRepositoryTests.swift
│   │   └── ChildRepositoryTests.swift
│   ├── Models/
│   │   ├── TransactionTests.swift
│   │   └── ChildTests.swift
│   └── Utilities/
│       ├── DateFormatterTests.swift
│       └── ValidationTests.swift
├── UITests/
│   ├── LoginFlowUITests.swift
│   ├── DashboardUITests.swift
│   ├── TransactionUITests.swift
│   └── NavigationUITests.swift
├── IntegrationTests/
│   ├── APIIntegrationTests.swift
│   ├── CoreDataIntegrationTests.swift
│   └── SignalRIntegrationTests.swift
└── Mocks/
    ├── MockAPIClient.swift
    ├── MockKeychainService.swift
    ├── MockUserDefaults.swift
    └── MockSignalRManager.swift
```

## 1. Unit Testing (100+ Tests)

### ViewModel Testing Pattern

#### AuthViewModel Tests
```swift
import XCTest
import Combine
@testable import AllowanceTracker

@MainActor
final class AuthViewModelTests: XCTestCase {
    var sut: AuthViewModel!
    var mockAPIClient: MockAPIClient!
    var mockKeychainService: MockKeychainService!
    var cancellables: Set<AnyCancellable>!

    override func setUp() {
        super.setUp()
        mockAPIClient = MockAPIClient()
        mockKeychainService = MockKeychainService()
        sut = AuthViewModel(
            apiClient: mockAPIClient,
            keychainService: mockKeychainService
        )
        cancellables = []
    }

    override func tearDown() {
        sut = nil
        mockAPIClient = nil
        mockKeychainService = nil
        cancellables = nil
        super.tearDown()
    }

    // MARK: - Login Tests

    func test_login_withValidCredentials_setsIsAuthenticatedToTrue() async throws {
        // Arrange
        let email = "test@example.com"
        let password = "Password123!"
        let expectedToken = "valid.jwt.token"

        mockAPIClient.mockResponse = LoginResponseDTO(
            token: expectedToken,
            userId: UUID(),
            email: email,
            firstName: "John",
            lastName: "Doe",
            role: .parent,
            expiresAt: Date().addingTimeInterval(86400)
        )

        // Act
        try await sut.login(email: email, password: password)

        // Assert
        XCTAssertTrue(sut.isAuthenticated)
        XCTAssertNotNil(sut.currentUser)
        XCTAssertEqual(sut.authToken, expectedToken)
    }

    func test_login_withInvalidCredentials_setsErrorMessage() async throws {
        // Arrange
        let email = "wrong@example.com"
        let password = "wrongpassword"
        mockAPIClient.shouldFail = true
        mockAPIClient.error = APIError.unauthorized

        // Act
        do {
            try await sut.login(email: email, password: password)
            XCTFail("Should throw error")
        } catch {
            // Assert
            XCTAssertFalse(sut.isAuthenticated)
            XCTAssertNil(sut.currentUser)
            XCTAssertNotNil(sut.errorMessage)
        }
    }

    func test_login_savesTokenToKeychain() async throws {
        // Arrange
        let token = "test.jwt.token"
        mockAPIClient.mockResponse = LoginResponseDTO(
            token: token,
            userId: UUID(),
            email: "test@example.com",
            firstName: "John",
            lastName: "Doe",
            role: .parent,
            expiresAt: Date().addingTimeInterval(86400)
        )

        // Act
        try await sut.login(email: "test@example.com", password: "Password123!")

        // Assert
        let savedToken = mockKeychainService.savedValues["authToken"] as? String
        XCTAssertEqual(savedToken, token)
    }

    func test_logout_clearsAuthenticationState() async {
        // Arrange
        try? await sut.login(email: "test@example.com", password: "Password123!")

        // Act
        await sut.logout()

        // Assert
        XCTAssertFalse(sut.isAuthenticated)
        XCTAssertNil(sut.currentUser)
        XCTAssertNil(sut.authToken)
    }

    func test_logout_removesTokenFromKeychain() async {
        // Arrange
        try? await sut.login(email: "test@example.com", password: "Password123!")

        // Act
        await sut.logout()

        // Assert
        XCTAssertNil(mockKeychainService.savedValues["authToken"])
    }

    // MARK: - Registration Tests

    func test_register_withValidData_createsAccount() async throws {
        // Arrange
        mockAPIClient.mockResponse = RegisterResponseDTO(
            userId: UUID(),
            email: "new@example.com",
            firstName: "Jane",
            lastName: "Doe",
            role: .parent,
            token: "new.jwt.token",
            expiresAt: Date().addingTimeInterval(86400)
        )

        // Act
        try await sut.register(
            email: "new@example.com",
            password: "Password123!",
            firstName: "Jane",
            lastName: "Doe",
            familyName: "Doe Family"
        )

        // Assert
        XCTAssertTrue(sut.isAuthenticated)
        XCTAssertEqual(sut.currentUser?.firstName, "Jane")
    }

    func test_register_withWeakPassword_throwsValidationError() async {
        // Arrange
        let weakPassword = "weak"

        // Act & Assert
        do {
            try await sut.register(
                email: "test@example.com",
                password: weakPassword,
                firstName: "John",
                lastName: "Doe",
                familyName: "Doe Family"
            )
            XCTFail("Should throw validation error")
        } catch {
            XCTAssertTrue(error is ValidationError)
        }
    }
}
```

#### DashboardViewModel Tests
```swift
@MainActor
final class DashboardViewModelTests: XCTestCase {
    var sut: DashboardViewModel!
    var mockAPIClient: MockAPIClient!
    var mockRepository: MockTransactionRepository!

    override func setUp() {
        super.setUp()
        mockAPIClient = MockAPIClient()
        mockRepository = MockTransactionRepository()
        sut = DashboardViewModel(
            apiClient: mockAPIClient,
            repository: mockRepository
        )
    }

    func test_loadData_fetchesBalanceAndTransactions() async throws {
        // Arrange
        let expectedBalance: Decimal = 125.50
        let expectedTransactions = [
            Transaction.mock(amount: 25.00),
            Transaction.mock(amount: -10.00)
        ]

        mockAPIClient.mockResponse = BalanceResponseDTO(balance: expectedBalance)
        mockRepository.mockTransactions = expectedTransactions

        // Act
        await sut.loadData()

        // Assert
        XCTAssertEqual(sut.currentBalance, expectedBalance)
        XCTAssertEqual(sut.recentTransactions.count, 2)
    }

    func test_handleBalanceUpdate_updatesCurrentBalance() async {
        // Arrange
        let initialBalance: Decimal = 100.00
        sut.currentBalance = initialBalance

        let updateEvent = BalanceEventDTO(
            childId: UUID(),
            newBalance: 125.00,
            previousBalance: 100.00
        )

        // Act
        await sut.handleBalanceUpdate(updateEvent)

        // Assert
        XCTAssertEqual(sut.currentBalance, 125.00)
    }

    func test_handleNewTransaction_insertsAtBeginning() async {
        // Arrange
        sut.recentTransactions = [Transaction.mock(amount: 10.00)]

        let newEvent = TransactionEventDTO(
            transactionId: UUID(),
            childId: UUID(),
            amount: 25.00,
            type: .credit,
            description: "New transaction",
            balanceAfter: 135.00,
            createdAt: Date()
        )

        // Act
        await sut.handleNewTransaction(newEvent)

        // Assert
        XCTAssertEqual(sut.recentTransactions.count, 2)
        XCTAssertEqual(sut.recentTransactions.first?.amount, 25.00)
    }

    func test_loadData_whenAPIFails_setsErrorMessage() async {
        // Arrange
        mockAPIClient.shouldFail = true
        mockAPIClient.error = APIError.serverError

        // Act
        await sut.loadData()

        // Assert
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertTrue(sut.hasError)
    }

    func test_refreshData_reloadsTransactions() async throws {
        // Test pull-to-refresh functionality
    }
}
```

#### TransactionViewModel Tests
```swift
@MainActor
final class TransactionViewModelTests: XCTestCase {
    var sut: TransactionViewModel!
    var mockAPIClient: MockAPIClient!

    override func setUp() {
        super.setUp()
        mockAPIClient = MockAPIClient()
        sut = TransactionViewModel(apiClient: mockAPIClient)
    }

    func test_createTransaction_withValidAmount_succeeds() async throws {
        // Arrange
        let childId = UUID()
        let amount: Decimal = 25.00
        let type = TransactionType.credit
        let description = "Weekly allowance"

        mockAPIClient.mockResponse = TransactionDTO(
            id: UUID(),
            childId: childId,
            amount: amount,
            type: type,
            description: description,
            balanceAfter: 125.00,
            createdAt: Date()
        )

        // Act
        try await sut.createTransaction(
            childId: childId,
            amount: amount,
            type: type,
            description: description
        )

        // Assert
        XCTAssertTrue(sut.transactionCreated)
        XCTAssertNil(sut.errorMessage)
    }

    func test_createTransaction_withNegativeAmount_throwsError() async {
        // Arrange
        let amount: Decimal = -10.00

        // Act & Assert
        do {
            try await sut.createTransaction(
                childId: UUID(),
                amount: amount,
                type: .credit,
                description: "Test"
            )
            XCTFail("Should throw validation error")
        } catch {
            XCTAssertTrue(error is ValidationError)
        }
    }

    func test_createTransaction_withInsufficientFunds_fails() async throws {
        // Test debit transaction with insufficient balance
    }

    func test_validateAmount_withZero_returnsFalse() {
        // Act
        let isValid = sut.validateAmount(0)

        // Assert
        XCTAssertFalse(isValid)
    }

    func test_validateAmount_withPositiveAmount_returnsTrue() {
        // Act
        let isValid = sut.validateAmount(10.50)

        // Assert
        XCTAssertTrue(isValid)
    }
}
```

### Service Testing

#### APIClient Tests
```swift
final class APIClientTests: XCTestCase {
    var sut: APIClient!
    var mockURLSession: MockURLSession!

    override func setUp() {
        super.setUp()
        mockURLSession = MockURLSession()
        sut = APIClient(session: mockURLSession)
    }

    func test_get_withValidEndpoint_returnsData() async throws {
        // Arrange
        let expectedData = """
        {"balance": 125.50}
        """.data(using: .utf8)!

        mockURLSession.mockData = expectedData
        mockURLSession.mockResponse = HTTPURLResponse(
            url: URL(string: "https://api.example.com")!,
            statusCode: 200,
            httpVersion: nil,
            headerFields: nil
        )

        // Act
        let response: BalanceResponseDTO = try await sut.get("/api/v1/balance")

        // Assert
        XCTAssertEqual(response.balance, 125.50)
    }

    func test_post_withBody_sendsCorrectData() async throws {
        // Arrange
        let body = LoginRequestDTO(email: "test@example.com", password: "password")

        // Act
        let _: LoginResponseDTO = try await sut.post("/api/v1/auth/login", body: body)

        // Assert
        let sentData = mockURLSession.lastRequestBody
        XCTAssertNotNil(sentData)
    }

    func test_request_withUnauthorized_throwsAuthError() async {
        // Arrange
        mockURLSession.mockResponse = HTTPURLResponse(
            url: URL(string: "https://api.example.com")!,
            statusCode: 401,
            httpVersion: nil,
            headerFields: nil
        )

        // Act & Assert
        do {
            let _: BalanceResponseDTO = try await sut.get("/api/v1/protected")
            XCTFail("Should throw unauthorized error")
        } catch {
            XCTAssertTrue(error is APIError)
            XCTAssertEqual(error as? APIError, .unauthorized)
        }
    }

    func test_request_withNetworkError_throwsNetworkError() async {
        // Test network connectivity errors
    }

    func test_request_includesAuthorizationHeader() async throws {
        // Test JWT token is included in headers
    }
}
```

#### KeychainService Tests
```swift
final class KeychainServiceTests: XCTestCase {
    var sut: KeychainService!

    override func setUp() {
        super.setUp()
        sut = KeychainService()
        // Clear keychain before each test
        try? sut.delete(key: "test_key")
    }

    override func tearDown() {
        try? sut.delete(key: "test_key")
        sut = nil
        super.tearDown()
    }

    func test_save_storesValueInKeychain() throws {
        // Arrange
        let key = "test_key"
        let value = "test_value"

        // Act
        try sut.save(value, forKey: key)

        // Assert
        let retrieved = try sut.retrieve(forKey: key)
        XCTAssertEqual(retrieved, value)
    }

    func test_retrieve_withNonExistentKey_throwsError() {
        // Arrange
        let key = "nonexistent_key"

        // Act & Assert
        XCTAssertThrowsError(try sut.retrieve(forKey: key))
    }

    func test_delete_removesValueFromKeychain() throws {
        // Arrange
        let key = "test_key"
        let value = "test_value"
        try sut.save(value, forKey: key)

        // Act
        try sut.delete(key: key)

        // Assert
        XCTAssertThrowsError(try sut.retrieve(forKey: key))
    }

    func test_update_modifiesExistingValue() throws {
        // Test updating existing keychain values
    }
}
```

#### SignalRManager Tests
```swift
@MainActor
final class SignalRManagerTests: XCTestCase {
    var sut: SignalRManager!

    override func setUp() {
        super.setUp()
        sut = SignalRManager.shared
    }

    override func tearDown() async throws {
        await sut.disconnect()
        sut = nil
    }

    func test_connect_withValidToken_setsConnectionStateToConnected() async throws {
        // Arrange
        let token = "valid.jwt.token"

        // Act
        try await sut.connect(withToken: token)

        // Assert
        XCTAssertEqual(sut.connectionState, .connected)
    }

    func test_disconnect_setsConnectionStateToDisconnected() async throws {
        // Arrange
        try await sut.connect(withToken: "token")

        // Act
        await sut.disconnect()

        // Assert
        XCTAssertEqual(sut.connectionState, .disconnected)
    }

    func test_transactionCreated_publishesEvent() throws {
        // Test SignalR event publishing
    }

    func test_reconnect_afterConnectionLoss() async throws {
        // Test automatic reconnection logic
    }
}
```

### Model Testing

#### Transaction Model Tests
```swift
final class TransactionTests: XCTestCase {
    func test_init_withValidData_createsTransaction() {
        // Arrange & Act
        let transaction = Transaction(
            id: UUID(),
            childId: UUID(),
            amount: 25.00,
            type: .credit,
            description: "Allowance",
            balanceAfter: 125.00,
            createdAt: Date()
        )

        // Assert
        XCTAssertEqual(transaction.amount, 25.00)
        XCTAssertEqual(transaction.type, .credit)
    }

    func test_codable_encodesAndDecodesCorrectly() throws {
        // Arrange
        let transaction = Transaction.mock()
        let encoder = JSONEncoder()
        let decoder = JSONDecoder()

        // Act
        let encoded = try encoder.encode(transaction)
        let decoded = try decoder.decode(Transaction.self, from: encoded)

        // Assert
        XCTAssertEqual(decoded.id, transaction.id)
        XCTAssertEqual(decoded.amount, transaction.amount)
    }

    func test_isCredit_returnsTrueForCreditType() {
        // Arrange
        let transaction = Transaction.mock(type: .credit)

        // Act
        let isCredit = transaction.isCredit

        // Assert
        XCTAssertTrue(isCredit)
    }

    func test_isDebit_returnsTrueForDebitType() {
        // Arrange
        let transaction = Transaction.mock(type: .debit)

        // Act
        let isDebit = transaction.isDebit

        // Assert
        XCTAssertTrue(isDebit)
    }

    func test_formattedAmount_returnsCorrectCurrencyFormat() {
        // Test currency formatting
    }
}
```

### Utility Testing

#### DateFormatter Tests
```swift
final class DateFormatterTests: XCTestCase {
    func test_relativeFormat_returnsJustNow() {
        // Arrange
        let date = Date()

        // Act
        let formatted = DateFormatter.relativeFormat(date)

        // Assert
        XCTAssertEqual(formatted, "Just now")
    }

    func test_relativeFormat_returnsMinutesAgo() {
        // Arrange
        let date = Date().addingTimeInterval(-5 * 60)

        // Act
        let formatted = DateFormatter.relativeFormat(date)

        // Assert
        XCTAssertEqual(formatted, "5m ago")
    }

    func test_relativeFormat_returnsHoursAgo() {
        // Test hours ago formatting
    }

    func test_relativeFormat_returnsDaysAgo() {
        // Test days ago formatting
    }

    func test_currencyFormat_returnsFormattedCurrency() {
        // Arrange
        let amount: Decimal = 125.50

        // Act
        let formatted = CurrencyFormatter.format(amount)

        // Assert
        XCTAssertEqual(formatted, "$125.50")
    }
}
```

#### Validation Tests
```swift
final class ValidationTests: XCTestCase {
    func test_validateEmail_withValidEmail_returnsTrue() {
        // Arrange
        let email = "test@example.com"

        // Act
        let isValid = Validator.isValidEmail(email)

        // Assert
        XCTAssertTrue(isValid)
    }

    func test_validateEmail_withInvalidEmail_returnsFalse() {
        // Arrange
        let email = "invalid.email"

        // Act
        let isValid = Validator.isValidEmail(email)

        // Assert
        XCTAssertFalse(isValid)
    }

    func test_validatePassword_withStrongPassword_returnsTrue() {
        // Arrange
        let password = "StrongPass123!"

        // Act
        let isValid = Validator.isValidPassword(password)

        // Assert
        XCTAssertTrue(isValid)
    }

    func test_validatePassword_withWeakPassword_returnsFalse() {
        // Arrange
        let password = "weak"

        // Act
        let isValid = Validator.isValidPassword(password)

        // Assert
        XCTAssertFalse(isValid)
    }

    func test_validateAmount_withPositiveAmount_returnsTrue() {
        // Test amount validation
    }

    func test_validateAmount_withNegativeAmount_returnsFalse() {
        // Test negative amount validation
    }
}
```

## 2. UI Testing (40+ Tests)

### Login Flow UI Tests
```swift
final class LoginFlowUITests: XCTestCase {
    var app: XCUIApplication!

    override func setUp() {
        super.setUp()
        continueAfterFailure = false
        app = XCUIApplication()
        app.launchArguments = ["UI-Testing"]
        app.launch()
    }

    func test_login_withValidCredentials_navigatesToDashboard() {
        // Arrange
        let emailField = app.textFields["email-field"]
        let passwordField = app.secureTextFields["password-field"]
        let loginButton = app.buttons["login-button"]

        // Act
        emailField.tap()
        emailField.typeText("test@example.com")

        passwordField.tap()
        passwordField.typeText("Password123!")

        loginButton.tap()

        // Assert
        let dashboardTitle = app.staticTexts["Dashboard"]
        XCTAssertTrue(dashboardTitle.waitForExistence(timeout: 5))
    }

    func test_login_withInvalidCredentials_showsErrorMessage() {
        // Arrange
        let emailField = app.textFields["email-field"]
        let passwordField = app.secureTextFields["password-field"]
        let loginButton = app.buttons["login-button"]

        // Act
        emailField.tap()
        emailField.typeText("wrong@example.com")

        passwordField.tap()
        passwordField.typeText("wrongpassword")

        loginButton.tap()

        // Assert
        let errorAlert = app.alerts["Error"]
        XCTAssertTrue(errorAlert.waitForExistence(timeout: 5))
    }

    func test_login_withEmptyFields_disablesLoginButton() {
        // Arrange
        let loginButton = app.buttons["login-button"]

        // Assert
        XCTAssertFalse(loginButton.isEnabled)
    }

    func test_forgotPassword_navigatesToResetScreen() {
        // Test forgot password flow
    }

    func test_register_navigatesToRegistrationScreen() {
        // Test navigation to registration
    }
}
```

### Dashboard UI Tests
```swift
final class DashboardUITests: XCTestCase {
    var app: XCUIApplication!

    override func setUp() {
        super.setUp()
        app = XCUIApplication()
        app.launchArguments = ["UI-Testing", "Authenticated"]
        app.launch()
    }

    func test_dashboard_displaysCurrentBalance() {
        // Arrange
        let balanceLabel = app.staticTexts["balance-label"]

        // Assert
        XCTAssertTrue(balanceLabel.exists)
        XCTAssertTrue(balanceLabel.label.contains("$"))
    }

    func test_dashboard_displaysRecentTransactions() {
        // Arrange
        let transactionsList = app.tables["transactions-list"]

        // Assert
        XCTAssertTrue(transactionsList.exists)
        XCTAssertGreaterThan(transactionsList.cells.count, 0)
    }

    func test_dashboard_tapTransaction_showsDetails() {
        // Arrange
        let transactionsList = app.tables["transactions-list"]
        let firstTransaction = transactionsList.cells.firstMatch

        // Act
        firstTransaction.tap()

        // Assert
        let detailView = app.otherElements["transaction-detail"]
        XCTAssertTrue(detailView.waitForExistence(timeout: 2))
    }

    func test_dashboard_pullToRefresh_reloadsData() {
        // Arrange
        let transactionsList = app.tables["transactions-list"]

        // Act
        transactionsList.swipeDown()

        // Assert
        let refreshIndicator = app.activityIndicators.firstMatch
        XCTAssertTrue(refreshIndicator.exists)
    }

    func test_dashboard_swipeTransaction_showsDeleteOption() {
        // Test swipe actions on transactions
    }
}
```

### Transaction Creation UI Tests
```swift
final class TransactionUITests: XCTestCase {
    var app: XCUIApplication!

    override func setUp() {
        super.setUp()
        app = XCUIApplication()
        app.launchArguments = ["UI-Testing", "Authenticated", "ParentRole"]
        app.launch()
    }

    func test_createTransaction_withValidData_succeeds() {
        // Arrange
        let createButton = app.buttons["create-transaction-button"]
        createButton.tap()

        let amountField = app.textFields["amount-field"]
        let descriptionField = app.textFields["description-field"]
        let saveButton = app.buttons["save-button"]

        // Act
        amountField.tap()
        amountField.typeText("25.00")

        descriptionField.tap()
        descriptionField.typeText("Weekly allowance")

        saveButton.tap()

        // Assert
        let successMessage = app.staticTexts["Transaction created"]
        XCTAssertTrue(successMessage.waitForExistence(timeout: 3))
    }

    func test_createTransaction_withInvalidAmount_showsError() {
        // Test validation error display
    }

    func test_createTransaction_selectsTransactionType() {
        // Test type selection (credit/debit)
    }

    func test_createTransaction_cancellation_dismissesSheet() {
        // Test cancel button
    }
}
```

### Navigation UI Tests
```swift
final class NavigationUITests: XCTestCase {
    var app: XCUIApplication!

    override func setUp() {
        super.setUp()
        app = XCUIApplication()
        app.launchArguments = ["UI-Testing", "Authenticated"]
        app.launch()
    }

    func test_tabBar_switchesBetweenTabs() {
        // Arrange
        let dashboardTab = app.tabBars.buttons["Dashboard"]
        let transactionsTab = app.tabBars.buttons["Transactions"]
        let settingsTab = app.tabBars.buttons["Settings"]

        // Act & Assert
        dashboardTab.tap()
        XCTAssertTrue(app.staticTexts["Dashboard"].exists)

        transactionsTab.tap()
        XCTAssertTrue(app.navigationBars["Transactions"].exists)

        settingsTab.tap()
        XCTAssertTrue(app.navigationBars["Settings"].exists)
    }

    func test_navigation_backButton_returnsToPreviousScreen() {
        // Test back navigation
    }

    func test_deepLink_navigatesToTransaction() {
        // Test deep linking from notification
    }
}
```

### Form Validation UI Tests
```swift
final class FormValidationUITests: XCTestCase {
    func test_emailField_showsErrorForInvalidEmail() {
        // Test email validation UI feedback
    }

    func test_passwordField_showsStrengthIndicator() {
        // Test password strength indicator
    }

    func test_amountField_onlyAcceptsNumbers() {
        // Test numeric keyboard and input validation
    }
}
```

### Accessibility UI Tests
```swift
final class AccessibilityUITests: XCTestCase {
    func test_allButtons_haveAccessibilityLabels() {
        // Verify accessibility labels on all interactive elements
    }

    func test_voiceOver_readsBalanceCorrectly() {
        // Test VoiceOver support
    }

    func test_dynamicType_layoutAdjustsCorrectly() {
        // Test different text sizes
    }
}
```

## 3. Integration Testing (20+ Tests)

### API Integration Tests
```swift
final class APIIntegrationTests: XCTestCase {
    var apiClient: APIClient!

    override func setUp() {
        super.setUp()
        apiClient = APIClient()
    }

    func test_loginAndFetchBalance_completeFlow() async throws {
        // Arrange
        let loginRequest = LoginRequestDTO(
            email: "test@example.com",
            password: "Password123!"
        )

        // Act
        let loginResponse: LoginResponseDTO = try await apiClient.post(
            "/api/v1/auth/login",
            body: loginRequest
        )

        apiClient.authToken = loginResponse.token

        let balance: BalanceResponseDTO = try await apiClient.get(
            "/api/v1/transactions/children/\(loginResponse.userId)/balance"
        )

        // Assert
        XCTAssertNotNil(loginResponse.token)
        XCTAssertGreaterThanOrEqual(balance.balance, 0)
    }

    func test_createTransaction_updatesBalance() async throws {
        // Test complete transaction creation flow
    }

    func test_multipleRequests_handleConcurrency() async throws {
        // Test concurrent API requests
    }
}
```

### Core Data Integration Tests
```swift
final class CoreDataIntegrationTests: XCTestCase {
    var context: NSManagedObjectContext!

    override func setUp() {
        super.setUp()
        let container = NSPersistentContainer(name: "AllowanceTracker")
        container.persistentStoreDescriptions.first?.url = URL(fileURLWithPath: "/dev/null")
        container.loadPersistentStores { _, error in
            XCTAssertNil(error)
        }
        context = container.viewContext
    }

    func test_saveTransaction_persistsToDatabase() throws {
        // Arrange
        let transaction = TransactionEntity(context: context)
        transaction.id = UUID()
        transaction.amount = NSDecimalNumber(decimal: 25.00)
        transaction.type = "Credit"

        // Act
        try context.save()

        // Assert
        let fetchRequest: NSFetchRequest<TransactionEntity> = TransactionEntity.fetchRequest()
        let results = try context.fetch(fetchRequest)
        XCTAssertEqual(results.count, 1)
    }

    func test_deleteTransaction_removesFromDatabase() throws {
        // Test deletion
    }

    func test_updateTransaction_modifiesExistingRecord() throws {
        // Test updates
    }
}
```

### SignalR Integration Tests
```swift
@MainActor
final class SignalRIntegrationTests: XCTestCase {
    var signalRManager: SignalRManager!

    override func setUp() async throws {
        signalRManager = SignalRManager.shared
        try await signalRManager.connect(withToken: "test.token")
    }

    override func tearDown() async throws {
        await signalRManager.disconnect()
    }

    func test_receiveTransactionEvent_triggersPublisher() async throws {
        // Arrange
        let expectation = XCTestExpectation(description: "Transaction event received")
        var receivedEvent: TransactionEventDTO?

        let cancellable = signalRManager.transactionCreatedPublisher.sink { event in
            receivedEvent = event
            expectation.fulfill()
        }

        // Act
        // Trigger server-side event (requires test server)

        // Assert
        await fulfillment(of: [expectation], timeout: 5.0)
        XCTAssertNotNil(receivedEvent)

        cancellable.cancel()
    }
}
```

## 4. Performance Testing

### Load Time Tests
```swift
final class PerformanceTests: XCTestCase {
    func test_dashboardLoad_completesWithinTimeLimit() {
        measure {
            let viewModel = DashboardViewModel()
            Task {
                await viewModel.loadData()
            }
        }
    }

    func test_transactionListLoad_handlesLargeDataset() {
        // Test with 1000+ transactions
    }

    func test_apiRequest_completesUnder200ms() {
        // Measure API request time
    }
}
```

## Mock Objects

### MockAPIClient
```swift
class MockAPIClient: APIClientProtocol {
    var mockResponse: Any?
    var shouldFail = false
    var error: Error?
    var lastRequestBody: Data?

    func get<T: Decodable>(_ endpoint: String) async throws -> T {
        if shouldFail, let error = error {
            throw error
        }

        guard let response = mockResponse as? T else {
            throw APIError.invalidResponse
        }

        return response
    }

    func post<T: Decodable, U: Encodable>(_ endpoint: String, body: U) async throws -> T {
        lastRequestBody = try? JSONEncoder().encode(body)

        if shouldFail, let error = error {
            throw error
        }

        guard let response = mockResponse as? T else {
            throw APIError.invalidResponse
        }

        return response
    }
}
```

### MockKeychainService
```swift
class MockKeychainService: KeychainServiceProtocol {
    var savedValues: [String: Any] = [:]

    func save(_ value: String, forKey key: String) throws {
        savedValues[key] = value
    }

    func retrieve(forKey key: String) throws -> String {
        guard let value = savedValues[key] as? String else {
            throw KeychainError.itemNotFound
        }
        return value
    }

    func delete(key: String) throws {
        savedValues.removeValue(forKey: key)
    }
}
```

## Test Coverage Requirements

| Layer | Minimum | Target | Priority |
|-------|---------|--------|----------|
| ViewModels | 90% | 95% | Critical |
| Services | 85% | 90% | Critical |
| Repositories | 80% | 85% | High |
| Models | 85% | 90% | High |
| Utilities | 75% | 85% | Medium |
| UI Tests | 70% | 80% | Medium |

## CI/CD Configuration

### Azure Pipelines for iOS
The iOS app integrates with the main Azure Pipelines configuration for comprehensive CI/CD:

**Pipeline Configuration:**
```yaml
# azure-pipelines.yml (iOS stages)
stages:
  - stage: BuildiOS
    jobs:
      - job: BuildiOS
        pool:
          vmImage: 'macOS-latest'
        steps:
          - task: Xcode@5
            inputs:
              actions: 'build'
              scheme: 'AllowanceTracker'
              sdk: 'iphoneos'
              configuration: 'Release'
              xcodeVersion: 'default'

  - stage: TestiOS
    dependsOn: BuildiOS
    jobs:
      - job: TestiOS
        pool:
          vmImage: 'macOS-latest'
        steps:
          - task: Xcode@5
            displayName: 'Run Unit Tests'
            inputs:
              actions: 'test'
              scheme: 'AllowanceTracker'
              sdk: 'iphonesimulator'
              configuration: 'Debug'
              destinationPlatformOption: 'iOS'
              destinationSimulators: 'iPhone 15'
              publishJUnitResults: true

          - task: PublishCodeCoverageResults@1
            inputs:
              codeCoverageTool: 'Cobertura'
              summaryFileLocation: '$(System.DefaultWorkingDirectory)/**/*coverage.xml'
```

**Key Features:**
- Automated builds on every commit to main/develop
- Unit and UI test execution on iOS Simulator
- Code coverage reporting integrated with Azure DevOps
- TestFlight deployment for release branches (future)

## Test Execution Commands

```bash
# Run all tests
xcodebuild test \
  -scheme AllowanceTracker \
  -destination 'platform=iOS Simulator,name=iPhone 15'

# Run unit tests only
xcodebuild test \
  -scheme AllowanceTracker \
  -destination 'platform=iOS Simulator,name=iPhone 15' \
  -only-testing:AllowanceTrackerTests

# Run UI tests only
xcodebuild test \
  -scheme AllowanceTracker \
  -destination 'platform=iOS Simulator,name=iPhone 15' \
  -only-testing:AllowanceTrackerUITests

# Run specific test class
xcodebuild test \
  -scheme AllowanceTracker \
  -destination 'platform=iOS Simulator,name=iPhone 15' \
  -only-testing:AllowanceTrackerTests/AuthViewModelTests

# Run with code coverage
xcodebuild test \
  -scheme AllowanceTracker \
  -destination 'platform=iOS Simulator,name=iPhone 15' \
  -enableCodeCoverage YES

# Generate coverage report
xcrun xccov view --report coverage.xcresult
```

## Success Metrics

### Quality Metrics
- **160+ tests passing** (100 unit + 40 UI + 20 integration)
- **Code coverage >85%** across all layers
- **Test execution time <5 minutes** for full suite
- **Zero flaky tests** (tests pass consistently)
- **All tests independent** (can run in any order)

### Performance Benchmarks
- Unit test execution: <2 seconds per test class
- UI test execution: <30 seconds per flow
- Integration test execution: <10 seconds per test

## Summary

This comprehensive testing strategy provides:
- **100+ Unit Tests**: ViewModels, Services, Repositories, Models, Utilities
- **40+ UI Tests**: Login flows, navigation, forms, accessibility
- **20+ Integration Tests**: API integration, Core Data, SignalR
- **Performance Tests**: Load times, large datasets, API response times
- **Mock Objects**: Complete test doubles for all external dependencies
- **CI/CD Integration**: Automated testing on every commit
- **TDD Workflow**: Red-Green-Refactor methodology enforced

Total test count: **160+ tests** covering all critical paths with >85% code coverage target.

All tests follow strict TDD principles with failing tests written before implementation code.
