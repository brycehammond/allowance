import Foundation

/// Main API service for backend communication
final class APIService: APIServiceProtocol, @unchecked Sendable {

    // MARK: - Shared Instance

    static let shared = APIService()

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
        // Use custom date formatter to handle various ISO 8601 formats
        decoder.dateDecodingStrategy = .custom { decoder in
            let container = try decoder.singleValueContainer()
            let dateString = try container.decode(String.self)

            // Try ISO 8601 with fractional seconds and timezone
            let formatterWithFractional = ISO8601DateFormatter()
            formatterWithFractional.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
            if let date = formatterWithFractional.date(from: dateString) {
                return date
            }

            // Try standard ISO 8601 with timezone
            let formatterStandard = ISO8601DateFormatter()
            formatterStandard.formatOptions = [.withInternetDateTime]
            if let date = formatterStandard.date(from: dateString) {
                return date
            }

            // Try ISO 8601 without timezone (assume UTC)
            let formatterNoTimezone = DateFormatter()
            formatterNoTimezone.dateFormat = "yyyy-MM-dd'T'HH:mm:ss"
            formatterNoTimezone.timeZone = TimeZone(identifier: "UTC")
            if let date = formatterNoTimezone.date(from: dateString) {
                return date
            }

            // Try with fractional seconds but no timezone
            formatterNoTimezone.dateFormat = "yyyy-MM-dd'T'HH:mm:ss.SSSSSS"
            if let date = formatterNoTimezone.date(from: dateString) {
                return date
            }

            throw DecodingError.dataCorruptedError(in: container, debugDescription: "Cannot decode date: \(dateString)")
        }
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

