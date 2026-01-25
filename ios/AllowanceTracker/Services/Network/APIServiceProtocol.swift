import Foundation

/// Protocol for APIService to enable dependency injection and testing
protocol APIServiceProtocol {
    // MARK: - Authentication
    func login(_ request: LoginRequest) async throws -> AuthResponse
    func register(_ request: RegisterRequest) async throws -> AuthResponse
    func logout() async throws
    func refreshToken() async throws -> AuthResponse
    func changePassword(_ request: ChangePasswordRequest) async throws -> PasswordMessageResponse
    func forgotPassword(_ request: ForgotPasswordRequest) async throws -> PasswordMessageResponse
    func resetPassword(_ request: ResetPasswordRequest) async throws -> PasswordMessageResponse
    func deleteAccount() async throws

    // MARK: - Children
    func getChildren() async throws -> [Child]
    func getChild(id: UUID) async throws -> Child
    func createChild(_ request: CreateChildRequest) async throws -> Child
    func updateChildSettings(childId: UUID, _ request: UpdateChildSettingsRequest) async throws -> UpdateChildSettingsResponse

    // MARK: - Transactions
    func getTransactions(forChild childId: UUID, limit: Int) async throws -> [Transaction]
    func createTransaction(_ request: CreateTransactionRequest) async throws -> Transaction
    func getBalance(forChild childId: UUID) async throws -> Decimal

    // MARK: - Analytics
    func getBalanceHistory(forChild childId: UUID, days: Int) async throws -> [BalancePoint]
    func getIncomeVsSpending(forChild childId: UUID) async throws -> IncomeSpendingSummary
    func getSpendingBreakdown(forChild childId: UUID) async throws -> [CategoryBreakdown]
    func getMonthlyComparison(forChild childId: UUID, months: Int) async throws -> [MonthlyComparison]

    // MARK: - Savings
    func getSavingsSummary(forChild childId: UUID) async throws -> SavingsAccountSummary
    func getSavingsHistory(forChild childId: UUID, limit: Int) async throws -> [SavingsTransaction]
    func depositToSavings(_ request: DepositToSavingsRequest) async throws -> SavingsTransaction
    func withdrawFromSavings(_ request: WithdrawFromSavingsRequest) async throws -> SavingsTransaction

    // MARK: - Parent Invites
    func sendParentInvite(_ request: SendParentInviteRequest) async throws -> ParentInviteResponse
    func getPendingInvites() async throws -> [PendingInvite]
    func cancelInvite(inviteId: String) async throws
    func resendInvite(inviteId: String) async throws -> ParentInviteResponse

    // MARK: - Badges
    func getAllBadges(category: BadgeCategory?, includeSecret: Bool) async throws -> [BadgeDto]
    func getChildBadges(forChild childId: UUID, category: BadgeCategory?, newOnly: Bool) async throws -> [ChildBadgeDto]
    func getBadgeProgress(forChild childId: UUID) async throws -> [BadgeProgressDto]
    func getAchievementSummary(forChild childId: UUID) async throws -> AchievementSummaryDto
    func toggleBadgeDisplay(forChild childId: UUID, badgeId: UUID, _ request: UpdateBadgeDisplayRequest) async throws -> ChildBadgeDto
    func markBadgesSeen(forChild childId: UUID, _ request: MarkBadgesSeenRequest) async throws
    func getChildPoints(forChild childId: UUID) async throws -> ChildPointsDto

    // MARK: - Rewards
    func getAvailableRewards(type: RewardType?, forChild childId: UUID?) async throws -> [RewardDto]
    func getChildRewards(forChild childId: UUID) async throws -> [RewardDto]
    func unlockReward(forChild childId: UUID, rewardId: UUID) async throws -> RewardDto
    func equipReward(forChild childId: UUID, rewardId: UUID) async throws -> RewardDto
    func unequipReward(forChild childId: UUID, rewardId: UUID) async throws

    // MARK: - Tasks/Chores
    func getTasks(childId: UUID?, status: ChoreTaskStatus?, isRecurring: Bool?) async throws -> [ChoreTask]
    func getTask(id: UUID) async throws -> ChoreTask
    func createTask(_ request: CreateTaskRequest) async throws -> ChoreTask
    func updateTask(id: UUID, _ request: UpdateTaskRequest) async throws -> ChoreTask
    func archiveTask(id: UUID) async throws
    func completeTask(id: UUID, notes: String?, photoData: Data?, photoFileName: String?) async throws -> TaskCompletion
    func getTaskCompletions(taskId: UUID, status: CompletionStatus?) async throws -> [TaskCompletion]
    func getPendingApprovals() async throws -> [TaskCompletion]
    func reviewCompletion(id: UUID, _ request: ReviewCompletionRequest) async throws -> TaskCompletion

