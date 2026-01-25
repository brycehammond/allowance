import Foundation

/// Mock API service for UI testing - provides stateful in-memory data management.
/// Supports CRUD operations that persist during the test session.
///
/// ## Test Scenarios
/// The mock can be configured with different data scenarios:
/// - `.populated`: Full test data for comprehensive UI testing (default)
/// - `.empty`: Empty state for testing empty state UI
/// - `.singleChild`: Only one child for simpler test flows
///
/// ## Usage
/// The mock maintains state across API calls, so if you create a transaction,
/// it will appear in subsequent `getTransactions` calls.
final class MockAPIService: APIServiceProtocol, @unchecked Sendable {

    // MARK: - Test Scenarios

    enum TestScenario {
        case populated   // Full test data
        case empty       // Empty state (no children, transactions, etc.)
        case singleChild // One child with minimal data
    }

    // MARK: - Singleton for Shared State

    static let shared = MockAPIService()

    // MARK: - Configuration

    var scenario: TestScenario = .populated

    // MARK: - Test Data IDs (Fixed for consistent testing)

    static let testUserId = UUID(uuidString: "11111111-1111-1111-1111-111111111111")!
    static let testFamilyId = UUID(uuidString: "22222222-2222-2222-2222-222222222222")!
    static let testChildId = UUID(uuidString: "33333333-3333-3333-3333-333333333333")!
    static let testChild2Id = UUID(uuidString: "44444444-4444-4444-4444-444444444444")!

    // MARK: - In-Memory State

    private var children: [Child] = []
    private var transactions: [UUID: [Transaction]] = [:] // childId -> transactions
    private var wishListItems: [UUID: [WishListItem]] = [:] // childId -> items
    private var savingsTransactions: [UUID: [SavingsTransaction]] = [:] // childId -> transactions
    private var currentUser: AuthResponse?
    private var isLoggedIn = false

    // MARK: - Initialization

    init(scenario: TestScenario = .populated) {
        self.scenario = scenario
        resetToScenario(scenario)
    }

    /// Reset all data to the specified scenario
    func resetToScenario(_ scenario: TestScenario) {
        self.scenario = scenario
        isLoggedIn = false
        currentUser = nil

        switch scenario {
        case .populated:
            setupPopulatedData()
        case .empty:
            setupEmptyData()
        case .singleChild:
            setupSingleChildData()
        }
    }

    /// Reset to default populated state
    func reset() {
        resetToScenario(.populated)
    }

    // MARK: - Data Setup

