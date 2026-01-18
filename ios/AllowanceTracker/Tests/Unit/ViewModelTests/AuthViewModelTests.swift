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
        if shouldDelay {
            try await Task.sleep(nanoseconds: 100_000_000) // 100ms
        }

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

    // MARK: - Badges (stub implementations)

    func getAllBadges(category: BadgeCategory?, includeSecret: Bool) async throws -> [BadgeDto] {
        return []
    }

    func getChildBadges(forChild childId: UUID, category: BadgeCategory?, newOnly: Bool) async throws -> [ChildBadgeDto] {
        return []
    }

    func getBadgeProgress(forChild childId: UUID) async throws -> [BadgeProgressDto] {
        return []
    }

    func getAchievementSummary(forChild childId: UUID) async throws -> AchievementSummaryDto {
        return AchievementSummaryDto(
            totalBadges: 0,
            earnedBadges: 0,
            totalPoints: 0,
            availablePoints: 0,
            recentBadges: [],
            inProgressBadges: [],
            badgesByCategory: [:]
        )
    }

    func toggleBadgeDisplay(forChild childId: UUID, badgeId: UUID, _ request: UpdateBadgeDisplayRequest) async throws -> ChildBadgeDto {
        throw APIError.notFound
    }

    func markBadgesSeen(forChild childId: UUID, _ request: MarkBadgesSeenRequest) async throws {
        // Mock implementation - do nothing
    }

    func getChildPoints(forChild childId: UUID) async throws -> ChildPointsDto {
        return ChildPointsDto(
            totalPoints: 0,
            availablePoints: 0,
            spentPoints: 0,
            badgesEarned: 0,
            rewardsUnlocked: 0
        )
    }

    // MARK: - Rewards (stub implementations)

    func getAvailableRewards(type: RewardType?, forChild childId: UUID?) async throws -> [RewardDto] {
        return []
    }

    func getChildRewards(forChild childId: UUID) async throws -> [RewardDto] {
        return []
    }

    func unlockReward(forChild childId: UUID, rewardId: UUID) async throws -> RewardDto {
        throw APIError.notFound
    }

    func equipReward(forChild childId: UUID, rewardId: UUID) async throws -> RewardDto {
        throw APIError.notFound
    }

    func unequipReward(forChild childId: UUID, rewardId: UUID) async throws {
        throw APIError.notFound
    }

    // MARK: - Tasks/Chores (stub implementations)

    func getTasks(childId: UUID?, status: ChoreTaskStatus?, isRecurring: Bool?) async throws -> [ChoreTask] {
        return []
    }

    func getTask(id: UUID) async throws -> ChoreTask {
        throw APIError.notFound
    }

    func createTask(_ request: CreateTaskRequest) async throws -> ChoreTask {
        throw APIError.notFound
    }

    func updateTask(id: UUID, _ request: UpdateTaskRequest) async throws -> ChoreTask {
        throw APIError.notFound
    }

    func archiveTask(id: UUID) async throws {
        throw APIError.notFound
    }

    func completeTask(id: UUID, notes: String?, photoData: Data?, photoFileName: String?) async throws -> TaskCompletion {
        throw APIError.notFound
    }

    func getTaskCompletions(taskId: UUID, status: CompletionStatus?) async throws -> [TaskCompletion] {
        return []
    }

    func getPendingApprovals() async throws -> [TaskCompletion] {
        return []
    }

    func reviewCompletion(id: UUID, _ request: ReviewCompletionRequest) async throws -> TaskCompletion {
        throw APIError.notFound
    }

    // MARK: - Savings Goals (stub implementations)

    func getSavingsGoals(forChild childId: UUID, status: GoalStatus?, includeCompleted: Bool) async throws -> [SavingsGoalDto] {
        return []
    }

    func getSavingsGoal(id: UUID) async throws -> SavingsGoalDto {
        throw APIError.notFound
    }

    func createSavingsGoal(_ request: CreateSavingsGoalRequest) async throws -> SavingsGoalDto {
        throw APIError.notFound
    }

    func updateSavingsGoal(id: UUID, _ request: UpdateSavingsGoalRequest) async throws -> SavingsGoalDto {
        throw APIError.notFound
    }

    func deleteSavingsGoal(id: UUID) async throws {
        throw APIError.notFound
    }

    func pauseSavingsGoal(id: UUID) async throws -> SavingsGoalDto {
        throw APIError.notFound
    }

    func resumeSavingsGoal(id: UUID) async throws -> SavingsGoalDto {
        throw APIError.notFound
    }

    func contributeToGoal(goalId: UUID, _ request: ContributeToGoalRequest) async throws -> GoalProgressEventDto {
        throw APIError.notFound
    }

    func withdrawFromGoal(goalId: UUID, _ request: WithdrawFromGoalRequest) async throws -> GoalContributionDto {
        throw APIError.notFound
    }

    func getGoalContributions(goalId: UUID, type: ContributionType?) async throws -> [GoalContributionDto] {
        return []
    }

    func markGoalAsPurchased(goalId: UUID, _ request: MarkGoalPurchasedRequest?) async throws -> SavingsGoalDto {
        throw APIError.notFound
    }

    func createMatchingRule(goalId: UUID, _ request: CreateMatchingRuleRequest) async throws -> MatchingRuleDto {
        throw APIError.notFound
    }

    func getMatchingRule(goalId: UUID) async throws -> MatchingRuleDto? {
        return nil
    }

    func updateMatchingRule(goalId: UUID, _ request: UpdateMatchingRuleRequest) async throws -> MatchingRuleDto {
        throw APIError.notFound
    }

    func deleteMatchingRule(goalId: UUID) async throws {
        throw APIError.notFound
    }

    func createGoalChallenge(goalId: UUID, _ request: CreateGoalChallengeRequest) async throws -> GoalChallengeDto {
        throw APIError.notFound
    }

    func getGoalChallenge(goalId: UUID) async throws -> GoalChallengeDto? {
        return nil
    }

    func cancelGoalChallenge(goalId: UUID) async throws {
        throw APIError.notFound
    }

    func getChildChallenges(forChild childId: UUID) async throws -> [GoalChallengeDto] {
        return []
    }

    // MARK: - Notifications (stub implementations)

    func getNotifications(page: Int, pageSize: Int, unreadOnly: Bool, type: NotificationType?) async throws -> NotificationListResponse {
        return NotificationListResponse(notifications: [], unreadCount: 0, totalCount: 0, hasMore: false)
    }

    func getUnreadCount() async throws -> Int {
        return 0
    }

    func getNotification(id: UUID) async throws -> NotificationDto {
        throw APIError.notFound
    }

    func markNotificationAsRead(id: UUID) async throws -> NotificationDto {
        throw APIError.notFound
    }

    func markMultipleAsRead(_ request: MarkNotificationsReadRequest) async throws -> Int {
        return 0
    }

    func deleteNotification(id: UUID) async throws {
        throw APIError.notFound
    }

    func deleteAllReadNotifications() async throws -> Int {
        return 0
    }

    func getNotificationPreferences() async throws -> NotificationPreferences {
        return NotificationPreferences(
            preferences: [],
            quietHoursEnabled: false,
            quietHoursStart: nil,
            quietHoursEnd: nil
        )
    }

    func updateNotificationPreferences(_ request: UpdateNotificationPreferencesRequest) async throws -> NotificationPreferences {
        throw APIError.notFound
    }

    func updateQuietHours(_ request: UpdateQuietHoursRequest) async throws -> NotificationPreferences {
        throw APIError.notFound
    }

    func registerDevice(_ request: RegisterDeviceRequest) async throws -> DeviceTokenDto {
        throw APIError.notFound
    }

    func getDevices() async throws -> [DeviceTokenDto] {
        return []
    }

    func unregisterDevice(id: UUID) async throws {
        throw APIError.notFound
    }

    // MARK: - Gift Links (stub implementations)

    func getGiftLinks() async throws -> [GiftLinkDto] {
        return []
    }

    func getGiftLink(id: UUID) async throws -> GiftLinkDto {
        throw APIError.notFound
    }

    func createGiftLink(_ request: CreateGiftLinkRequest) async throws -> GiftLinkDto {
        throw APIError.notFound
    }

    func updateGiftLink(id: UUID, _ request: UpdateGiftLinkRequest) async throws -> GiftLinkDto {
        throw APIError.notFound
    }

    func deactivateGiftLink(id: UUID) async throws -> GiftLinkDto {
        throw APIError.notFound
    }

    func regenerateGiftLinkToken(id: UUID) async throws -> GiftLinkDto {
        throw APIError.notFound
    }

    func getGiftLinkStats(id: UUID) async throws -> GiftLinkStatsDto {
        throw APIError.notFound
    }

    // MARK: - Gifts (stub implementations)

    func getGifts(forChild childId: UUID) async throws -> [GiftDto] {
        return []
    }

    func getGift(id: UUID) async throws -> GiftDto {
        throw APIError.notFound
    }

    func approveGift(id: UUID, _ request: ApproveGiftRequest) async throws -> GiftDto {
        throw APIError.notFound
    }

    func rejectGift(id: UUID, _ request: RejectGiftRequest) async throws -> GiftDto {
        throw APIError.notFound
    }

    // MARK: - Thank You Notes (stub implementations)

    func getPendingThankYous() async throws -> [PendingThankYouDto] {
        return []
    }

    func getThankYouNote(forGiftId giftId: UUID) async throws -> ThankYouNoteDto {
        throw APIError.notFound
    }

    func createThankYouNote(forGiftId giftId: UUID, _ request: CreateThankYouNoteRequest) async throws -> ThankYouNoteDto {
        throw APIError.notFound
    }

    func updateThankYouNote(id: UUID, _ request: UpdateThankYouNoteRequest) async throws -> ThankYouNoteDto {
        throw APIError.notFound
    }

    func sendThankYouNote(forGiftId giftId: UUID) async throws -> ThankYouNoteDto {
        throw APIError.notFound
    }
}
