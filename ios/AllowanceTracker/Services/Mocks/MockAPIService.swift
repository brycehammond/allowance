import Foundation

/// Mock API service for UI testing - returns predetermined test data for authentication
/// Most methods return empty arrays or throw errors as they're not needed for basic UI testing
final class MockAPIService: APIServiceProtocol {

    // MARK: - Test Data IDs

    static let testUserId = UUID(uuidString: "11111111-1111-1111-1111-111111111111")!
    static let testFamilyId = UUID(uuidString: "22222222-2222-2222-2222-222222222222")!
    static let testChildId = UUID(uuidString: "33333333-3333-3333-3333-333333333333")!

    // MARK: - Authentication (Core functionality for UI tests)

    func login(_ request: LoginRequest) async throws -> AuthResponse {
        // Accept the test credentials
        if request.email == "testuser@example.com" && request.password == "Password123@" {
            return AuthResponse(
                userId: Self.testUserId,
                email: request.email,
                firstName: "Test",
                lastName: "User",
                role: "Parent",
                familyId: Self.testFamilyId,
                familyName: "Test Family",
                token: "mock-jwt-token-for-ui-testing",
                expiresAt: Date().addingTimeInterval(86400)
            )
        }
        // Also accept child test credentials
        if request.email == "testchild@example.com" && request.password == "ChildPass123@" {
            return AuthResponse(
                userId: UUID(),
                email: request.email,
                firstName: "Test",
                lastName: "Child",
                role: "Child",
                familyId: Self.testFamilyId,
                familyName: "Test Family",
                token: "mock-jwt-token-child",
                expiresAt: Date().addingTimeInterval(86400)
            )
        }
        throw APIError.unauthorized
    }

    func register(_ request: RegisterRequest) async throws -> AuthResponse {
        return AuthResponse(
            userId: UUID(),
            email: request.email,
            firstName: request.firstName,
            lastName: request.lastName,
            role: request.role.rawValue,
            familyId: Self.testFamilyId,
            familyName: "Test Family",
            token: "mock-jwt-token-for-ui-testing",
            expiresAt: Date().addingTimeInterval(86400)
        )
    }

    func logout() async throws {}

    func refreshToken() async throws -> AuthResponse {
        return AuthResponse(
            userId: Self.testUserId,
            email: "testuser@example.com",
            firstName: "Test",
            lastName: "User",
            role: "Parent",
            familyId: Self.testFamilyId,
            familyName: "Test Family",
            token: "mock-jwt-token-refreshed",
            expiresAt: Date().addingTimeInterval(86400)
        )
    }

    func changePassword(_ request: ChangePasswordRequest) async throws -> PasswordMessageResponse {
        return PasswordMessageResponse(message: "Password changed successfully")
    }

    func forgotPassword(_ request: ForgotPasswordRequest) async throws -> PasswordMessageResponse {
        return PasswordMessageResponse(message: "Password reset email sent")
    }

    func resetPassword(_ request: ResetPasswordRequest) async throws -> PasswordMessageResponse {
        return PasswordMessageResponse(message: "Password reset successfully")
    }

    // MARK: - Children (Returns test data for dashboard)

    func getChildren() async throws -> [Child] {
        return [
            Child(
                id: Self.testChildId,
                firstName: "Test",
                lastName: "Child",
                weeklyAllowance: 10.00,
                currentBalance: 150.00,
                savingsBalance: 50.00,
                lastAllowanceDate: Date().addingTimeInterval(-604800),
                allowanceDay: .friday,
                savingsAccountEnabled: true,
                savingsTransferType: .percentage,
                savingsTransferPercentage: 10,
                savingsTransferAmount: nil,
                savingsBalanceVisibleToChild: true,
                allowDebt: false
            )
        ]
    }

    func getChild(id: UUID) async throws -> Child {
        return (try await getChildren()).first!
    }

    func createChild(_ request: CreateChildRequest) async throws -> Child {
        return Child(
            id: UUID(),
            firstName: request.firstName,
            lastName: request.lastName,
            weeklyAllowance: request.weeklyAllowance,
            currentBalance: request.initialBalance ?? 0,
            savingsBalance: request.initialSavingsBalance ?? 0,
            lastAllowanceDate: nil,
            allowanceDay: nil,
            savingsAccountEnabled: request.savingsAccountEnabled,
            savingsTransferType: request.savingsTransferType,
            savingsTransferPercentage: request.savingsTransferPercentage,
            savingsTransferAmount: request.savingsTransferAmount,
            savingsBalanceVisibleToChild: true,
            allowDebt: false
        )
    }