    private func setupPopulatedData() {
        // Create two children
        let child1 = Child(
            id: Self.testChildId,
            firstName: "Emma",
            lastName: "Test",
            weeklyAllowance: 10.00,
            currentBalance: 150.00,
            savingsBalance: 75.00,
            lastAllowanceDate: Date().addingTimeInterval(-604800), // 1 week ago
            allowanceDay: .friday,
            savingsAccountEnabled: true,
            savingsTransferType: .percentage,
            savingsTransferPercentage: 10,
            savingsTransferAmount: nil,
            savingsBalanceVisibleToChild: true,
            allowDebt: false
        )

        let child2 = Child(
            id: Self.testChild2Id,
            firstName: "Jake",
            lastName: "Test",
            weeklyAllowance: 15.00,
            currentBalance: 85.50,
            savingsBalance: 120.00,
            lastAllowanceDate: Date().addingTimeInterval(-604800),
            allowanceDay: .saturday,
            savingsAccountEnabled: true,
            savingsTransferType: .fixedAmount,
            savingsTransferPercentage: nil,
            savingsTransferAmount: 5.00,
            savingsBalanceVisibleToChild: true,
            allowDebt: false
        )

        children = [child1, child2]

        // Create transactions for child 1
        let now = Date()
        transactions[child1.id] = [
            Transaction(
                id: UUID(),
                childId: child1.id,
                amount: 10.00,
                type: .credit,
                category: "Allowance",
                description: "Weekly Allowance",
                notes: nil,
                balanceAfter: 150.00,
                createdAt: now,
                createdByName: "Test Parent"
            ),
            Transaction(
                id: UUID(),
                childId: child1.id,
                amount: -5.50,
                type: .debit,
                category: "Entertainment",
                description: "Comic book",
                notes: "Bought at the bookstore",
                balanceAfter: 140.00,
                createdAt: now.addingTimeInterval(-86400),
                createdByName: "Test Parent"
            ),
            Transaction(
                id: UUID(),
                childId: child1.id,
                amount: -12.00,
                type: .debit,
                category: "Toys",
                description: "Action figure",
                notes: nil,
                balanceAfter: 145.50,
                createdAt: now.addingTimeInterval(-172800),
                createdByName: "Test Parent"
            ),
            Transaction(
                id: UUID(),
                childId: child1.id,
                amount: 10.00,
                type: .credit,
                category: "Allowance",
                description: "Weekly Allowance",
                notes: nil,
                balanceAfter: 157.50,
                createdAt: now.addingTimeInterval(-604800),
                createdByName: "System"
            ),
            Transaction(
                id: UUID(),
                childId: child1.id,
                amount: 25.00,
                type: .credit,
                category: "Gift",
                description: "Birthday money from Grandma",
                notes: "Happy Birthday!",
                balanceAfter: 147.50,
                createdAt: now.addingTimeInterval(-1209600),
                createdByName: "Test Parent"
            )
        ]

        // Create transactions for child 2
        transactions[child2.id] = [
            Transaction(
                id: UUID(),
                childId: child2.id,
                amount: 15.00,
                type: .credit,
                category: "Allowance",
                description: "Weekly Allowance",
                notes: nil,
                balanceAfter: 85.50,
                createdAt: now,
                createdByName: "System"
            ),
            Transaction(
                id: UUID(),
                childId: child2.id,
                amount: -20.00,
                type: .debit,
                category: "Games",
                description: "Video game",
                notes: nil,
                balanceAfter: 70.50,
                createdAt: now.addingTimeInterval(-259200),
                createdByName: "Test Parent"
            )
        ]

        // Create wish list items for child 1
        wishListItems[child1.id] = [
            WishListItem(
                id: UUID(),
                childId: child1.id,
                name: "LEGO Star Wars Set",
                price: 89.99,
                url: "https://example.com/lego",
                notes: "The Millennium Falcon one!",
                isPurchased: false,
                purchasedAt: nil,
                createdAt: now.addingTimeInterval(-86400),
                canAfford: true
            ),
            WishListItem(
                id: UUID(),
                childId: child1.id,
                name: "New Skateboard",
                price: 45.00,
                url: nil,
                notes: nil,
                isPurchased: false,
                purchasedAt: nil,
                createdAt: now.addingTimeInterval(-172800),
                canAfford: true
            ),
            WishListItem(
                id: UUID(),
                childId: child1.id,
                name: "Books Collection",
                price: 35.00,
                url: nil,
                notes: "Harry Potter series",
                isPurchased: true,
                purchasedAt: now.addingTimeInterval(-604800),
                createdAt: now.addingTimeInterval(-1209600),
                canAfford: true
            )
        ]

        // Create wish list items for child 2
        wishListItems[child2.id] = [
            WishListItem(
                id: UUID(),
                childId: child2.id,
                name: "Basketball",
                price: 29.99,
                url: nil,
                notes: nil,
                isPurchased: false,
                purchasedAt: nil,
                createdAt: now.addingTimeInterval(-86400),
                canAfford: true
            )
        ]

        // Create savings transactions
        savingsTransactions[child1.id] = [
            SavingsTransaction(
                id: UUID(),
                childId: child1.id,
                type: .deposit,
                amount: 1.00,
                description: "10% of allowance",
                balanceAfter: 75.00,
                createdAt: now,
                createdById: Self.testUserId,
                createdByName: "System"
            ),
            SavingsTransaction(
                id: UUID(),
                childId: child1.id,
                type: .deposit,
                amount: 1.00,
                description: "10% of allowance",
                balanceAfter: 74.00,
                createdAt: now.addingTimeInterval(-604800),
                createdById: Self.testUserId,
                createdByName: "System"
            ),
            SavingsTransaction(
                id: UUID(),
                childId: child1.id,
                type: .deposit,
                amount: 25.00,
                description: "Extra savings deposit",
                balanceAfter: 73.00,
                createdAt: now.addingTimeInterval(-1209600),
                createdById: Self.testUserId,
                createdByName: "Test Parent"
            )
        ]

        savingsTransactions[child2.id] = [
            SavingsTransaction(
                id: UUID(),
                childId: child2.id,
                type: .deposit,
                amount: 5.00,
                description: "Weekly savings",
                balanceAfter: 120.00,
                createdAt: now,
                createdById: Self.testUserId,
                createdByName: "System"
            )
        ]
    }

