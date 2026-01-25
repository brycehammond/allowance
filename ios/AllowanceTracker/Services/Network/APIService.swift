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
        // Create URLSession with reasonable timeout (15 seconds)
        let configuration = URLSessionConfiguration.default
        configuration.timeoutIntervalForRequest = 15
        configuration.timeoutIntervalForResource = 30
        let session = URLSession(configuration: configuration)

        self.init(baseURL: Configuration.apiBaseURL, urlSession: session)
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

    /// Refresh the current JWT token
    /// - Returns: New authentication response with fresh token
    /// - Throws: APIError if refresh fails (e.g., token expired)
    func refreshToken() async throws -> AuthResponse {
        let endpoint = baseURL.appendingPathComponent("/api/v1/auth/refresh")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST")

        let response: AuthResponse = try await performRequest(urlRequest)

        // Save new token to keychain
        try keychainService.saveToken(response.token)

        return response
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

    /// Delete current user's account
    /// - Throws: APIError if request fails
    func deleteAccount() async throws {
        let endpoint = baseURL.appendingPathComponent("/api/v1/auth/account")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "DELETE")
        let _: EmptyResponse = try await performRequest(urlRequest)
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
        var components = URLComponents(url: baseURL, resolvingAgainstBaseURL: true)!
        components.path = "/api/v1/children/\(childId.uuidString)/transactions"
        components.queryItems = [URLQueryItem(name: "limit", value: String(limit))]
        guard let endpoint = components.url else { throw APIError.invalidURL }
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
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/balance")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        let response: [String: Decimal] = try await performRequest(urlRequest)
        return response["balance"] ?? 0
    }

    // MARK: - Analytics

    /// Get balance history for a child
    /// - Parameters:
    ///   - childId: Child's unique identifier
    ///   - days: Number of days to retrieve
    /// - Returns: Array of balance points
    /// - Throws: APIError if request fails
    func getBalanceHistory(forChild childId: UUID, days: Int = 30) async throws -> [BalancePoint] {
        var components = URLComponents(url: baseURL, resolvingAgainstBaseURL: true)!
        components.path = "/api/v1/children/\(childId.uuidString)/analytics/balance-history"
        components.queryItems = [URLQueryItem(name: "days", value: String(days))]
        guard let endpoint = components.url else { throw APIError.invalidURL }
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get income vs spending summary for a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Income spending summary
    /// - Throws: APIError if request fails
    func getIncomeVsSpending(forChild childId: UUID) async throws -> IncomeSpendingSummary {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/analytics/income-spending")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get spending breakdown by category for a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Array of category breakdowns
    /// - Throws: APIError if request fails
    func getSpendingBreakdown(forChild childId: UUID) async throws -> [CategoryBreakdown] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/analytics/spending-breakdown")
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
        var components = URLComponents(url: baseURL, resolvingAgainstBaseURL: true)!
        components.path = "/api/v1/children/\(childId.uuidString)/analytics/monthly-comparison"
        components.queryItems = [URLQueryItem(name: "months", value: String(months))]
        guard let endpoint = components.url else { throw APIError.invalidURL }
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    // MARK: - Savings

    /// Get savings account summary for a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Savings account summary (includes balanceHidden flag)
    /// - Throws: APIError if request fails
    func getSavingsSummary(forChild childId: UUID) async throws -> SavingsAccountSummary {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/savings/summary")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get savings transaction history for a child
    /// - Parameters:
    ///   - childId: Child's unique identifier
    ///   - limit: Maximum number of transactions to return
    /// - Returns: Array of savings transactions
    /// - Throws: APIError if request fails
    func getSavingsHistory(forChild childId: UUID, limit: Int = 50) async throws -> [SavingsTransaction] {
        var components = URLComponents(url: baseURL, resolvingAgainstBaseURL: true)!
        components.path = "/api/v1/children/\(childId.uuidString)/savings/history"
        components.queryItems = [URLQueryItem(name: "limit", value: String(limit))]
        guard let endpoint = components.url else { throw APIError.invalidURL }
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Deposit to savings from main balance
    /// - Parameter request: Deposit request with childId, amount, and description
    /// - Returns: Created savings transaction
    /// - Throws: APIError if request fails
    func depositToSavings(_ request: DepositToSavingsRequest) async throws -> SavingsTransaction {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(request.childId.uuidString)/savings/deposit")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Withdraw from savings to main balance
    /// - Parameter request: Withdraw request with childId, amount, and description
    /// - Returns: Created savings transaction
    /// - Throws: APIError if request fails
    func withdrawFromSavings(_ request: WithdrawFromSavingsRequest) async throws -> SavingsTransaction {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(request.childId.uuidString)/savings/withdraw")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
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

    // MARK: - Badges

    /// Get all available badges
    /// - Parameters:
    ///   - category: Optional category filter
    ///   - includeSecret: Whether to include secret badges
    /// - Returns: Array of badge definitions
    /// - Throws: APIError if request fails
    func getAllBadges(category: BadgeCategory?, includeSecret: Bool) async throws -> [BadgeDto] {
        var components = URLComponents(url: baseURL, resolvingAgainstBaseURL: true)!
        components.path = "/api/v1/badges"
        var queryItems: [URLQueryItem] = []
        if let category = category {
            queryItems.append(URLQueryItem(name: "category", value: category.rawValue))
        }
        queryItems.append(URLQueryItem(name: "includeSecret", value: String(includeSecret)))
        components.queryItems = queryItems.isEmpty ? nil : queryItems
        guard let endpoint = components.url else { throw APIError.invalidURL }
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get badges earned by a child
    /// - Parameters:
    ///   - childId: Child's unique identifier
    ///   - category: Optional category filter
    ///   - newOnly: Whether to only return newly earned badges
    /// - Returns: Array of earned badges
    /// - Throws: APIError if request fails
    func getChildBadges(forChild childId: UUID, category: BadgeCategory?, newOnly: Bool) async throws -> [ChildBadgeDto] {
        var components = URLComponents(url: baseURL, resolvingAgainstBaseURL: true)!
        components.path = "/api/v1/children/\(childId.uuidString)/badges"
        var queryItems: [URLQueryItem] = []
        if let category = category {
            queryItems.append(URLQueryItem(name: "category", value: category.rawValue))
        }
        if newOnly {
            queryItems.append(URLQueryItem(name: "newOnly", value: "true"))
        }
        components.queryItems = queryItems.isEmpty ? nil : queryItems
        guard let endpoint = components.url else { throw APIError.invalidURL }
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get badge progress for a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Array of badge progress items
    /// - Throws: APIError if request fails
    func getBadgeProgress(forChild childId: UUID) async throws -> [BadgeProgressDto] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/badges/progress")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get achievement summary for a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Achievement summary with stats and recent badges
    /// - Throws: APIError if request fails
    func getAchievementSummary(forChild childId: UUID) async throws -> AchievementSummaryDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/badges/summary")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Toggle whether a badge is displayed on child's profile
    /// - Parameters:
    ///   - childId: Child's unique identifier
    ///   - badgeId: Badge's unique identifier
    ///   - request: Update request with display setting
    /// - Returns: Updated child badge
    /// - Throws: APIError if request fails
    func toggleBadgeDisplay(forChild childId: UUID, badgeId: UUID, _ request: UpdateBadgeDisplayRequest) async throws -> ChildBadgeDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/badges/\(badgeId.uuidString)/display")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PATCH", body: body)
        return try await performRequest(urlRequest)
    }

    /// Mark badges as seen by the child
    /// - Parameters:
    ///   - childId: Child's unique identifier
    ///   - request: Request with badge IDs to mark as seen
    /// - Throws: APIError if request fails
    func markBadgesSeen(forChild childId: UUID, _ request: MarkBadgesSeenRequest) async throws {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/badges/seen")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        let _: EmptyResponse = try await performRequest(urlRequest)
    }

    /// Get points summary for a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Points summary with totals and counts
    /// - Throws: APIError if request fails
    func getChildPoints(forChild childId: UUID) async throws -> ChildPointsDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/points")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    // MARK: - Rewards

    /// Get all available rewards
    /// - Parameters:
    ///   - type: Optional reward type filter
    ///   - childId: Optional child ID to check affordability
    /// - Returns: Array of available rewards
    /// - Throws: APIError if request fails
    func getAvailableRewards(type: RewardType?, forChild childId: UUID?) async throws -> [RewardDto] {
        var components = URLComponents(url: baseURL, resolvingAgainstBaseURL: true)!
        components.path = "/api/v1/rewards"
        var queryItems: [URLQueryItem] = []
        if let type = type {
            queryItems.append(URLQueryItem(name: "type", value: type.rawValue))
        }
        if let childId = childId {
            queryItems.append(URLQueryItem(name: "childId", value: childId.uuidString))
        }
        components.queryItems = queryItems.isEmpty ? nil : queryItems
        guard let endpoint = components.url else { throw APIError.invalidURL }
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get rewards unlocked by a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Array of unlocked rewards
    /// - Throws: APIError if request fails
    func getChildRewards(forChild childId: UUID) async throws -> [RewardDto] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/rewards")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Unlock a reward using points
    /// - Parameters:
    ///   - childId: Child's unique identifier
    ///   - rewardId: Reward's unique identifier
    /// - Returns: Unlocked reward details
    /// - Throws: APIError if request fails
    func unlockReward(forChild childId: UUID, rewardId: UUID) async throws -> RewardDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/rewards/\(rewardId.uuidString)/unlock")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST")
        return try await performRequest(urlRequest)
    }

    /// Equip a reward (avatar, theme, or title)
    /// - Parameters:
    ///   - childId: Child's unique identifier
    ///   - rewardId: Reward's unique identifier
    /// - Returns: Updated reward details
    /// - Throws: APIError if request fails
    func equipReward(forChild childId: UUID, rewardId: UUID) async throws -> RewardDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/rewards/\(rewardId.uuidString)/equip")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST")
        return try await performRequest(urlRequest)
    }

    /// Unequip a reward
    /// - Parameters:
    ///   - childId: Child's unique identifier
    ///   - rewardId: Reward's unique identifier
    /// - Throws: APIError if request fails
    func unequipReward(forChild childId: UUID, rewardId: UUID) async throws {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/rewards/\(rewardId.uuidString)/unequip")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST")
        let _: EmptyResponse = try await performRequest(urlRequest)
    }

    // MARK: - Tasks/Chores

    /// Get all tasks with optional filters
    /// - Parameters:
    ///   - childId: Optional child ID to filter by
    ///   - status: Optional status filter (Active/Archived)
    ///   - isRecurring: Optional recurring filter
    /// - Returns: Array of tasks
    /// - Throws: APIError if request fails
    func getTasks(childId: UUID?, status: ChoreTaskStatus?, isRecurring: Bool?) async throws -> [ChoreTask] {
        var components = URLComponents(url: baseURL.appendingPathComponent("/api/v1/tasks"), resolvingAgainstBaseURL: false)!
        var queryItems: [URLQueryItem] = []

        if let childId = childId {
            queryItems.append(URLQueryItem(name: "childId", value: childId.uuidString))
        }
        if let status = status {
            queryItems.append(URLQueryItem(name: "status", value: status.rawValue))
        }
        if let isRecurring = isRecurring {
            queryItems.append(URLQueryItem(name: "isRecurring", value: String(isRecurring)))
        }

        if !queryItems.isEmpty {
            components.queryItems = queryItems
        }

        let urlRequest = try await createAuthenticatedRequest(url: components.url!, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get a task by ID
    /// - Parameter id: Task's unique identifier
    /// - Returns: Task details
    /// - Throws: APIError if request fails
    func getTask(id: UUID) async throws -> ChoreTask {
        let endpoint = baseURL.appendingPathComponent("/api/v1/tasks/\(id.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Create a new task
    /// - Parameter request: Task creation data
    /// - Returns: Created task
    /// - Throws: APIError if request fails
    func createTask(_ request: CreateTaskRequest) async throws -> ChoreTask {
        let endpoint = baseURL.appendingPathComponent("/api/v1/tasks")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Update an existing task
    /// - Parameters:
    ///   - id: Task's unique identifier
    ///   - request: Task update data
    /// - Returns: Updated task
    /// - Throws: APIError if request fails
    func updateTask(id: UUID, _ request: UpdateTaskRequest) async throws -> ChoreTask {
        let endpoint = baseURL.appendingPathComponent("/api/v1/tasks/\(id.uuidString)")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PUT", body: body)
        return try await performRequest(urlRequest)
    }

    /// Archive a task (soft delete)
    /// - Parameter id: Task's unique identifier
    /// - Throws: APIError if request fails
    func archiveTask(id: UUID) async throws {
        let endpoint = baseURL.appendingPathComponent("/api/v1/tasks/\(id.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "DELETE")
        let _: EmptyResponse = try await performRequest(urlRequest)
    }

    /// Complete a task with optional photo
    /// - Parameters:
    ///   - id: Task's unique identifier
    ///   - notes: Optional completion notes
    ///   - photoData: Optional photo data
    ///   - photoFileName: Optional photo filename
    /// - Returns: Task completion record
    /// - Throws: APIError if request fails
    func completeTask(id: UUID, notes: String?, photoData: Data?, photoFileName: String?) async throws -> TaskCompletion {
        let endpoint = baseURL.appendingPathComponent("/api/v1/tasks/\(id.uuidString)/complete")

        guard let token = try? keychainService.getToken() else {
            throw APIError.unauthorized
        }

        let boundary = UUID().uuidString
        var request = URLRequest(url: endpoint)
        request.httpMethod = "POST"
        request.setValue("multipart/form-data; boundary=\(boundary)", forHTTPHeaderField: "Content-Type")
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")

        var body = Data()

        // Add notes if provided
        if let notes = notes, !notes.isEmpty {
            body.append("--\(boundary)\r\n".data(using: .utf8)!)
            body.append("Content-Disposition: form-data; name=\"notes\"\r\n\r\n".data(using: .utf8)!)
            body.append("\(notes)\r\n".data(using: .utf8)!)
        }

        // Add photo if provided
        if let photoData = photoData, let fileName = photoFileName {
            let mimeType = getMimeType(for: fileName)
            body.append("--\(boundary)\r\n".data(using: .utf8)!)
            body.append("Content-Disposition: form-data; name=\"photo\"; filename=\"\(fileName)\"\r\n".data(using: .utf8)!)
            body.append("Content-Type: \(mimeType)\r\n\r\n".data(using: .utf8)!)
            body.append(photoData)
            body.append("\r\n".data(using: .utf8)!)
        }

        body.append("--\(boundary)--\r\n".data(using: .utf8)!)
        request.httpBody = body

        return try await performRequest(request)
    }

    /// Get completions for a task
    /// - Parameters:
    ///   - taskId: Task's unique identifier
    ///   - status: Optional status filter
    /// - Returns: Array of task completions
    /// - Throws: APIError if request fails
    func getTaskCompletions(taskId: UUID, status: CompletionStatus?) async throws -> [TaskCompletion] {
        var components = URLComponents(url: baseURL.appendingPathComponent("/api/v1/tasks/\(taskId.uuidString)/completions"), resolvingAgainstBaseURL: false)!

        if let status = status {
            components.queryItems = [URLQueryItem(name: "status", value: status.rawValue)]
        }

        let urlRequest = try await createAuthenticatedRequest(url: components.url!, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get all pending approvals for the current user's family
    /// - Returns: Array of pending task completions
    /// - Throws: APIError if request fails
    func getPendingApprovals() async throws -> [TaskCompletion] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/tasks/completions/pending")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Review a task completion (approve or reject)
    /// - Parameters:
    ///   - id: Completion's unique identifier
    ///   - request: Review data with approval status
    /// - Returns: Updated task completion
    /// - Throws: APIError if request fails
    func reviewCompletion(id: UUID, _ request: ReviewCompletionRequest) async throws -> TaskCompletion {
        let endpoint = baseURL.appendingPathComponent("/api/v1/tasks/completions/\(id.uuidString)/review")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PUT", body: body)
        return try await performRequest(urlRequest)
    }

    /// Get MIME type for file extension
    private func getMimeType(for fileName: String) -> String {
        let ext = (fileName as NSString).pathExtension.lowercased()
        switch ext {
        case "jpg", "jpeg": return "image/jpeg"
        case "png": return "image/png"
        case "gif": return "image/gif"
        case "webp": return "image/webp"
        default: return "application/octet-stream"
        }
    }

    // MARK: - Savings Goals

    /// Get savings goals for a child
    /// - Parameters:
    ///   - childId: Child's unique identifier
    ///   - status: Optional status filter
    ///   - includeCompleted: Whether to include completed goals
    /// - Returns: Array of savings goals
    /// - Throws: APIError if request fails
    func getSavingsGoals(forChild childId: UUID, status: GoalStatus?, includeCompleted: Bool) async throws -> [SavingsGoalDto] {
        var components = URLComponents(url: baseURL, resolvingAgainstBaseURL: true)!
        components.path = "/api/v1/children/\(childId.uuidString)/savings-goals"
        var queryItems: [URLQueryItem] = []
        if let status = status {
            queryItems.append(URLQueryItem(name: "status", value: status.rawValue))
        }
        queryItems.append(URLQueryItem(name: "includeCompleted", value: String(includeCompleted)))
        components.queryItems = queryItems.isEmpty ? nil : queryItems
        guard let endpoint = components.url else { throw APIError.invalidURL }
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get a savings goal by ID
    /// - Parameter id: Goal's unique identifier
    /// - Returns: Savings goal details
    /// - Throws: APIError if request fails
    func getSavingsGoal(id: UUID) async throws -> SavingsGoalDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(id.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Create a new savings goal
    /// - Parameter request: Goal creation data
    /// - Returns: Created savings goal
    /// - Throws: APIError if request fails
    func createSavingsGoal(_ request: CreateSavingsGoalRequest) async throws -> SavingsGoalDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Update an existing savings goal
    /// - Parameters:
    ///   - id: Goal's unique identifier
    ///   - request: Goal update data
    /// - Returns: Updated savings goal
    /// - Throws: APIError if request fails
    func updateSavingsGoal(id: UUID, _ request: UpdateSavingsGoalRequest) async throws -> SavingsGoalDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(id.uuidString)")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PUT", body: body)
        return try await performRequest(urlRequest)
    }

    /// Delete a savings goal
    /// - Parameter id: Goal's unique identifier
    /// - Throws: APIError if request fails
    func deleteSavingsGoal(id: UUID) async throws {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(id.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "DELETE")
        let _: EmptyResponse = try await performRequest(urlRequest)
    }

    /// Pause a savings goal
    /// - Parameter id: Goal's unique identifier
    /// - Returns: Updated savings goal
    /// - Throws: APIError if request fails
    func pauseSavingsGoal(id: UUID) async throws -> SavingsGoalDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(id.uuidString)/pause")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST")
        return try await performRequest(urlRequest)
    }

    /// Resume a paused savings goal
    /// - Parameter id: Goal's unique identifier
    /// - Returns: Updated savings goal
    /// - Throws: APIError if request fails
    func resumeSavingsGoal(id: UUID) async throws -> SavingsGoalDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(id.uuidString)/resume")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST")
        return try await performRequest(urlRequest)
    }

    /// Contribute to a savings goal
    /// - Parameters:
    ///   - goalId: Goal's unique identifier
    ///   - request: Contribution data
    /// - Returns: Goal progress event with contribution details
    /// - Throws: APIError if request fails
    func contributeToGoal(goalId: UUID, _ request: ContributeToGoalRequest) async throws -> GoalProgressEventDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(goalId.uuidString)/contribute")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Withdraw from a savings goal
    /// - Parameters:
    ///   - goalId: Goal's unique identifier
    ///   - request: Withdrawal data
    /// - Returns: Contribution record for the withdrawal
    /// - Throws: APIError if request fails
    func withdrawFromGoal(goalId: UUID, _ request: WithdrawFromGoalRequest) async throws -> GoalContributionDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(goalId.uuidString)/withdraw")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Get contributions for a savings goal
    /// - Parameters:
    ///   - goalId: Goal's unique identifier
    ///   - type: Optional contribution type filter
    /// - Returns: Array of contributions
    /// - Throws: APIError if request fails
    func getGoalContributions(goalId: UUID, type: ContributionType?) async throws -> [GoalContributionDto] {
        var components = URLComponents(url: baseURL, resolvingAgainstBaseURL: true)!
        components.path = "/api/v1/savings-goals/\(goalId.uuidString)/contributions"
        if let type = type {
            components.queryItems = [URLQueryItem(name: "type", value: type.rawValue)]
        }
        guard let endpoint = components.url else { throw APIError.invalidURL }
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Mark a savings goal as purchased
    /// - Parameters:
    ///   - goalId: Goal's unique identifier
    ///   - request: Optional purchase notes
    /// - Returns: Updated savings goal
    /// - Throws: APIError if request fails
    func markGoalAsPurchased(goalId: UUID, _ request: MarkGoalPurchasedRequest?) async throws -> SavingsGoalDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(goalId.uuidString)/purchase")
        var urlRequest: URLRequest
        if let request = request {
            let body = try jsonEncoder.encode(request)
            urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        } else {
            urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST")
        }
        return try await performRequest(urlRequest)
    }

    /// Create a matching rule for a savings goal
    /// - Parameters:
    ///   - goalId: Goal's unique identifier
    ///   - request: Matching rule configuration
    /// - Returns: Created matching rule
    /// - Throws: APIError if request fails
    func createMatchingRule(goalId: UUID, _ request: CreateMatchingRuleRequest) async throws -> MatchingRuleDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(goalId.uuidString)/matching")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Get matching rule for a savings goal
    /// - Parameter goalId: Goal's unique identifier
    /// - Returns: Matching rule if exists, nil otherwise
    /// - Throws: APIError if request fails
    func getMatchingRule(goalId: UUID) async throws -> MatchingRuleDto? {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(goalId.uuidString)/matching")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        do {
            return try await performRequest(urlRequest)
        } catch APIError.notFound {
            return nil
        }
    }

    /// Update matching rule for a savings goal
    /// - Parameters:
    ///   - goalId: Goal's unique identifier
    ///   - request: Updated matching rule configuration
    /// - Returns: Updated matching rule
    /// - Throws: APIError if request fails
    func updateMatchingRule(goalId: UUID, _ request: UpdateMatchingRuleRequest) async throws -> MatchingRuleDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(goalId.uuidString)/matching")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PUT", body: body)
        return try await performRequest(urlRequest)
    }

    /// Delete matching rule for a savings goal
    /// - Parameter goalId: Goal's unique identifier
    /// - Throws: APIError if request fails
    func deleteMatchingRule(goalId: UUID) async throws {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(goalId.uuidString)/matching")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "DELETE")
        let _: EmptyResponse = try await performRequest(urlRequest)
    }

    /// Create a challenge for a savings goal
    /// - Parameters:
    ///   - goalId: Goal's unique identifier
    ///   - request: Challenge configuration
    /// - Returns: Created challenge
    /// - Throws: APIError if request fails
    func createGoalChallenge(goalId: UUID, _ request: CreateGoalChallengeRequest) async throws -> GoalChallengeDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(goalId.uuidString)/challenge")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Get active challenge for a savings goal
    /// - Parameter goalId: Goal's unique identifier
    /// - Returns: Active challenge if exists, nil otherwise
    /// - Throws: APIError if request fails
    func getGoalChallenge(goalId: UUID) async throws -> GoalChallengeDto? {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(goalId.uuidString)/challenge")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        do {
            return try await performRequest(urlRequest)
        } catch APIError.notFound {
            return nil
        }
    }

    /// Cancel a challenge for a savings goal
    /// - Parameter goalId: Goal's unique identifier
    /// - Throws: APIError if request fails
    func cancelGoalChallenge(goalId: UUID) async throws {
        let endpoint = baseURL.appendingPathComponent("/api/v1/savings-goals/\(goalId.uuidString)/challenge")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "DELETE")
        let _: EmptyResponse = try await performRequest(urlRequest)
    }

    /// Get all challenges for a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Array of challenges
    /// - Throws: APIError if request fails
    func getChildChallenges(forChild childId: UUID) async throws -> [GoalChallengeDto] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/challenges")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    // MARK: - Notifications

    /// Get notifications with pagination
    /// - Parameters:
    ///   - page: Page number (1-based)
    ///   - pageSize: Number of notifications per page
    ///   - unreadOnly: Filter to only unread notifications
    ///   - type: Optional notification type filter
    /// - Returns: Notification list response with pagination info
    /// - Throws: APIError if request fails
    func getNotifications(page: Int, pageSize: Int, unreadOnly: Bool, type: NotificationType?) async throws -> NotificationListResponse {
        var components = URLComponents(url: baseURL, resolvingAgainstBaseURL: true)!
        components.path = "/api/v1/notifications"
        var queryItems: [URLQueryItem] = [
            URLQueryItem(name: "page", value: String(page)),
            URLQueryItem(name: "pageSize", value: String(pageSize)),
            URLQueryItem(name: "unreadOnly", value: String(unreadOnly))
        ]
        if let type = type {
            queryItems.append(URLQueryItem(name: "type", value: String(type.rawValue)))
        }
        components.queryItems = queryItems
        guard let endpoint = components.url else { throw APIError.invalidURL }
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get unread notification count
    /// - Returns: Number of unread notifications
    /// - Throws: APIError if request fails
    func getUnreadCount() async throws -> Int {
        let endpoint = baseURL.appendingPathComponent("/api/v1/notifications/unread-count")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        let response: [String: Int] = try await performRequest(urlRequest)
        return response["count"] ?? 0
    }

    /// Get a specific notification by ID
    /// - Parameter id: Notification's unique identifier
    /// - Returns: Notification details
    /// - Throws: APIError if request fails
    func getNotification(id: UUID) async throws -> NotificationDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/notifications/\(id.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Mark a notification as read
    /// - Parameter id: Notification's unique identifier
    /// - Returns: Updated notification
    /// - Throws: APIError if request fails
    func markNotificationAsRead(id: UUID) async throws -> NotificationDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/notifications/\(id.uuidString)/read")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PATCH")
        return try await performRequest(urlRequest)
    }

    /// Mark multiple notifications as read
    /// - Parameter request: Request with notification IDs to mark as read
    /// - Returns: Number of notifications marked as read
    /// - Throws: APIError if request fails
    func markMultipleAsRead(_ request: MarkNotificationsReadRequest) async throws -> Int {
        let endpoint = baseURL.appendingPathComponent("/api/v1/notifications/read")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        let response: [String: Int] = try await performRequest(urlRequest)
        return response["markedCount"] ?? 0
    }

    /// Delete a notification
    /// - Parameter id: Notification's unique identifier
    /// - Throws: APIError if request fails
    func deleteNotification(id: UUID) async throws {
        let endpoint = baseURL.appendingPathComponent("/api/v1/notifications/\(id.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "DELETE")
        let _: EmptyResponse = try await performRequest(urlRequest)
    }

    /// Delete all read notifications
    /// - Returns: Number of notifications deleted
    /// - Throws: APIError if request fails
    func deleteAllReadNotifications() async throws -> Int {
        let endpoint = baseURL.appendingPathComponent("/api/v1/notifications")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "DELETE")
        let response: [String: Int] = try await performRequest(urlRequest)
        return response["deletedCount"] ?? 0
    }

    /// Get notification preferences
    /// - Returns: User's notification preferences
    /// - Throws: APIError if request fails
    func getNotificationPreferences() async throws -> NotificationPreferences {
        let endpoint = baseURL.appendingPathComponent("/api/v1/notifications/preferences")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Update notification preferences
    /// - Parameter request: Updated preferences
    /// - Returns: Updated notification preferences
    /// - Throws: APIError if request fails
    func updateNotificationPreferences(_ request: UpdateNotificationPreferencesRequest) async throws -> NotificationPreferences {
        let endpoint = baseURL.appendingPathComponent("/api/v1/notifications/preferences")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PUT", body: body)
        return try await performRequest(urlRequest)
    }

    /// Update quiet hours settings
    /// - Parameter request: Quiet hours configuration
    /// - Returns: Updated notification preferences
    /// - Throws: APIError if request fails
    func updateQuietHours(_ request: UpdateQuietHoursRequest) async throws -> NotificationPreferences {
        let endpoint = baseURL.appendingPathComponent("/api/v1/notifications/preferences/quiet-hours")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PUT", body: body)
        return try await performRequest(urlRequest)
    }

    /// Register a device for push notifications
    /// - Parameter request: Device registration data
    /// - Returns: Registered device token
    /// - Throws: APIError if request fails
    func registerDevice(_ request: RegisterDeviceRequest) async throws -> DeviceTokenDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/devices")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Get registered devices
    /// - Returns: Array of registered devices
    /// - Throws: APIError if request fails
    func getDevices() async throws -> [DeviceTokenDto] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/devices")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Unregister a device
    /// - Parameter id: Device token's unique identifier
    /// - Throws: APIError if request fails
    func unregisterDevice(id: UUID) async throws {
        let endpoint = baseURL.appendingPathComponent("/api/v1/devices/\(id.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "DELETE")
        let _: EmptyResponse = try await performRequest(urlRequest)
    }

    // MARK: - Gift Links

    /// Get all gift links for the current family
    /// - Returns: Array of gift links
    /// - Throws: APIError if request fails
    func getGiftLinks() async throws -> [GiftLinkDto] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/gift-links")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get a gift link by ID
    /// - Parameter id: Gift link's unique identifier
    /// - Returns: Gift link details
    /// - Throws: APIError if request fails
    func getGiftLink(id: UUID) async throws -> GiftLinkDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/gift-links/\(id.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Create a new gift link
    /// - Parameter request: Gift link creation data
    /// - Returns: Created gift link
    /// - Throws: APIError if request fails
    func createGiftLink(_ request: CreateGiftLinkRequest) async throws -> GiftLinkDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/gift-links")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Update an existing gift link
    /// - Parameters:
    ///   - id: Gift link's unique identifier
    ///   - request: Gift link update data
    /// - Returns: Updated gift link
    /// - Throws: APIError if request fails
    func updateGiftLink(id: UUID, _ request: UpdateGiftLinkRequest) async throws -> GiftLinkDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/gift-links/\(id.uuidString)")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PUT", body: body)
        return try await performRequest(urlRequest)
    }

    /// Deactivate a gift link
    /// - Parameter id: Gift link's unique identifier
    /// - Returns: Updated gift link
    /// - Throws: APIError if request fails
    func deactivateGiftLink(id: UUID) async throws -> GiftLinkDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/gift-links/\(id.uuidString)/deactivate")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST")
        return try await performRequest(urlRequest)
    }

    /// Regenerate token for a gift link
    /// - Parameter id: Gift link's unique identifier
    /// - Returns: Updated gift link with new token
    /// - Throws: APIError if request fails
    func regenerateGiftLinkToken(id: UUID) async throws -> GiftLinkDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/gift-links/\(id.uuidString)/regenerate-token")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST")
        return try await performRequest(urlRequest)
    }

    /// Get statistics for a gift link
    /// - Parameter id: Gift link's unique identifier
    /// - Returns: Gift link statistics
    /// - Throws: APIError if request fails
    func getGiftLinkStats(id: UUID) async throws -> GiftLinkStatsDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/gift-links/\(id.uuidString)/stats")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    // MARK: - Gifts

    /// Get gifts for a child
    /// - Parameter childId: Child's unique identifier
    /// - Returns: Array of gifts
    /// - Throws: APIError if request fails
    func getGifts(forChild childId: UUID) async throws -> [GiftDto] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/gifts/child/\(childId.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get a gift by ID
    /// - Parameter id: Gift's unique identifier
    /// - Returns: Gift details
    /// - Throws: APIError if request fails
    func getGift(id: UUID) async throws -> GiftDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/gifts/\(id.uuidString)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Approve a pending gift
    /// - Parameters:
    ///   - id: Gift's unique identifier
    ///   - request: Approval options (goal allocation, savings percentage)
    /// - Returns: Approved gift
    /// - Throws: APIError if request fails
    func approveGift(id: UUID, _ request: ApproveGiftRequest) async throws -> GiftDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/gifts/\(id.uuidString)/approve")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PUT", body: body)
        return try await performRequest(urlRequest)
    }

    /// Reject a pending gift
    /// - Parameters:
    ///   - id: Gift's unique identifier
    ///   - request: Rejection reason
    /// - Returns: Rejected gift
    /// - Throws: APIError if request fails
    func rejectGift(id: UUID, _ request: RejectGiftRequest) async throws -> GiftDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/gifts/\(id.uuidString)/reject")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PUT", body: body)
        return try await performRequest(urlRequest)
    }

    // MARK: - Thank You Notes

    /// Get pending thank yous for the current child
    /// - Returns: Array of pending thank yous
    /// - Throws: APIError if request fails
    func getPendingThankYous() async throws -> [PendingThankYouDto] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/thank-you-notes/pending")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get thank you note for a gift
    /// - Parameter giftId: Gift's unique identifier
    /// - Returns: Thank you note
    /// - Throws: APIError if request fails
    func getThankYouNote(forGiftId giftId: UUID) async throws -> ThankYouNoteDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/gifts/\(giftId.uuidString)/thank-you-note")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Create a thank you note for a gift
    /// - Parameters:
    ///   - giftId: Gift's unique identifier
    ///   - request: Thank you note content
    /// - Returns: Created thank you note
    /// - Throws: APIError if request fails
    func createThankYouNote(forGiftId giftId: UUID, _ request: CreateThankYouNoteRequest) async throws -> ThankYouNoteDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/gifts/\(giftId.uuidString)/thank-you-note")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "POST", body: body)
        return try await performRequest(urlRequest)
    }

    /// Update a thank you note
    /// - Parameters:
    ///   - id: Thank you note's unique identifier
    ///   - request: Updated content
    /// - Returns: Updated thank you note
    /// - Throws: APIError if request fails
    func updateThankYouNote(id: UUID, _ request: UpdateThankYouNoteRequest) async throws -> ThankYouNoteDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/thank-you-notes/\(id.uuidString)")
        let body = try jsonEncoder.encode(request)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PUT", body: body)
        return try await performRequest(urlRequest)
    }

    /// Send a thank you note
    /// - Parameter giftId: Gift's unique identifier
    /// - Returns: Sent thank you note
    /// - Throws: APIError if request fails
    func sendThankYouNote(forGiftId giftId: UUID) async throws -> ThankYouNoteDto {
        let endpoint = baseURL.appendingPathComponent("/api/v1/gifts/\(giftId.uuidString)/thank-you-note/send")
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