    // MARK: - Savings Goals
    func getSavingsGoals(forChild childId: UUID, status: GoalStatus?, includeCompleted: Bool) async throws -> [SavingsGoalDto]
    func getSavingsGoal(id: UUID) async throws -> SavingsGoalDto
    func createSavingsGoal(_ request: CreateSavingsGoalRequest) async throws -> SavingsGoalDto
    func updateSavingsGoal(id: UUID, _ request: UpdateSavingsGoalRequest) async throws -> SavingsGoalDto
    func deleteSavingsGoal(id: UUID) async throws
    func pauseSavingsGoal(id: UUID) async throws -> SavingsGoalDto
    func resumeSavingsGoal(id: UUID) async throws -> SavingsGoalDto
    func contributeToGoal(goalId: UUID, _ request: ContributeToGoalRequest) async throws -> GoalProgressEventDto
    func withdrawFromGoal(goalId: UUID, _ request: WithdrawFromGoalRequest) async throws -> GoalContributionDto
    func getGoalContributions(goalId: UUID, type: ContributionType?) async throws -> [GoalContributionDto]
    func markGoalAsPurchased(goalId: UUID, _ request: MarkGoalPurchasedRequest?) async throws -> SavingsGoalDto
    func createMatchingRule(goalId: UUID, _ request: CreateMatchingRuleRequest) async throws -> MatchingRuleDto
    func getMatchingRule(goalId: UUID) async throws -> MatchingRuleDto?
    func updateMatchingRule(goalId: UUID, _ request: UpdateMatchingRuleRequest) async throws -> MatchingRuleDto
    func deleteMatchingRule(goalId: UUID) async throws
    func createGoalChallenge(goalId: UUID, _ request: CreateGoalChallengeRequest) async throws -> GoalChallengeDto
    func getGoalChallenge(goalId: UUID) async throws -> GoalChallengeDto?
    func cancelGoalChallenge(goalId: UUID) async throws
    func getChildChallenges(forChild childId: UUID) async throws -> [GoalChallengeDto]

    // MARK: - Notifications
    func getNotifications(page: Int, pageSize: Int, unreadOnly: Bool, type: NotificationType?) async throws -> NotificationListResponse
    func getUnreadCount() async throws -> Int
    func getNotification(id: UUID) async throws -> NotificationDto
    func markNotificationAsRead(id: UUID) async throws -> NotificationDto
    func markMultipleAsRead(_ request: MarkNotificationsReadRequest) async throws -> Int
    func deleteNotification(id: UUID) async throws
    func deleteAllReadNotifications() async throws -> Int
    func getNotificationPreferences() async throws -> NotificationPreferences
    func updateNotificationPreferences(_ request: UpdateNotificationPreferencesRequest) async throws -> NotificationPreferences
    func updateQuietHours(_ request: UpdateQuietHoursRequest) async throws -> NotificationPreferences
    func registerDevice(_ request: RegisterDeviceRequest) async throws -> DeviceTokenDto
    func getDevices() async throws -> [DeviceTokenDto]
    func unregisterDevice(id: UUID) async throws

    // MARK: - Gift Links
    func getGiftLinks() async throws -> [GiftLinkDto]
    func getGiftLink(id: UUID) async throws -> GiftLinkDto
    func createGiftLink(_ request: CreateGiftLinkRequest) async throws -> GiftLinkDto
    func updateGiftLink(id: UUID, _ request: UpdateGiftLinkRequest) async throws -> GiftLinkDto
    func deactivateGiftLink(id: UUID) async throws -> GiftLinkDto
    func regenerateGiftLinkToken(id: UUID) async throws -> GiftLinkDto
    func getGiftLinkStats(id: UUID) async throws -> GiftLinkStatsDto

    // MARK: - Gifts
    func getGifts(forChild childId: UUID) async throws -> [GiftDto]
    func getGift(id: UUID) async throws -> GiftDto
    func approveGift(id: UUID, _ request: ApproveGiftRequest) async throws -> GiftDto
    func rejectGift(id: UUID, _ request: RejectGiftRequest) async throws -> GiftDto

    // MARK: - Thank You Notes
    func getPendingThankYous() async throws -> [PendingThankYouDto]
    func getThankYouNote(forGiftId giftId: UUID) async throws -> ThankYouNoteDto
    func createThankYouNote(forGiftId giftId: UUID, _ request: CreateThankYouNoteRequest) async throws -> ThankYouNoteDto
    func updateThankYouNote(id: UUID, _ request: UpdateThankYouNoteRequest) async throws -> ThankYouNoteDto
    func sendThankYouNote(forGiftId giftId: UUID) async throws -> ThankYouNoteDto
}