    func updateChildSettings(childId: UUID, _ request: UpdateChildSettingsRequest) async throws -> UpdateChildSettingsResponse {
        return UpdateChildSettingsResponse(
            childId: childId,
            firstName: "Test",
            lastName: "Child",
            weeklyAllowance: request.weeklyAllowance,
            allowanceDay: request.allowanceDay,
            savingsAccountEnabled: request.savingsAccountEnabled,
            savingsTransferType: request.savingsTransferType.rawValue,
            savingsTransferPercentage: Int(truncating: (request.savingsTransferPercentage ?? 0) as NSDecimalNumber),
            savingsTransferAmount: request.savingsTransferAmount ?? 0,
            allowDebt: request.allowDebt ?? false,
            message: "Settings updated"
        )
    }

    // MARK: - Transactions (Returns sample data)

    func getTransactions(forChild childId: UUID, limit: Int) async throws -> [Transaction] {
        return [
            Transaction(
                id: UUID(),
                childId: childId,
                amount: 10.00,
                type: .credit,
                category: "Allowance",
                description: "Weekly Allowance",
                notes: nil,
                balanceAfter: 160.00,
                createdAt: Date(),
                createdByName: "Test User"
            )
        ]
    }

    func createTransaction(_ request: CreateTransactionRequest) async throws -> Transaction {
        return Transaction(
            id: UUID(),
            childId: request.childId,
            amount: request.amount,
            type: request.type,
            category: request.category,
            description: request.description,
            notes: request.notes,
            balanceAfter: 160.00,
            createdAt: Date(),
            createdByName: "Test User"
        )
    }

    func getBalance(forChild childId: UUID) async throws -> Decimal { return 150.00 }

    // MARK: - Wish List (Returns empty)

    func getWishList(forChild childId: UUID) async throws -> [WishListItem] { return [] }
    func createWishListItem(_ request: CreateWishListItemRequest) async throws -> WishListItem { throw APIError.notFound }
    func updateWishListItem(forChild childId: UUID, id: UUID, _ request: UpdateWishListItemRequest) async throws -> WishListItem { throw APIError.notFound }
    func deleteWishListItem(forChild childId: UUID, id: UUID) async throws {}
    func markWishListItemAsPurchased(forChild childId: UUID, id: UUID) async throws -> WishListItem { throw APIError.notFound }

    // MARK: - Analytics (Returns empty/defaults)

    func getBalanceHistory(forChild childId: UUID, days: Int) async throws -> [BalancePoint] { return [] }
    func getIncomeVsSpending(forChild childId: UUID) async throws -> IncomeSpendingSummary {
        return IncomeSpendingSummary(
            totalIncome: 100,
            totalSpending: 50,
            netSavings: 50,
            incomeTransactionCount: 5,
            spendingTransactionCount: 3,
            savingsRate: 50
        )
    }
    func getSpendingBreakdown(forChild childId: UUID) async throws -> [CategoryBreakdown] { return [] }
    func getMonthlyComparison(forChild childId: UUID, months: Int) async throws -> [MonthlyComparison] { return [] }

    // MARK: - Savings (Returns defaults)

    func getSavingsSummary(forChild childId: UUID) async throws -> SavingsAccountSummary {
        return SavingsAccountSummary(
            childId: childId,
            isEnabled: true,
            currentBalance: 50,
            transferType: "Percentage",
            transferAmount: 0,
            transferPercentage: 10,
            totalTransactions: 5,
            totalDeposited: 100,
            totalWithdrawn: 50,
            lastTransactionDate: Date(),
            configDescription: "10% of allowance",
            balanceHidden: false
        )
    }

    func getSavingsHistory(forChild childId: UUID, limit: Int) async throws -> [SavingsTransaction] { return [] }
    func depositToSavings(_ request: DepositToSavingsRequest) async throws -> SavingsTransaction { throw APIError.notFound }
    func withdrawFromSavings(_ request: WithdrawFromSavingsRequest) async throws -> SavingsTransaction { throw APIError.notFound }

    // MARK: - Parent Invites (Returns empty)

    func sendParentInvite(_ request: SendParentInviteRequest) async throws -> ParentInviteResponse { throw APIError.notFound }
    func getPendingInvites() async throws -> [PendingInvite] { return [] }
    func cancelInvite(inviteId: String) async throws {}
    func resendInvite(inviteId: String) async throws -> ParentInviteResponse { throw APIError.notFound }

    // MARK: - Badges (Returns empty)

