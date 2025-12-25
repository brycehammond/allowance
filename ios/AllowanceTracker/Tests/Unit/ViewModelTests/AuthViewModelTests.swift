import XCTest
@testable import AllowanceTracker

@MainActor
final class AuthViewModelTests: XCTestCase {

    var sut: AuthViewModel!
    var mockAPIService: MockAPIService!

    override func setUp() {
        super.setUp()
        mockAPIService = MockAPIService()
        sut = AuthViewModel(apiService: mockAPIService)
    }

    override func tearDown() {
        sut = nil
        mockAPIService = nil
        super.tearDown()
    }

    // MARK: - Login Tests

    func testLogin_WithValidCredentials_SetsUserAndAuthenticationState() async throws {
        // Arrange
        let userId = UUID()
        mockAPIService.loginResult = .success(AuthResponse(
            userId: userId,
            email: "test@example.com",
            firstName: "John",
            lastName: "Doe",
            role: "Parent",
            familyId: nil,
            familyName: nil,
            token: "valid-token",
            expiresAt: Date().addingTimeInterval(86400)
        ))

        // Act
        await sut.login(email: "test@example.com", password: "password123")

        // Assert
        XCTAssertTrue(sut.isAuthenticated)
        XCTAssertNotNil(sut.currentUser)
        XCTAssertEqual(sut.currentUser?.email, "test@example.com")
        XCTAssertNil(sut.errorMessage)
        XCTAssertFalse(sut.isLoading)
    }

    func testLogin_WithInvalidCredentials_ShowsErrorMessage() async throws {
        // Arrange
        mockAPIService.loginResult = .failure(.unauthorized)

        // Act
        await sut.login(email: "test@example.com", password: "wrong")

        // Assert
        XCTAssertFalse(sut.isAuthenticated)
        XCTAssertNil(sut.currentUser)
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertEqual(sut.errorMessage, "Authentication failed. Please check your credentials.")
        XCTAssertFalse(sut.isLoading)
    }

    func testLogin_SetsLoadingStateDuringExecution() async throws {
        // Arrange
        mockAPIService.loginResult = .success(AuthResponse(
            userId: UUID(),
            email: "test@example.com",
            firstName: "Test",
            lastName: "User",
            role: "Parent",
            familyId: nil,
            familyName: nil,
            token: "token",
            expiresAt: Date().addingTimeInterval(86400)
        ))
        mockAPIService.shouldDelay = true

        // Act
        let task = Task {
            await sut.login(email: "test@example.com", password: "password")
        }

        // Assert - should be loading
        try await Task.sleep(nanoseconds: 50_000_000) // 50ms
        XCTAssertTrue(sut.isLoading)

        await task.value

        // Assert - should not be loading after completion
        XCTAssertFalse(sut.isLoading)
    }

    func testLogin_WithEmptyEmail_ShowsValidationError() async throws {
        // Act
        await sut.login(email: "", password: "password123")

        // Assert
        XCTAssertFalse(sut.isAuthenticated)
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertEqual(sut.errorMessage, "Please enter a valid email address.")
    }

    func testLogin_WithEmptyPassword_ShowsValidationError() async throws {
        // Act
        await sut.login(email: "test@example.com", password: "")

        // Assert
        XCTAssertFalse(sut.isAuthenticated)
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertEqual(sut.errorMessage, "Please enter your password.")
    }

    // MARK: - Register Tests

    func testRegister_WithValidData_SetsUserAndAuthenticationState() async throws {
        // Arrange
        mockAPIService.registerResult = .success(AuthResponse(
            userId: UUID(),
            email: "new@example.com",
            firstName: "Jane",
            lastName: "Smith",
            role: "Parent",
            familyId: nil,
            familyName: nil,
            token: "new-token",
            expiresAt: Date().addingTimeInterval(86400)
        ))

        // Act
        await sut.register(
            email: "new@example.com",
            password: "password123",
            firstName: "Jane",
            lastName: "Smith",
            role: .parent
        )

        // Assert
        XCTAssertTrue(sut.isAuthenticated)
        XCTAssertNotNil(sut.currentUser)
        XCTAssertEqual(sut.currentUser?.email, "new@example.com")
        XCTAssertNil(sut.errorMessage)
        XCTAssertFalse(sut.isLoading)
    }

    func testRegister_WithExistingEmail_ShowsConflictError() async throws {
        // Arrange
        mockAPIService.registerResult = .failure(.conflict)

        // Act
        await sut.register(
            email: "existing@example.com",
            password: "password123",
            firstName: "Test",
            lastName: "User",
            role: .parent
        )

        // Assert
        XCTAssertFalse(sut.isAuthenticated)
        XCTAssertNil(sut.currentUser)
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertEqual(sut.errorMessage, "A conflict occurred. This resource may already exist.")
    }

