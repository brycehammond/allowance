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
        let expectedUser = User(
            id: UUID(),
            email: "test@example.com",
            firstName: "John",
            lastName: "Doe",
            role: .parent,
            familyId: nil
        )
        mockAPIService.loginResult = .success(AuthResponse(
            token: "valid-token",
            expiresAt: Date().addingTimeInterval(86400),
            user: expectedUser
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
            token: "token",
            expiresAt: Date().addingTimeInterval(86400),
            user: User(id: UUID(), email: "test@example.com", firstName: "Test", lastName: "User", role: .parent, familyId: nil)
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
        let expectedUser = User(
            id: UUID(),
            email: "new@example.com",
            firstName: "Jane",
            lastName: "Smith",
            role: .parent,
            familyId: nil
        )
        mockAPIService.registerResult = .success(AuthResponse(
            token: "new-token",
            expiresAt: Date().addingTimeInterval(86400),
            user: expectedUser
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
            token: "token",
            expiresAt: Date().addingTimeInterval(86400),
            user: User(id: UUID(), email: "test@example.com", firstName: "Test", lastName: "User", role: .parent, familyId: nil)
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

    func getChildren() async throws -> [Child] {
        return []
    }

    func getChild(id: UUID) async throws -> Child {
        throw APIError.notFound
    }
}