    private func setupEmptyData() {
        children = []
        transactions = [:]
        wishListItems = [:]
        savingsTransactions = [:]
    }

    private func setupSingleChildData() {
        let child = Child(
            id: Self.testChildId,
            firstName: "Test",
            lastName: "Child",
            weeklyAllowance: 10.00,
            currentBalance: 100.00,
            savingsBalance: 50.00,
            lastAllowanceDate: nil,
            allowanceDay: nil,
            savingsAccountEnabled: true,
            savingsTransferType: .percentage,
            savingsTransferPercentage: 10,
            savingsTransferAmount: nil,
            savingsBalanceVisibleToChild: true,
            allowDebt: false
        )

        children = [child]
        transactions[child.id] = []
        wishListItems[child.id] = []
        savingsTransactions[child.id] = []
    }

    // MARK: - Authentication

    func login(_ request: LoginRequest) async throws -> AuthResponse {
        // Accept parent test credentials
        if request.email == "testuser@example.com" && request.password == "Password123@" {
            let response = AuthResponse(
                userId: Self.testUserId,
                email: request.email,
                firstName: "Test",
                lastName: "User",
                role: "Parent",
                familyId: Self.testFamilyId,
                familyName: "Test Family",
                token: "mock-jwt-token-for-ui-testing-\(UUID().uuidString)",
                expiresAt: Date().addingTimeInterval(86400)
            )
            currentUser = response
            isLoggedIn = true
            return response
        }

        // Accept child test credentials
        if request.email == "testchild@example.com" && request.password == "ChildPass123@" {
            let response = AuthResponse(
                userId: UUID(),
                email: request.email,
                firstName: "Emma",
                lastName: "Test",
                role: "Child",
                familyId: Self.testFamilyId,
                familyName: "Test Family",
                token: "mock-jwt-token-child-\(UUID().uuidString)",
                expiresAt: Date().addingTimeInterval(86400)
            )
            currentUser = response
            isLoggedIn = true
            return response
        }

        throw APIError.unauthorized
    }

    func register(_ request: RegisterRequest) async throws -> AuthResponse {
        let response = AuthResponse(
            userId: UUID(),
            email: request.email,
            firstName: request.firstName,
            lastName: request.lastName,
            role: request.role.rawValue,
            familyId: Self.testFamilyId,
            familyName: "Test Family",
            token: "mock-jwt-token-new-user-\(UUID().uuidString)",
            expiresAt: Date().addingTimeInterval(86400)
        )
        currentUser = response
        isLoggedIn = true
        return response
    }

    func logout() async throws {
        currentUser = nil
        isLoggedIn = false
    }

