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

    // MARK: - Children
    func getChildren() async throws -> [Child]
    func getChild(id: UUID) async throws -> Child
    func createChild(_ request: CreateChildRequest) async throws -> Child
    func updateChildSettings(childId: UUID, _ request: UpdateChildSettingsRequest) async throws -> UpdateChildSettingsResponse

    // MARK: - Transactions
    func getTransactions(forChild childId: UUID, limit: Int) async throws -> [Transaction]
    func createTransaction(_ request: CreateTransactionRequest) async throws -> Transaction
    func getBalance(forChild childId: UUID) async throws -> Decimal

    // MARK: - Wish List
    func getWishList(forChild childId: UUID) async throws -> [WishListItem]
    func createWishListItem(_ request: CreateWishListItemRequest) async throws -> WishListItem
    func updateWishListItem(forChild childId: UUID, id: UUID, _ request: UpdateWishListItemRequest) async throws -> WishListItem
    func deleteWishListItem(forChild childId: UUID, id: UUID) async throws
    func markWishListItemAsPurchased(forChild childId: UUID, id: UUID) async throws -> WishListItem

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
}