    func getAllBadges(category: BadgeCategory?, includeSecret: Bool) async throws -> [BadgeDto] { return [] }
    func getChildBadges(forChild childId: UUID, category: BadgeCategory?, newOnly: Bool) async throws -> [ChildBadgeDto] { return [] }
    func getBadgeProgress(forChild childId: UUID) async throws -> [BadgeProgressDto] { return [] }
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
    func toggleBadgeDisplay(forChild childId: UUID, badgeId: UUID, _ request: UpdateBadgeDisplayRequest) async throws -> ChildBadgeDto { throw APIError.notFound }
    func markBadgesSeen(forChild childId: UUID, _ request: MarkBadgesSeenRequest) async throws {}
    func getChildPoints(forChild childId: UUID) async throws -> ChildPointsDto {
        return ChildPointsDto(
            totalPoints: 0,
            availablePoints: 0,
            spentPoints: 0,
            badgesEarned: 0,
            rewardsUnlocked: 0
        )
    }

    // MARK: - Rewards (Returns empty)

    func getAvailableRewards(type: RewardType?, forChild childId: UUID?) async throws -> [RewardDto] { return [] }
    func getChildRewards(forChild childId: UUID) async throws -> [RewardDto] { return [] }
    func unlockReward(forChild childId: UUID, rewardId: UUID) async throws -> RewardDto { throw APIError.notFound }
    func equipReward(forChild childId: UUID, rewardId: UUID) async throws -> RewardDto { throw APIError.notFound }
    func unequipReward(forChild childId: UUID, rewardId: UUID) async throws {}

    // MARK: - Tasks/Chores (Returns empty)

    func getTasks(childId: UUID?, status: ChoreTaskStatus?, isRecurring: Bool?) async throws -> [ChoreTask] { return [] }
    func getTask(id: UUID) async throws -> ChoreTask { throw APIError.notFound }
    func createTask(_ request: CreateTaskRequest) async throws -> ChoreTask { throw APIError.notFound }
    func updateTask(id: UUID, _ request: UpdateTaskRequest) async throws -> ChoreTask { throw APIError.notFound }
    func archiveTask(id: UUID) async throws {}
    func completeTask(id: UUID, notes: String?, photoData: Data?, photoFileName: String?) async throws -> TaskCompletion { throw APIError.notFound }
    func getTaskCompletions(taskId: UUID, status: CompletionStatus?) async throws -> [TaskCompletion] { return [] }
    func getPendingApprovals() async throws -> [TaskCompletion] { return [] }
    func reviewCompletion(id: UUID, _ request: ReviewCompletionRequest) async throws -> TaskCompletion { throw APIError.notFound }

    // MARK: - Savings Goals (Returns empty)

    func getSavingsGoals(forChild childId: UUID, status: GoalStatus?, includeCompleted: Bool) async throws -> [SavingsGoalDto] { return [] }
    func getSavingsGoal(id: UUID) async throws -> SavingsGoalDto { throw APIError.notFound }
    func createSavingsGoal(_ request: CreateSavingsGoalRequest) async throws -> SavingsGoalDto { throw APIError.notFound }
    func updateSavingsGoal(id: UUID, _ request: UpdateSavingsGoalRequest) async throws -> SavingsGoalDto { throw APIError.notFound }
    func deleteSavingsGoal(id: UUID) async throws {}
    func pauseSavingsGoal(id: UUID) async throws -> SavingsGoalDto { throw APIError.notFound }
    func resumeSavingsGoal(id: UUID) async throws -> SavingsGoalDto { throw APIError.notFound }
    func contributeToGoal(goalId: UUID, _ request: ContributeToGoalRequest) async throws -> GoalProgressEventDto { throw APIError.notFound }
    func withdrawFromGoal(goalId: UUID, _ request: WithdrawFromGoalRequest) async throws -> GoalContributionDto { throw APIError.notFound }
    func getGoalContributions(goalId: UUID, type: ContributionType?) async throws -> [GoalContributionDto] { return [] }
    func markGoalAsPurchased(goalId: UUID, _ request: MarkGoalPurchasedRequest?) async throws -> SavingsGoalDto { throw APIError.notFound }
    func createMatchingRule(goalId: UUID, _ request: CreateMatchingRuleRequest) async throws -> MatchingRuleDto { throw APIError.notFound }
    func getMatchingRule(goalId: UUID) async throws -> MatchingRuleDto? { return nil }
    func updateMatchingRule(goalId: UUID, _ request: UpdateMatchingRuleRequest) async throws -> MatchingRuleDto { throw APIError.notFound }
    func deleteMatchingRule(goalId: UUID) async throws {}
    func createGoalChallenge(goalId: UUID, _ request: CreateGoalChallengeRequest) async throws -> GoalChallengeDto { throw APIError.notFound }
    func getGoalChallenge(goalId: UUID) async throws -> GoalChallengeDto? { return nil }
    func cancelGoalChallenge(goalId: UUID) async throws {}
    func getChildChallenges(forChild childId: UUID) async throws -> [GoalChallengeDto] { return [] }

