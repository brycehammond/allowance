import Foundation

/// API client for UI test setup and teardown operations.
/// Makes direct HTTP calls to create and delete test accounts without going through the app UI.
class UITestAPIClient {

    // MARK: - Configuration

    /// Base URL for the API - defaults to local development server
    /// Can be overridden via TEST_API_BASE_URL environment variable
    static var baseURL: URL {
        if let urlString = ProcessInfo.processInfo.environment["TEST_API_BASE_URL"],
           let url = URL(string: urlString) {
            return url
        }
        // Default to local development server
        return URL(string: "http://localhost:5000")!
    }

    /// API key for test operations - required for deleting test accounts
    /// Set via TEST_API_KEY environment variable
    static var testApiKey: String {
        ProcessInfo.processInfo.environment["TEST_API_KEY"] ?? "test-api-key-12345"
    }

    // MARK: - Types

    struct TestAccount {
        let email: String
        let password: String
        let firstName: String
        let lastName: String
        let familyName: String
        let userId: UUID?
        let token: String?

        init(
            email: String,
            password: String = "TestPass123!",
            firstName: String = "Test",
            lastName: String = "User",
            familyName: String = "Test Family"
        ) {
            self.email = email
            self.password = password
            self.firstName = firstName
            self.lastName = lastName
            self.familyName = familyName
            self.userId = nil
            self.token = nil
        }

        init(
            email: String,
            password: String,
            firstName: String,
            lastName: String,
            familyName: String,
            userId: UUID?,
            token: String?
        ) {
            self.email = email
            self.password = password
            self.firstName = firstName
            self.lastName = lastName
            self.familyName = familyName
            self.userId = userId
            self.token = token
        }
    }

    struct AuthResponse: Codable {
        let userId: UUID
        let email: String
        let firstName: String
        let lastName: String
        let role: String
        let familyId: UUID?
        let familyName: String?
        let token: String
        let expiresAt: String
    }

    struct ErrorResponse: Codable {
        let error: ErrorDetail

        struct ErrorDetail: Codable {
            let code: String
            let message: String
        }
    }

    enum APIError: Error, LocalizedError {
        case invalidURL
        case requestFailed(statusCode: Int, message: String)
        case decodingError(Error)
        case networkError(Error)
        case serverError(String)

        var errorDescription: String? {
            switch self {
            case .invalidURL:
                return "Invalid URL"
            case .requestFailed(let statusCode, let message):
                return "Request failed with status \(statusCode): \(message)"
            case .decodingError(let error):
                return "Decoding error: \(error.localizedDescription)"
            case .networkError(let error):
                return "Network error: \(error.localizedDescription)"
            case .serverError(let message):
                return "Server error: \(message)"
            }
        }
    }

    // MARK: - Singleton

    static let shared = UITestAPIClient()

    private let session: URLSession

    private init() {
        let config = URLSessionConfiguration.default
        config.timeoutIntervalForRequest = 30
        config.timeoutIntervalForResource = 60
        self.session = URLSession(configuration: config)
    }

    // MARK: - Public Methods

    /// Create a new test parent account
    /// - Parameters:
    ///   - email: Unique email for the test account
    ///   - password: Password for the account (default: "TestPass123!")
    ///   - firstName: First name (default: "Test")
    ///   - lastName: Last name (default: "User")
    ///   - familyName: Family name (default: "Test Family")
    /// - Returns: TestAccount with userId and token populated
    func createTestParentAccount(
        email: String,
        password: String = "TestPass123!",
        firstName: String = "Test",
        lastName: String = "User",
        familyName: String = "Test Family"
    ) async throws -> TestAccount {
        let url = Self.baseURL.appendingPathComponent("api/v1/auth/register/parent")

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        let body: [String: Any] = [
            "email": email,
            "password": password,
            "firstName": firstName,
            "lastName": lastName,
            "familyName": familyName
        ]
        request.httpBody = try JSONSerialization.data(withJSONObject: body)

        let (data, response) = try await session.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.networkError(NSError(domain: "Invalid response", code: -1))
        }

