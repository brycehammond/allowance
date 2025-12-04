import Foundation

/// Protocol for APIService to enable dependency injection and testing
protocol APIServiceProtocol {
    func login(_ request: LoginRequest) async throws -> AuthResponse
    func register(_ request: RegisterRequest) async throws -> AuthResponse
    func logout() async throws
    func getChildren() async throws -> [Child]
    func getChild(id: UUID) async throws -> Child
    func updateChildSettings(childId: UUID, _ request: UpdateChildSettingsRequest) async throws -> UpdateChildSettingsResponse

    // MARK: - Savings
    func getSavingsAccounts(forChild childId: UUID) async throws -> [SavingsAccount]
    func getSavingsSummary(forChild childId: UUID) async throws -> SavingsAccountSummary
    func createSavingsAccount(_ request: CreateSavingsAccountRequest) async throws -> SavingsAccount
    func updateSavingsAccount(id: UUID, _ request: UpdateSavingsAccountRequest) async throws -> SavingsAccount
    func deleteSavingsAccount(id: UUID) async throws
    func depositToSavings(accountId: UUID, _ request: DepositRequest) async throws -> SavingsTransaction
    func withdrawFromSavings(accountId: UUID, _ request: WithdrawRequest) async throws -> SavingsTransaction
    func getSavingsTransactions(forAccount accountId: UUID) async throws -> [SavingsTransaction]
}