    func refreshToken() async throws -> AuthResponse {
        guard let user = currentUser else {
            throw APIError.unauthorized
        }
        let response = AuthResponse(
            userId: user.userId,
            email: user.email,
            firstName: user.firstName,
            lastName: user.lastName,
            role: user.role,
            familyId: user.familyId,
            familyName: user.familyName,
            token: "mock-jwt-token-refreshed-\(UUID().uuidString)",
            expiresAt: Date().addingTimeInterval(86400)
        )
        currentUser = response
        return response
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

    func deleteAccount() async throws {
        currentUser = nil
        isLoggedIn = false
    }

    // MARK: - Children

    func getChildren() async throws -> [Child] {
        return children
    }

    func getChild(id: UUID) async throws -> Child {
        guard let child = children.first(where: { $0.id == id }) else {
            throw APIError.notFound
        }
        return child
    }

    func createChild(_ request: CreateChildRequest) async throws -> Child {
        let child = Child(
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
        children.append(child)
        transactions[child.id] = []
        wishListItems[child.id] = []
        savingsTransactions[child.id] = []
        return child
    }

    func updateChildSettings(childId: UUID, _ request: UpdateChildSettingsRequest) async throws -> UpdateChildSettingsResponse {
        guard let index = children.firstIndex(where: { $0.id == childId }) else {
            throw APIError.notFound
        }

        let child = children[index]
        let updated = Child(
            id: child.id,
            firstName: child.firstName,
            lastName: child.lastName,
            weeklyAllowance: request.weeklyAllowance,
            currentBalance: child.currentBalance,
            savingsBalance: child.savingsBalance,
            lastAllowanceDate: child.lastAllowanceDate,
            allowanceDay: request.allowanceDay,
            savingsAccountEnabled: request.savingsAccountEnabled,
            savingsTransferType: request.savingsTransferType,
            savingsTransferPercentage: request.savingsTransferPercentage,
            savingsTransferAmount: request.savingsTransferAmount,
            savingsBalanceVisibleToChild: child.savingsBalanceVisibleToChild,
            allowDebt: request.allowDebt ?? child.allowDebt
        )
        children[index] = updated

        return UpdateChildSettingsResponse(
            childId: childId,
            firstName: updated.firstName,
            lastName: updated.lastName,
            weeklyAllowance: updated.weeklyAllowance,
            allowanceDay: updated.allowanceDay,
            savingsAccountEnabled: updated.savingsAccountEnabled,
            savingsTransferType: updated.savingsTransferType.rawValue,
            savingsTransferPercentage: Int(truncating: (updated.savingsTransferPercentage ?? 0) as NSDecimalNumber),
            savingsTransferAmount: updated.savingsTransferAmount ?? 0,
            allowDebt: updated.allowDebt,
            message: "Settings updated"
        )
    }

    // MARK: - Transactions

    func getTransactions(forChild childId: UUID, limit: Int) async throws -> [Transaction] {
        let childTransactions = transactions[childId] ?? []
        return Array(childTransactions.prefix(limit))
    }

    func createTransaction(_ request: CreateTransactionRequest) async throws -> Transaction {
        guard let childIndex = children.firstIndex(where: { $0.id == request.childId }) else {
            throw APIError.notFound
        }

        let child = children[childIndex]
        let signedAmount = request.type == .debit ? -abs(request.amount) : abs(request.amount)
        let newBalance = child.currentBalance + signedAmount

        let transaction = Transaction(
            id: UUID(),
            childId: request.childId,
            amount: signedAmount,
            type: request.type,
            category: request.category,
            description: request.description,
            notes: request.notes,
            balanceAfter: newBalance,
            createdAt: Date(),
            createdByName: currentUser?.firstName ?? "Test User"
        )

        // Update child balance
        let updatedChild = Child(
            id: child.id,
            firstName: child.firstName,
            lastName: child.lastName,
            weeklyAllowance: child.weeklyAllowance,
            currentBalance: newBalance,
            savingsBalance: child.savingsBalance,
            lastAllowanceDate: child.lastAllowanceDate,
            allowanceDay: child.allowanceDay,
            savingsAccountEnabled: child.savingsAccountEnabled,
            savingsTransferType: child.savingsTransferType,
            savingsTransferPercentage: child.savingsTransferPercentage,
            savingsTransferAmount: child.savingsTransferAmount,
            savingsBalanceVisibleToChild: child.savingsBalanceVisibleToChild,
            allowDebt: child.allowDebt
        )
        children[childIndex] = updatedChild

        // Add transaction to list
        if transactions[request.childId] == nil {
            transactions[request.childId] = []
        }
        transactions[request.childId]?.insert(transaction, at: 0)

        return transaction
    }

    func getBalance(forChild childId: UUID) async throws -> Decimal {
        guard let child = children.first(where: { $0.id == childId }) else {
            throw APIError.notFound
        }
        return child.currentBalance
    }

    // MARK: - Wish List

    func getWishList(forChild childId: UUID) async throws -> [WishListItem] {
        return wishListItems[childId] ?? []
    }

    func createWishListItem(_ request: CreateWishListItemRequest) async throws -> WishListItem {
        let childBalance = children.first(where: { $0.id == request.childId })?.currentBalance ?? 0
        let item = WishListItem(
            id: UUID(),
            childId: request.childId,
            name: request.name,
            price: request.price,
            url: request.url,
            notes: request.notes,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: childBalance >= request.price
        )

        if wishListItems[request.childId] == nil {
            wishListItems[request.childId] = []
        }
        wishListItems[request.childId]?.append(item)

        return item
    }

    func updateWishListItem(forChild childId: UUID, id: UUID, _ request: UpdateWishListItemRequest) async throws -> WishListItem {
        guard let items = wishListItems[childId],
              let index = items.firstIndex(where: { $0.id == id }) else {
            throw APIError.notFound
        }

        let existing = items[index]
        let childBalance = children.first(where: { $0.id == childId })?.currentBalance ?? 0
        let updated = WishListItem(
            id: existing.id,
            childId: existing.childId,
            name: request.name,
            price: request.price,
            url: request.url,
            notes: request.notes,
            isPurchased: existing.isPurchased,
            purchasedAt: existing.purchasedAt,
            createdAt: existing.createdAt,
            canAfford: childBalance >= request.price
        )

        wishListItems[childId]?[index] = updated
        return updated
    }

    func deleteWishListItem(forChild childId: UUID, id: UUID) async throws {
        wishListItems[childId]?.removeAll { $0.id == id }
    }

    func markWishListItemAsPurchased(forChild childId: UUID, id: UUID) async throws -> WishListItem {
        guard let items = wishListItems[childId],
              let index = items.firstIndex(where: { $0.id == id }) else {
            throw APIError.notFound
        }

        let existing = items[index]
        let updated = WishListItem(
            id: existing.id,
            childId: existing.childId,
            name: existing.name,
            price: existing.price,
            url: existing.url,
            notes: existing.notes,
            isPurchased: true,
            purchasedAt: Date(),
            createdAt: existing.createdAt,
            canAfford: existing.canAfford
        )

        wishListItems[childId]?[index] = updated
        return updated
    }

    // MARK: - Analytics

    func getBalanceHistory(forChild childId: UUID, days: Int) async throws -> [BalancePoint] {
        var points: [BalancePoint] = []
        let now = Date()
        var balance = children.first(where: { $0.id == childId })?.currentBalance ?? 100

        for day in 0..<days {
            let date = now.addingTimeInterval(Double(-day * 86400))
            points.append(BalancePoint(date: date, balance: balance, transactionDescription: nil))
            // Vary balance slightly for realistic chart
            let variation = Decimal(Int.random(in: -5...10))
            balance = balance + variation
            if balance < 0 { balance = 0 }
        }

        return points.reversed()
    }

    func getIncomeVsSpending(forChild childId: UUID) async throws -> IncomeSpendingSummary {
        let childTransactions = transactions[childId] ?? []
        let income = childTransactions.filter { $0.type == .credit }.reduce(Decimal(0)) { $0 + abs($1.amount) }
        let spending = childTransactions.filter { $0.type == .debit }.reduce(Decimal(0)) { $0 + abs($1.amount) }

        return IncomeSpendingSummary(
            totalIncome: income,
            totalSpending: spending,
            netSavings: income - spending,
            incomeTransactionCount: childTransactions.filter { $0.type == .credit }.count,
            spendingTransactionCount: childTransactions.filter { $0.type == .debit }.count,
            savingsRate: income > 0 ? ((income - spending) / income) * 100 : 0
        )
    }

    func getSpendingBreakdown(forChild childId: UUID) async throws -> [CategoryBreakdown] {
        let childTransactions = transactions[childId] ?? []
        let debits = childTransactions.filter { $0.type == .debit }

        var categoryTotals: [String: Decimal] = [:]
        for transaction in debits {
            let category = transaction.category
            categoryTotals[category, default: 0] += abs(transaction.amount)
        }

        let total = categoryTotals.values.reduce(Decimal(0), +)

        return categoryTotals.map { category, amount in
            CategoryBreakdown(
                category: category,
                amount: amount,
                percentage: total > 0 ? (amount / total) * 100 : 0,
                transactionCount: debits.filter { $0.category == category }.count
            )
        }.sorted { $0.amount > $1.amount }
    }

    func getMonthlyComparison(forChild childId: UUID, months: Int) async throws -> [MonthlyComparison] {
        var comparisons: [MonthlyComparison] = []
        let calendar = Calendar.current
        let now = Date()
        var runningBalance = children.first(where: { $0.id == childId })?.currentBalance ?? 100

        for month in 0..<months {
            guard let date = calendar.date(byAdding: .month, value: -month, to: now) else { continue }
            let monthFormatter = DateFormatter()
            monthFormatter.dateFormat = "MMMM"

            let income = Decimal(Int.random(in: 40...80))
            let spending = Decimal(Int.random(in: 20...60))
            let netSavings = income - spending

            comparisons.append(MonthlyComparison(
                year: calendar.component(.year, from: date),
                month: calendar.component(.month, from: date),
                monthName: monthFormatter.string(from: date),
                income: income,
                spending: spending,
                netSavings: netSavings,
                endingBalance: runningBalance
            ))

            runningBalance = runningBalance - netSavings
        }

        return comparisons.reversed()
    }

    // MARK: - Savings

    func getSavingsSummary(forChild childId: UUID) async throws -> SavingsAccountSummary {
        guard let child = children.first(where: { $0.id == childId }) else {
            throw APIError.notFound
        }

        let childSavings = savingsTransactions[childId] ?? []
        let deposits = childSavings.filter { $0.type == .deposit }.reduce(Decimal(0)) { $0 + $1.amount }
        let withdrawals = childSavings.filter { $0.type == .withdrawal }.reduce(Decimal(0)) { $0 + $1.amount }

        var configDescription = "Not configured"
        if child.savingsAccountEnabled {
            if child.savingsTransferType == .percentage, let pct = child.savingsTransferPercentage {
                configDescription = "\(pct)% of allowance"
            } else if let amount = child.savingsTransferAmount {
                configDescription = "$\(amount) per allowance"
            }
        }

        return SavingsAccountSummary(
            childId: childId,
            isEnabled: child.savingsAccountEnabled,
            currentBalance: child.savingsBalance,
            transferType: child.savingsTransferType.rawValue,
            transferAmount: child.savingsTransferAmount ?? 0,
            transferPercentage: child.savingsTransferPercentage ?? 0,
            totalTransactions: childSavings.count,
            totalDeposited: deposits,
            totalWithdrawn: withdrawals,
            lastTransactionDate: childSavings.first?.createdAt,
            configDescription: configDescription,
            balanceHidden: !child.savingsBalanceVisibleToChild
        )
    }

    func getSavingsHistory(forChild childId: UUID, limit: Int) async throws -> [SavingsTransaction] {
        let history = savingsTransactions[childId] ?? []
        return Array(history.prefix(limit))
    }

    func depositToSavings(_ request: DepositToSavingsRequest) async throws -> SavingsTransaction {
        guard let childIndex = children.firstIndex(where: { $0.id == request.childId }) else {
            throw APIError.notFound
        }

        let child = children[childIndex]
        let newSavingsBalance = child.savingsBalance + request.amount
        let newCurrentBalance = child.currentBalance - request.amount

        // Update child
        let updatedChild = Child(
            id: child.id,
            firstName: child.firstName,
            lastName: child.lastName,
            weeklyAllowance: child.weeklyAllowance,
            currentBalance: newCurrentBalance,
            savingsBalance: newSavingsBalance,
            lastAllowanceDate: child.lastAllowanceDate,
            allowanceDay: child.allowanceDay,
            savingsAccountEnabled: child.savingsAccountEnabled,
            savingsTransferType: child.savingsTransferType,
            savingsTransferPercentage: child.savingsTransferPercentage,
            savingsTransferAmount: child.savingsTransferAmount,
            savingsBalanceVisibleToChild: child.savingsBalanceVisibleToChild,
            allowDebt: child.allowDebt
        )
        children[childIndex] = updatedChild

        let transaction = SavingsTransaction(
            id: UUID(),
            childId: request.childId,
            type: .deposit,
            amount: request.amount,
            description: request.description,
            balanceAfter: newSavingsBalance,
            createdAt: Date(),
            createdById: currentUser?.userId ?? Self.testUserId,
            createdByName: currentUser?.firstName ?? "Test User"
        )

        if savingsTransactions[request.childId] == nil {
            savingsTransactions[request.childId] = []
        }
        savingsTransactions[request.childId]?.insert(transaction, at: 0)

        return transaction
    }

    func withdrawFromSavings(_ request: WithdrawFromSavingsRequest) async throws -> SavingsTransaction {
        guard let childIndex = children.firstIndex(where: { $0.id == request.childId }) else {
            throw APIError.notFound
        }

        let child = children[childIndex]
        guard child.savingsBalance >= request.amount else {
            throw APIError.validationError("Insufficient savings balance")
        }

        let newSavingsBalance = child.savingsBalance - request.amount
        let newCurrentBalance = child.currentBalance + request.amount

        // Update child
        let updatedChild = Child(
            id: child.id,
            firstName: child.firstName,
            lastName: child.lastName,
            weeklyAllowance: child.weeklyAllowance,
            currentBalance: newCurrentBalance,
            savingsBalance: newSavingsBalance,
            lastAllowanceDate: child.lastAllowanceDate,
            allowanceDay: child.allowanceDay,
            savingsAccountEnabled: child.savingsAccountEnabled,
            savingsTransferType: child.savingsTransferType,
            savingsTransferPercentage: child.savingsTransferPercentage,
            savingsTransferAmount: child.savingsTransferAmount,
            savingsBalanceVisibleToChild: child.savingsBalanceVisibleToChild,
            allowDebt: child.allowDebt
        )
        children[childIndex] = updatedChild

        let transaction = SavingsTransaction(
            id: UUID(),
            childId: request.childId,
            type: .withdrawal,
            amount: request.amount,
            description: request.description,
            balanceAfter: newSavingsBalance,
            createdAt: Date(),
            createdById: currentUser?.userId ?? Self.testUserId,
            createdByName: currentUser?.firstName ?? "Test User"
        )

        if savingsTransactions[request.childId] == nil {
            savingsTransactions[request.childId] = []
        }
        savingsTransactions[request.childId]?.insert(transaction, at: 0)

        return transaction
    }

    // MARK: - Parent Invites (Stubs)

    func sendParentInvite(_ request: SendParentInviteRequest) async throws -> ParentInviteResponse {
        return ParentInviteResponse(
            inviteId: UUID().uuidString,
            email: request.email,
            firstName: request.firstName,
            lastName: request.lastName,
            isExistingUser: false,
            expiresAt: Date().addingTimeInterval(604800),
            message: "Invite sent"
        )
    }

    func getPendingInvites() async throws -> [PendingInvite] { return [] }
    func cancelInvite(inviteId: String) async throws {}
    func resendInvite(inviteId: String) async throws -> ParentInviteResponse {
        return ParentInviteResponse(
            inviteId: inviteId,
            email: "test@example.com",
            firstName: "Test",
            lastName: "User",
            isExistingUser: false,
            expiresAt: Date().addingTimeInterval(604800),
            message: "Invite resent"
        )
    }

    // MARK: - Badges (Returns empty)

    func getAllBadges(category: BadgeCategory?, includeSecret: Bool) async throws -> [BadgeDto] { return [] }
    func getChildBadges(forChild childId: UUID, category: BadgeCategory?, newOnly: Bool) async throws -> [ChildBadgeDto] { return [] }
    func getBadgeProgress(forChild childId: UUID) async throws -> [BadgeProgressDto] { return [] }
    func getAchievementSummary(forChild childId: UUID) async throws -> AchievementSummaryDto {
        return AchievementSummaryDto(
            totalBadges: 10,
            earnedBadges: 3,
            totalPoints: 150,
            availablePoints: 100,
            recentBadges: [],
            inProgressBadges: [],
            badgesByCategory: [:]
        )
    }
    func toggleBadgeDisplay(forChild childId: UUID, badgeId: UUID, _ request: UpdateBadgeDisplayRequest) async throws -> ChildBadgeDto { throw APIError.notFound }
    func markBadgesSeen(forChild childId: UUID, _ request: MarkBadgesSeenRequest) async throws {}
    func getChildPoints(forChild childId: UUID) async throws -> ChildPointsDto {
        return ChildPointsDto(
            totalPoints: 150,
            availablePoints: 100,
            spentPoints: 50,
            badgesEarned: 3,
            rewardsUnlocked: 1
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
