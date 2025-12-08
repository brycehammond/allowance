import XCTest
@testable import AllowanceTracker

final class APIServiceTests: XCTestCase {

    var sut: APIService!
    var mockURLSession: MockURLSession!
    var mockKeychainService: MockKeychainService!

    override func setUp() {
        super.setUp()
        mockURLSession = MockURLSession()
        mockKeychainService = MockKeychainService()
        sut = APIService(
            baseURL: URL(string: "https://api.test.com")!,
            urlSession: mockURLSession,
            keychainService: mockKeychainService
        )
    }

    override func tearDown() {
        sut = nil
        mockURLSession = nil
        mockKeychainService = nil
        super.tearDown()
    }

    // MARK: - Authentication Tests

    func testLogin_WithValidCredentials_ReturnsAuthResponse() async throws {
        // Arrange
        let loginRequest = LoginRequest(email: "test@example.com", password: "password123")
        let expectedResponse = AuthResponse(
            userId: UUID(),
            email: "test@example.com",
            firstName: "John",
            lastName: "Doe",
            role: "Parent",
            familyId: nil,
            familyName: nil,
            token: "valid-jwt-token",
            expiresAt: Date().addingTimeInterval(86400)
        )
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        mockURLSession.mockData = try encoder.encode(expectedResponse)
        mockURLSession.mockResponse = HTTPURLResponse(
            url: URL(string: "https://api.test.com")!,
            statusCode: 200,
            httpVersion: nil,
            headerFields: nil
        )

        // Act
        let response = try await sut.login(loginRequest)

        // Assert
        XCTAssertEqual(response.token, expectedResponse.token)
        XCTAssertEqual(response.user.email, expectedResponse.user.email)
        XCTAssertEqual(mockKeychainService.savedToken, "valid-jwt-token")
    }

    func testLogin_WithInvalidCredentials_ThrowsUnauthorizedError() async throws {
        // Arrange
        let loginRequest = LoginRequest(email: "test@example.com", password: "wrong")
        mockURLSession.mockData = Data() // Empty data for error response
        mockURLSession.mockResponse = HTTPURLResponse(
            url: URL(string: "https://api.test.com")!,
            statusCode: 401,
            httpVersion: nil,
            headerFields: nil
        )

        // Act & Assert
        do {
            _ = try await sut.login(loginRequest)
            XCTFail("Expected unauthorized error")
        } catch let error as APIError {
            XCTAssertEqual(error, .unauthorized)
        }
    }

    func testRegister_WithValidData_ReturnsAuthResponse() async throws {
        // Arrange
        let registerRequest = RegisterRequest(
            email: "new@example.com",
            password: "password123",
            firstName: "Jane",
            lastName: "Smith",
            role: .parent
        )
        let expectedResponse = AuthResponse(
            userId: UUID(),
            email: "new@example.com",
            firstName: "Jane",
            lastName: "Smith",
            role: "Parent",
            familyId: nil,
            familyName: nil,
            token: "new-user-token",
            expiresAt: Date().addingTimeInterval(86400)
        )
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        mockURLSession.mockData = try encoder.encode(expectedResponse)
        mockURLSession.mockResponse = HTTPURLResponse(
            url: URL(string: "https://api.test.com")!,
            statusCode: 201,
            httpVersion: nil,
            headerFields: nil
        )

        // Act
        let response = try await sut.register(registerRequest)

        // Assert
        XCTAssertEqual(response.token, expectedResponse.token)
        XCTAssertEqual(response.user.email, "new@example.com")
        XCTAssertEqual(mockKeychainService.savedToken, "new-user-token")
    }

    func testRegister_WithExistingEmail_ThrowsConflictError() async throws {
        // Arrange
        let registerRequest = RegisterRequest(
            email: "existing@example.com",
            password: "password123",
            firstName: "Test",
            lastName: "User",
            role: .parent
        )
        mockURLSession.mockData = Data() // Empty data for error response
        mockURLSession.mockResponse = HTTPURLResponse(
            url: URL(string: "https://api.test.com")!,
            statusCode: 409,
            httpVersion: nil,
            headerFields: nil
        )

        // Act & Assert
        do {
            _ = try await sut.register(registerRequest)
            XCTFail("Expected conflict error")
        } catch let error as APIError {
            XCTAssertEqual(error, .conflict)
        }
    }

    // MARK: - Authenticated Request Tests