        if httpResponse.statusCode == 201 || httpResponse.statusCode == 200 {
            let decoder = JSONDecoder()
            let authResponse = try decoder.decode(AuthResponse.self, from: data)

            return TestAccount(
                email: email,
                password: password,
                firstName: firstName,
                lastName: lastName,
                familyName: familyName,
                userId: authResponse.userId,
                token: authResponse.token
            )
        } else {
            let errorMessage = parseErrorMessage(from: data) ?? "Unknown error"
            throw APIError.requestFailed(statusCode: httpResponse.statusCode, message: errorMessage)
        }
    }

    /// Delete a test account by email
    /// - Parameter email: Email of the account to delete
    /// - Note: Requires TEST_API_KEY to be configured
    func deleteTestAccount(email: String) async throws {
        // URL encode the email
        guard let encodedEmail = email.addingPercentEncoding(withAllowedCharacters: .urlPathAllowed) else {
            throw APIError.invalidURL
        }

        let url = Self.baseURL.appendingPathComponent("api/v1/auth/test-account/\(encodedEmail)")

        var request = URLRequest(url: url)
        request.httpMethod = "DELETE"
        request.setValue(Self.testApiKey, forHTTPHeaderField: "X-Test-Api-Key")

        let (data, response) = try await session.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.networkError(NSError(domain: "Invalid response", code: -1))
        }

        // 200 = deleted, 404 = account doesn't exist (ok for cleanup)
        if httpResponse.statusCode == 200 || httpResponse.statusCode == 404 {
            return
        } else {
            let errorMessage = parseErrorMessage(from: data) ?? "Unknown error"
            throw APIError.requestFailed(statusCode: httpResponse.statusCode, message: errorMessage)
        }
    }

    /// Login with existing credentials
    /// - Parameters:
    ///   - email: Account email
    ///   - password: Account password
    /// - Returns: Auth token
    func login(email: String, password: String) async throws -> String {
        let url = Self.baseURL.appendingPathComponent("api/v1/auth/login")

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        let body: [String: Any] = [
            "email": email,
            "password": password,
            "rememberMe": false
        ]
        request.httpBody = try JSONSerialization.data(withJSONObject: body)

        let (data, response) = try await session.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.networkError(NSError(domain: "Invalid response", code: -1))
        }

        if httpResponse.statusCode == 200 {
            let decoder = JSONDecoder()
            let authResponse = try decoder.decode(AuthResponse.self, from: data)
            return authResponse.token
        } else {
            let errorMessage = parseErrorMessage(from: data) ?? "Unknown error"
            throw APIError.requestFailed(statusCode: httpResponse.statusCode, message: errorMessage)
        }
    }

    /// Check if the API is reachable
    /// - Returns: true if API is reachable
    func isAPIReachable() async -> Bool {
        // Try to hit the health endpoint or login endpoint
        let url = Self.baseURL.appendingPathComponent("api/v1/auth/login")

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try? JSONSerialization.data(withJSONObject: ["email": "", "password": ""])
        request.timeoutInterval = 5

        do {
            let (_, response) = try await session.data(for: request)
            guard let httpResponse = response as? HTTPURLResponse else {
                return false
            }
            // Any response (even 400/401) means API is reachable
            return httpResponse.statusCode > 0
        } catch {
            return false
        }
    }

    // MARK: - Private Helpers

    private func parseErrorMessage(from data: Data) -> String? {
        if let errorResponse = try? JSONDecoder().decode(ErrorResponse.self, from: data) {
            return errorResponse.error.message
        }
        return String(data: data, encoding: .utf8)
    }
}

// MARK: - Test Account Generation

extension UITestAPIClient {

    /// Generate a unique test email for this test run
    /// - Parameter prefix: Optional prefix for the email (default: "uitest")
    /// - Returns: Unique email like "uitest_1705612345_abc123@test.local"
    static func generateTestEmail(prefix: String = "uitest") -> String {
        let timestamp = Int(Date().timeIntervalSince1970)
        let random = String(UUID().uuidString.prefix(6)).lowercased()
        return "\(prefix)_\(timestamp)_\(random)@test.local"
    }
}