    func testRegister_WithInvalidEmail_ShowsValidationError() async throws {
        // Act
        await sut.register(
            email: "invalid-email",
            password: "password123",
            firstName: "Test",
            lastName: "User",
            role: .parent
        )

        // Assert
        XCTAssertFalse(sut.isAuthenticated)
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertEqual(sut.errorMessage, "Please enter a valid email address.")
    }

    func testRegister_WithShortPassword_ShowsValidationError() async throws {
        // Act
        await sut.register(
            email: "test@example.com",
            password: "123",
            firstName: "Test",
            lastName: "User",
            role: .parent
        )

        // Assert
        XCTAssertFalse(sut.isAuthenticated)
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertEqual(sut.errorMessage, "Password must be at least 6 characters long.")
    }

    // MARK: - Logout Tests

    func testLogout_ClearsUserAndAuthenticationState() async throws {
        // Arrange - first login
        mockAPIService.loginResult = .success(AuthResponse(
            userId: UUID(),
            email: "test@example.com",
            firstName: "Test",
            lastName: "User",
            role: "Parent",
            familyId: nil,
            familyName: nil,
            token: "token",
            expiresAt: Date().addingTimeInterval(86400)
        ))
        await sut.login(email: "test@example.com", password: "password")

        XCTAssertTrue(sut.isAuthenticated)
        XCTAssertNotNil(sut.currentUser)

        // Act
        await sut.logout()

        // Assert
        XCTAssertFalse(sut.isAuthenticated)
        XCTAssertNil(sut.currentUser)
        XCTAssertNil(sut.errorMessage)
    }

    // MARK: - Error Message Tests

    func testClearError_RemovesErrorMessage() async throws {
        // Arrange
        mockAPIService.loginResult = .failure(.unauthorized)
        await sut.login(email: "test@example.com", password: "wrong")

        XCTAssertNotNil(sut.errorMessage)

        // Act
        sut.clearError()

        // Assert
        XCTAssertNil(sut.errorMessage)
    }

    // MARK: - Network Error Tests

    func testLogin_WithNetworkError_ShowsUserFriendlyMessage() async throws {
        // Arrange
        mockAPIService.loginResult = .failure(.networkError)

        // Act
        await sut.login(email: "test@example.com", password: "password")

        // Assert
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertEqual(sut.errorMessage, "Network connection failed. Please check your internet connection.")
    }
}

// MARK: - Mock API Service

class MockAPIService: APIServiceProtocol {
    var loginResult: Result<AuthResponse, APIError>?
    var registerResult: Result<AuthResponse, APIError>?
    var changePasswordResult: Result<PasswordMessageResponse, APIError>?
    var forgotPasswordResult: Result<PasswordMessageResponse, APIError>?
    var resetPasswordResult: Result<PasswordMessageResponse, APIError>?
    var childrenResponse: Result<[Child], Error> = .success([])
    var transactionsResponse: Result<[Transaction], Error>?
    var createTransactionResponse: Result<Transaction, Error>?
    var wishListResponse: Result<[WishListItem], Error> = .success([])
    var createWishListItemResponse: Result<WishListItem, Error>?
    var markPurchasedResponse: Result<WishListItem, Error>?
    var deleteWishListItemResponse: Result<Void, Error>?
    var shouldDelay = false

    func login(_ request: LoginRequest) async throws -> AuthResponse {
        if shouldDelay {
            try await Task.sleep(nanoseconds: 100_000_000) // 100ms
        }

        switch loginResult {
        case .success(let response):
            return response
        case .failure(let error):
            throw error
        case .none:
            throw APIError.unknown
        }
    }

    func register(_ request: RegisterRequest) async throws -> AuthResponse {
        if shouldDelay {
            try await Task.sleep(nanoseconds: 100_000_000) // 100ms
        }

        switch registerResult {
        case .success(let response):
            return response
        case .failure(let error):
            throw error
        case .none:
            throw APIError.unknown
        }
    }

    func logout() async throws {
        // Mock implementation - do nothing
    }

    var refreshTokenResult: Result<AuthResponse, APIError>?

    func refreshToken() async throws -> AuthResponse {
        switch refreshTokenResult {
        case .success(let response):
            return response
        case .failure(let error):
            throw error
        case .none:
            throw APIError.unauthorized
        }
    }

    func changePassword(_ request: ChangePasswordRequest) async throws -> PasswordMessageResponse {
        switch changePasswordResult {
        case .success(let response):
            return response
        case .failure(let error):
            throw error
        case .none:
            return PasswordMessageResponse(message: "Password changed")
        }
    }