    func testAuthenticatedRequest_IncludesJWTToken() async throws {
        // Arrange
        mockKeychainService.tokenToReturn = "stored-jwt-token"
        let expectedChildren = [
            Child(
                id: UUID(),
                firstName: "Alice",
                lastName: "Johnson",
                weeklyAllowance: 10.0,
                currentBalance: 50.0,
                lastAllowanceDate: nil
            )
        ]
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        mockURLSession.mockData = try encoder.encode(expectedChildren)
        mockURLSession.mockResponse = HTTPURLResponse(
            url: URL(string: "https://api.test.com")!,
            statusCode: 200,
            httpVersion: nil,
            headerFields: nil
        )

        // Act
        let children: [Child] = try await sut.getChildren()

        // Assert
        XCTAssertEqual(children.count, 1)
        XCTAssertEqual(children[0].firstName, "Alice")
        XCTAssertTrue(mockURLSession.lastRequest?.allHTTPHeaderFields?["Authorization"]?.hasPrefix("Bearer ") == true)
    }

    func testAuthenticatedRequest_WithoutToken_ThrowsUnauthorizedError() async throws {
        // Arrange
        mockKeychainService.shouldThrowError = true

        // Act & Assert
        do {
            let _: [Child] = try await sut.getChildren()
            XCTFail("Expected unauthorized error")
        } catch let error as APIError {
            XCTAssertEqual(error, .unauthorized)
        }
    }

    // MARK: - Network Error Tests

    func testRequest_WithNetworkError_ThrowsNetworkError() async throws {
        // Arrange
        let loginRequest = LoginRequest(email: "test@example.com", password: "password")
        mockURLSession.mockError = URLError(.notConnectedToInternet)

        // Act & Assert
        do {
            _ = try await sut.login(loginRequest)
            XCTFail("Expected network error")
        } catch let error as APIError {
            XCTAssertEqual(error, .networkError)
        }
    }

    func testRequest_WithInvalidJSON_ThrowsDecodingError() async throws {
        // Arrange
        let loginRequest = LoginRequest(email: "test@example.com", password: "password")
        mockURLSession.mockData = "Invalid JSON".data(using: .utf8)
        mockURLSession.mockResponse = HTTPURLResponse(
            url: URL(string: "https://api.test.com")!,
            statusCode: 200,
            httpVersion: nil,
            headerFields: nil
        )

        // Act & Assert
        do {
            _ = try await sut.login(loginRequest)
            XCTFail("Expected decoding error")
        } catch let error as APIError {
            XCTAssertEqual(error, .decodingError)
        }
    }

    func testRequest_WithServerError_ThrowsServerError() async throws {
        // Arrange
        let loginRequest = LoginRequest(email: "test@example.com", password: "password")
        mockURLSession.mockData = Data() // Empty data for error response
        mockURLSession.mockResponse = HTTPURLResponse(
            url: URL(string: "https://api.test.com")!,
            statusCode: 500,
            httpVersion: nil,
            headerFields: nil
        )

        // Act & Assert
        do {
            _ = try await sut.login(loginRequest)
            XCTFail("Expected server error")
        } catch let error as APIError {
            XCTAssertEqual(error, .serverError)
        }
    }

    // MARK: - Logout Tests

    func testLogout_RemovesTokenFromKeychain() async throws {
        // Arrange
        mockKeychainService.tokenToReturn = "token-to-remove"

        // Act
        try await sut.logout()

        // Assert
        XCTAssertTrue(mockKeychainService.didDeleteToken)
    }
}

// MARK: - Mock Classes

class MockURLSession: URLSessionProtocol {
    var mockData: Data?
    var mockResponse: URLResponse?
    var mockError: Error?
    var lastRequest: URLRequest?

    func data(for request: URLRequest) async throws -> (Data, URLResponse) {
        lastRequest = request

        if let error = mockError {
            throw error
        }

        guard let data = mockData, let response = mockResponse else {
            throw URLError(.badServerResponse)
        }

        return (data, response)
    }
}

class MockKeychainService: KeychainServiceProtocol {
    var savedToken: String?
    var tokenToReturn: String?
    var shouldThrowError = false
    var didDeleteToken = false

    func saveToken(_ token: String) throws {
        if shouldThrowError {
            throw KeychainError.unableToSave
        }
        savedToken = token
    }

    func getToken() throws -> String {
        if shouldThrowError {
            throw KeychainError.notFound
        }
        guard let token = tokenToReturn else {
            throw KeychainError.notFound
        }
        return token
    }

    func deleteToken() throws {
        if shouldThrowError {
            throw KeychainError.unableToDelete
        }
        didDeleteToken = true
        savedToken = nil
        tokenToReturn = nil
    }
}
