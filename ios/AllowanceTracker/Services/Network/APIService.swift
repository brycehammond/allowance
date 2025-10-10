import Foundation

/// Main API service for backend communication
final class APIService: APIServiceProtocol {

    // MARK: - Properties

    private let baseURL: URL
    private let urlSession: URLSessionProtocol
    private let keychainService: KeychainServiceProtocol

    private let jsonEncoder: JSONEncoder = {
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        return encoder
    }()

    private let jsonDecoder: JSONDecoder = {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return decoder
    }()

    // MARK: - Initialization

    /// Initialize APIService with custom dependencies (for testing)
    init(
        baseURL: URL,
        urlSession: URLSessionProtocol = URLSession.shared,
        keychainService: KeychainServiceProtocol = KeychainService.shared
    ) {
        self.baseURL = baseURL
        self.urlSession = urlSession
        self.keychainService = keychainService
    }

    /// Convenience initializer with default production configuration
    convenience init() {
        // TODO: Load from configuration file or environment
        let defaultURL = URL(string: "https://api.allowancetracker.com")!
        self.init(baseURL: defaultURL)
    }

    // MARK: - Authentication

    /// Login with email and password
    /// - Parameter request: Login credentials
    /// - Returns: Authentication response with user and token
    /// - Throws: APIError if login fails
    func login(_ request: LoginRequest) async throws -> AuthResponse {
        let endpoint = baseURL.appendingPathComponent("/api/v1/auth/login")

        var urlRequest = URLRequest(url: endpoint)
        urlRequest.httpMethod = "POST"
        urlRequest.setValue("application/json", forHTTPHeaderField: "Content-Type")
        urlRequest.httpBody = try jsonEncoder.encode(request)

        let response: AuthResponse = try await performRequest(urlRequest)

        // Save token to keychain
        try keychainService.saveToken(response.token)

        return response
    }

    /// Register a new user
    /// - Parameter request: Registration data
    /// - Returns: Authentication response with user and token
    /// - Throws: APIError if registration fails
    func register(_ request: RegisterRequest) async throws -> AuthResponse {
        let endpoint = baseURL.appendingPathComponent("/api/v1/auth/register")

        var urlRequest = URLRequest(url: endpoint)
        urlRequest.httpMethod = "POST"
        urlRequest.setValue("application/json", forHTTPHeaderField: "Content-Type")
        urlRequest.httpBody = try jsonEncoder.encode(request)

        let response: AuthResponse = try await performRequest(urlRequest)

        // Save token to keychain
        try keychainService.saveToken(response.token)

        return response
    }

    /// Logout current user
    /// - Throws: APIError if logout fails
    func logout() async throws {
        try keychainService.deleteToken()
    }

    // MARK: - Children

    /// Get all children for the current family
    /// - Returns: Array of children
    /// - Throws: APIError if request fails
    func getChildren() async throws -> [Child] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get a specific child by ID
    /// - Parameter id: Child's unique identifier
    /// - Returns: Child object
    /// - Throws: APIError if request fails
    func getChild(id: UUID) async throws -> Child {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(id.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    // MARK: - Private Helpers

    /// Create an authenticated request with JWT token
    /// - Parameters:
    ///   - url: Request URL
    ///   - method: HTTP method
    ///   - body: Optional request body
    /// - Returns: URLRequest with authorization header
    /// - Throws: APIError.unauthorized if token not found
    private func createAuthenticatedRequest(
        url: URL,
        method: String,
        body: Data? = nil
    ) async throws -> URLRequest {
        // Get token from keychain
        guard let token = try? keychainService.getToken() else {
            throw APIError.unauthorized
        }

        var request = URLRequest(url: url)
        request.httpMethod = method
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")

        if let body = body {
            request.httpBody = body
        }

        return request
    }

    /// Perform network request and decode response
    /// - Parameter request: URLRequest to perform
    /// - Returns: Decoded response object
    /// - Throws: APIError if request fails
    private func performRequest<T: Decodable>(_ request: URLRequest) async throws -> T {
        do {
            let (data, response) = try await urlSession.data(for: request)

            // Validate HTTP response
            guard let httpResponse = response as? HTTPURLResponse else {
                throw APIError.invalidResponse
            }

            // Handle HTTP status codes
            switch httpResponse.statusCode {
            case 200...299:
                // Success - decode response
                do {
                    return try jsonDecoder.decode(T.self, from: data)
                } catch {
                    throw APIError.decodingError
                }

            case 401:
                throw APIError.unauthorized

            case 404:
                throw APIError.notFound

            case 409:
                throw APIError.conflict

            case 500...599:
                throw APIError.serverError

            default:
                throw APIError.unknown
            }

        } catch let error as APIError {
            // Re-throw API errors
            throw error
        } catch is URLError {
            // Network errors
            throw APIError.networkError
        } catch {
            // Unknown errors
            throw APIError.unknown
        }
    }
}