    // MARK: - Notifications (Returns empty)

    func getNotifications(page: Int, pageSize: Int, unreadOnly: Bool, type: NotificationType?) async throws -> NotificationListResponse {
        return NotificationListResponse(notifications: [], unreadCount: 0, totalCount: 0, hasMore: false)
    }
    func getUnreadCount() async throws -> Int { return 0 }
    func getNotification(id: UUID) async throws -> NotificationDto { throw APIError.notFound }
    func markNotificationAsRead(id: UUID) async throws -> NotificationDto { throw APIError.notFound }
    func markMultipleAsRead(_ request: MarkNotificationsReadRequest) async throws -> Int { return 0 }
    func deleteNotification(id: UUID) async throws {}
    func deleteAllReadNotifications() async throws -> Int { return 0 }
    func getNotificationPreferences() async throws -> NotificationPreferences {
        return NotificationPreferences(preferences: [], quietHoursEnabled: false, quietHoursStart: nil, quietHoursEnd: nil)
    }
    func updateNotificationPreferences(_ request: UpdateNotificationPreferencesRequest) async throws -> NotificationPreferences {
        return NotificationPreferences(preferences: [], quietHoursEnabled: false, quietHoursStart: nil, quietHoursEnd: nil)
    }
    func updateQuietHours(_ request: UpdateQuietHoursRequest) async throws -> NotificationPreferences {
        return NotificationPreferences(preferences: [], quietHoursEnabled: request.enabled, quietHoursStart: request.startTime, quietHoursEnd: request.endTime)
    }
    func registerDevice(_ request: RegisterDeviceRequest) async throws -> DeviceTokenDto {
        return DeviceTokenDto(id: UUID(), platform: request.platform, deviceName: request.deviceName, isActive: true, createdAt: Date(), lastUsedAt: nil)
    }
    func getDevices() async throws -> [DeviceTokenDto] { return [] }
    func unregisterDevice(id: UUID) async throws {}

    // MARK: - Gift Links (Returns empty)

    func getGiftLinks() async throws -> [GiftLinkDto] { return [] }
    func getGiftLink(id: UUID) async throws -> GiftLinkDto { throw APIError.notFound }
    func createGiftLink(_ request: CreateGiftLinkRequest) async throws -> GiftLinkDto { throw APIError.notFound }
    func updateGiftLink(id: UUID, _ request: UpdateGiftLinkRequest) async throws -> GiftLinkDto { throw APIError.notFound }
    func deactivateGiftLink(id: UUID) async throws -> GiftLinkDto { throw APIError.notFound }
    func regenerateGiftLinkToken(id: UUID) async throws -> GiftLinkDto { throw APIError.notFound }
    func getGiftLinkStats(id: UUID) async throws -> GiftLinkStatsDto {
        return GiftLinkStatsDto(
            giftLinkId: id,
            totalGifts: 0,
            pendingGifts: 0,
            approvedGifts: 0,
            rejectedGifts: 0,
            expiredGifts: 0,
            totalAmountReceived: 0,
            averageGiftAmount: nil
        )
    }

    // MARK: - Gifts (Returns empty)

    func getGifts(forChild childId: UUID) async throws -> [GiftDto] { return [] }
    func getGift(id: UUID) async throws -> GiftDto { throw APIError.notFound }
    func approveGift(id: UUID, _ request: ApproveGiftRequest) async throws -> GiftDto { throw APIError.notFound }
    func rejectGift(id: UUID, _ request: RejectGiftRequest) async throws -> GiftDto { throw APIError.notFound }

    // MARK: - Thank You Notes (Returns empty)

    func getPendingThankYous() async throws -> [PendingThankYouDto] { return [] }
    func getThankYouNote(forGiftId giftId: UUID) async throws -> ThankYouNoteDto { throw APIError.notFound }
    func createThankYouNote(forGiftId giftId: UUID, _ request: CreateThankYouNoteRequest) async throws -> ThankYouNoteDto { throw APIError.notFound }
    func updateThankYouNote(id: UUID, _ request: UpdateThankYouNoteRequest) async throws -> ThankYouNoteDto { throw APIError.notFound }
    func sendThankYouNote(forGiftId giftId: UUID) async throws -> ThankYouNoteDto { throw APIError.notFound }
}