    /// Convenience initializer with configuration from Info.plist
    convenience init() {
        self.init(baseURL: Configuration.apiBaseURL)
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

    /// Change password for authenticated user
    /// - Parameter request: Change password request with current and new password
    /// - Returns: Success message
    /// - Throws: APIError if request fails
    func changePassword(_ request: ChangePasswordRequest) async throws -> PasswordMessageResponse {
        let endpoint = baseURL.appendingPathComponent("/api/v1/auth/change-password")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Request password reset email
    /// - Parameter request: Forgot password request with email
    /// - Returns: Success message
    /// - Throws: APIError if request fails
    func forgotPassword(_ request: ForgotPasswordRequest) async throws -> PasswordMessageResponse {
        let endpoint = baseURL.appendingPathComponent("/api/v1/auth/forgot-password")

        var urlRequest = URLRequest(url: endpoint)
        urlRequest.httpMethod = "POST"
        urlRequest.setValue("application/json", forHTTPHeaderField: "Content-Type")
        urlRequest.httpBody = try jsonEncoder.encode(request)

        return try await performRequest(urlRequest)
    }

    /// Reset password with token from email
    /// - Parameter request: Reset password request with email, token, and new password
    /// - Returns: Success message
    /// - Throws: APIError if request fails
    func resetPassword(_ request: ResetPasswordRequest) async throws -> PasswordMessageResponse {
        let endpoint = baseURL.appendingPathComponent("/api/v1/auth/reset-password")

        var urlRequest = URLRequest(url: endpoint)
        urlRequest.httpMethod = "POST"
        urlRequest.setValue("application/json", forHTTPHeaderField: "Content-Type")
        urlRequest.httpBody = try jsonEncoder.encode(request)

        return try await performRequest(urlRequest)
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

    /// Create a new child account
    /// - Parameter request: Child creation request with account and settings details
    /// - Returns: Created child object
    /// - Throws: APIError if request fails
    func createChild(_ request: CreateChildRequest) async throws -> Child {
        let endpoint = baseURL.appendingPathComponent("/api/v1/auth/register/child")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Update child settings including allowance, allowanceDay, and savings configuration
    /// - Parameters:
    ///   - childId: Child's unique identifier
    ///   - request: Update request with all settings
    /// - Returns: Updated child settings response
    /// - Throws: APIError if request fails
    func updateChildSettings(childId: UUID, _ request: UpdateChildSettingsRequest) async throws -> UpdateChildSettingsResponse {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/settings")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PUT", body: body)
        return try await performRequest(urlRequest)
    }

    // MARK: - Transactions

    /// Get transactions for a specific child
    /// - Parameters:
    ///   - childId: Child's unique identifier
    ///   - limit: Maximum number of transactions to return
    /// - Returns: Array of transactions
    /// - Throws: APIError if request fails
    func getTransactions(forChild childId: UUID, limit: Int = 20) async throws -> [Transaction] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/transactions?limit=\(limit)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Create a new transaction
    /// - Parameter request: Transaction creation request
    /// - Returns: Created transaction
    /// - Throws: APIError if request fails
    func createTransaction(_ request: CreateTransactionRequest) async throws -> Transaction {
        let endpoint = baseURL.appendingPathComponent("/api/v1/transactions")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Get current balance for a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Current balance
    /// - Throws: APIError if request fails
    func getBalance(forChild childId: UUID) async throws -> Decimal {
        let endpoint = baseURL.appendingPathComponent("/api/v1/transactions/children/\(childId.uuidString)/balance")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        let response: [String: Decimal] = try await performRequest(urlRequest)
        return response["balance"] ?? 0
    }

    // MARK: - Wish List

    /// Get wish list items for a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Array of wish list items
    /// - Throws: APIError if request fails
    func getWishList(forChild childId: UUID) async throws -> [WishListItem] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/wishlist/children/\(childId.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Create a new wish list item
    /// - Parameter request: Wish list item creation request
    /// - Returns: Created wish list item
    /// - Throws: APIError if request fails
    func createWishListItem(_ request: CreateWishListItemRequest) async throws -> WishListItem {
        let endpoint = baseURL.appendingPathComponent("/api/v1/wishlist")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Update a wish list item
    /// - Parameters:
    ///   - id: Wish list item identifier
    ///   - request: Update request
    /// - Returns: Updated wish list item
    /// - Throws: APIError if request fails
    func updateWishListItem(id: UUID, _ request: UpdateWishListItemRequest) async throws -> WishListItem {
        let endpoint = baseURL.appendingPathComponent("/api/v1/wishlist/\(id.uuidString)")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PUT", body: body)
        return try await performRequest(urlRequest)
    }

    /// Delete a wish list item
    /// - Parameter id: Wish list item identifier
    /// - Throws: APIError if request fails
    func deleteWishListItem(id: UUID) async throws {
        let endpoint = baseURL.appendingPathComponent("/api/v1/wishlist/\(id.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "DELETE")
        let _: EmptyResponse = try await performRequest(urlRequest)
    }

    /// Mark wish list item as purchased
    /// - Parameter id: Wish list item identifier
    /// - Returns: Updated wish list item
    /// - Throws: APIError if request fails
    func markWishListItemAsPurchased(id: UUID) async throws -> WishListItem {
        let endpoint = baseURL.appendingPathComponent("/api/v1/wishlist/\(id.uuidString)/purchase")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST")
        return try await performRequest(urlRequest)
    }

    // MARK: - Analytics

    /// Get balance history for a child
    /// - Parameters:
    ///   - childId: Child's unique identifier
    ///   - days: Number of days to retrieve
    /// - Returns: Array of balance points
    /// - Throws: APIError if request fails
    func getBalanceHistory(forChild childId: UUID, days: Int = 30) async throws -> [BalancePoint] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/analytics/children/\(childId.uuidString)/balance-history?days=\(days)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get income vs spending summary for a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Income spending summary
    /// - Throws: APIError if request fails
    func getIncomeVsSpending(forChild childId: UUID) async throws -> IncomeSpendingSummary {
        let endpoint = baseURL.appendingPathComponent("/api/v1/analytics/children/\(childId.uuidString)/income-spending")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get spending breakdown by category for a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Array of category breakdowns
    /// - Throws: APIError if request fails
    func getSpendingBreakdown(forChild childId: UUID) async throws -> [CategoryBreakdown] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/analytics/children/\(childId.uuidString)/spending-breakdown")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get monthly comparison for a child
    /// - Parameters:
    ///   - childId: Child's unique identifier
    ///   - months: Number of months to retrieve
    /// - Returns: Array of monthly comparisons
    /// - Throws: APIError if request fails
    func getMonthlyComparison(forChild childId: UUID, months: Int = 6) async throws -> [MonthlyComparison] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/analytics/children/\(childId.uuidString)/monthly-comparison?months=\(months)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    // MARK: - Savings Accounts

    /// Get savings accounts for a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Array of savings accounts
    /// - Throws: APIError if request fails
    func getSavingsAccounts(forChild childId: UUID) async throws -> [SavingsAccount] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings/children/\(childId.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Create a new savings account
    /// - Parameter request: Savings account creation request
    /// - Returns: Created savings account
    /// - Throws: APIError if request fails
    func createSavingsAccount(_ request: CreateSavingsAccountRequest) async throws -> SavingsAccount {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Update a savings account
    /// - Parameters:
    ///   - id: Savings account identifier
    ///   - request: Update request
    /// - Returns: Updated savings account
    /// - Throws: APIError if request fails
    func updateSavingsAccount(id: UUID, _ request: UpdateSavingsAccountRequest) async throws -> SavingsAccount {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings/\(id.uuidString)")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PUT", body: body)
        return try await performRequest(urlRequest)
    }

    /// Delete a savings account
    /// - Parameter id: Savings account identifier
    /// - Throws: APIError if request fails
    func deleteSavingsAccount(id: UUID) async throws {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings/\(id.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "DELETE")
        let _: EmptyResponse = try await performRequest(urlRequest)
    }

    /// Deposit into a savings account
    /// - Parameters:
    ///   - accountId: Savings account identifier
    ///   - request: Deposit request
    /// - Returns: Created savings transaction
    /// - Throws: APIError if request fails
    func depositToSavings(accountId: UUID, _ request: DepositRequest) async throws -> SavingsTransaction {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings/\(accountId.uuidString)/deposit")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Withdraw from a savings account
    /// - Parameters:
    ///   - accountId: Savings account identifier
    ///   - request: Withdraw request
    /// - Returns: Created savings transaction
    /// - Throws: APIError if request fails
    func withdrawFromSavings(accountId: UUID, _ request: WithdrawRequest) async throws -> SavingsTransaction {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings/\(accountId.uuidString)/withdraw")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Get transactions for a savings account
    /// - Parameter accountId: Savings account identifier
    /// - Returns: Array of savings transactions
    /// - Throws: APIError if request fails
    func getSavingsTransactions(forAccount accountId: UUID) async throws -> [SavingsTransaction] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings/\(accountId.uuidString)/transactions")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get savings account summary for a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Savings account summary (includes balanceHidden flag)
    /// - Throws: APIError if request fails
    func getSavingsSummary(forChild childId: UUID) async throws -> SavingsAccountSummary {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings/\(childId.uuidString)/summary")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    // MARK: - Parent Invites

    /// Send an invite to a co-parent
    /// - Parameter request: Invite request with email and name
    /// - Returns: Invite response with details
    /// - Throws: APIError if request fails
    func sendParentInvite(_ request: SendParentInviteRequest) async throws -> ParentInviteResponse {
        let endpoint = baseURL.appendingPathComponent("/api/v1/invites/parent")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Get all pending invites for the current family
    /// - Returns: Array of pending invites
    /// - Throws: APIError if request fails
    func getPendingInvites() async throws -> [PendingInvite] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/invites")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Cancel a pending invite
    /// - Parameter inviteId: Invite identifier to cancel
    /// - Throws: APIError if request fails
    func cancelInvite(inviteId: String) async throws {
        let endpoint = baseURL.appendingPathComponent("/api/v1/invites/\(inviteId)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "DELETE")
        let _: EmptyResponse = try await performRequest(urlRequest)
    }

    /// Resend a pending invite
    /// - Parameter inviteId: Invite identifier to resend
    /// - Returns: Updated invite response
    /// - Throws: APIError if request fails
    func resendInvite(inviteId: String) async throws -> ParentInviteResponse {
        let endpoint = baseURL.appendingPathComponent("/api/v1/invites/\(inviteId)/resend")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST")
        return try await performRequest(urlRequest)
    }

    // MARK: - Private Helpers

    /// Empty response for DELETE requests
    private struct EmptyResponse: Codable {}

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
                } catch let decodingError {
                    #if DEBUG
                    print("🔴 Decoding error: \(decodingError)")
                    if let jsonString = String(data: data, encoding: .utf8) {
                        print("🔴 Raw response: \(jsonString)")
                    }
                    #endif
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