    func forgotPassword(_ request: ForgotPasswordRequest) async throws -> PasswordMessageResponse {
        switch forgotPasswordResult {
        case .success(let response):
            return response
        case .failure(let error):
            throw error
        case .none:
            return PasswordMessageResponse(message: "Reset email sent")
        }
    }

    func resetPassword(_ request: ResetPasswordRequest) async throws -> PasswordMessageResponse {
        switch resetPasswordResult {
        case .success(let response):
            return response
        case .failure(let error):
            throw error
        case .none:
            return PasswordMessageResponse(message: "Password reset successful")
        }
    }

    func getChildren() async throws -> [Child] {
        switch childrenResponse {
        case .success(let children):
            return children
        case .failure(let error):
            throw error
        }
    }

    func getChild(id: UUID) async throws -> Child {
        throw APIError.notFound
    }

    func createChild(_ request: CreateChildRequest) async throws -> Child {
        throw APIError.notFound
    }

    func updateChildSettings(childId: UUID, _ request: UpdateChildSettingsRequest) async throws -> UpdateChildSettingsResponse {
        throw APIError.notFound
    }

    // MARK: - Savings (stub implementations)

    func getSavingsSummary(forChild childId: UUID) async throws -> SavingsAccountSummary {
        throw APIError.notFound
    }

    func getSavingsHistory(forChild childId: UUID, limit: Int) async throws -> [SavingsTransaction] {
        return []
    }

    func depositToSavings(_ request: DepositToSavingsRequest) async throws -> SavingsTransaction {
        throw APIError.notFound
    }

    func withdrawFromSavings(_ request: WithdrawFromSavingsRequest) async throws -> SavingsTransaction {
        throw APIError.notFound
    }

    // MARK: - Parent Invites (stub implementations)

    func sendParentInvite(_ request: SendParentInviteRequest) async throws -> ParentInviteResponse {
        throw APIError.notFound
    }

    func getPendingInvites() async throws -> [PendingInvite] {
        return []
    }

    func cancelInvite(inviteId: String) async throws {
        throw APIError.notFound
    }

    func resendInvite(inviteId: String) async throws -> ParentInviteResponse {
        throw APIError.notFound
    }

    // MARK: - Transactions

    func getTransactions(forChild childId: UUID, limit: Int) async throws -> [Transaction] {
        switch transactionsResponse {
        case .success(let transactions):
            return transactions
        case .failure(let error):
            throw error
        case .none:
            return []
        }
    }

    func createTransaction(_ request: CreateTransactionRequest) async throws -> Transaction {
        switch createTransactionResponse {
        case .success(let transaction):
            return transaction
        case .failure(let error):
            throw error
        case .none:
            throw APIError.notFound
        }
    }

    func getBalance(forChild childId: UUID) async throws -> Decimal {
        return 0
    }

    // MARK: - Wish List

    func getWishList(forChild childId: UUID) async throws -> [WishListItem] {
        switch wishListResponse {
        case .success(let items):
            return items
        case .failure(let error):
            throw error
        }
    }

    func createWishListItem(_ request: CreateWishListItemRequest) async throws -> WishListItem {
        switch createWishListItemResponse {
        case .success(let item):
            return item
        case .failure(let error):
            throw error
        case .none:
            throw APIError.notFound
        }
    }

    func updateWishListItem(forChild childId: UUID, id: UUID, _ request: UpdateWishListItemRequest) async throws -> WishListItem {
        throw APIError.notFound
    }

    func deleteWishListItem(forChild childId: UUID, id: UUID) async throws {
        switch deleteWishListItemResponse {
        case .success:
            return
        case .failure(let error):
            throw error
        case .none:
            throw APIError.notFound
        }
    }

    func markWishListItemAsPurchased(forChild childId: UUID, id: UUID) async throws -> WishListItem {
        switch markPurchasedResponse {
        case .success(let item):
            return item
        case .failure(let error):
            throw error
        case .none:
            throw APIError.notFound
        }
    }

    // MARK: - Analytics (stub implementations)

    func getBalanceHistory(forChild childId: UUID, days: Int) async throws -> [BalancePoint] {
        return []
    }

    func getIncomeVsSpending(forChild childId: UUID) async throws -> IncomeSpendingSummary {
        return IncomeSpendingSummary(totalIncome: 0, totalSpending: 0, netSavings: 0, incomeTransactionCount: 0, spendingTransactionCount: 0, savingsRate: 0)
    }

    func getSpendingBreakdown(forChild childId: UUID) async throws -> [CategoryBreakdown] {
        return []
    }

    func getMonthlyComparison(forChild childId: UUID, months: Int) async throws -> [MonthlyComparison] {
        return []
    }
}
