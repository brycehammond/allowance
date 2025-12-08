import XCTest
@testable import AllowanceTracker

@MainActor
final class TransactionViewModelTests: XCTestCase {
    var sut: TransactionViewModel!
    var mockAPIService: MockAPIService!
    let testChildId = UUID()

    override func setUp() {
        super.setUp()
        mockAPIService = MockAPIService()
        sut = TransactionViewModel(childId: testChildId, apiService: mockAPIService)
    }

    override func tearDown() {
        sut = nil
        mockAPIService = nil
        super.tearDown()
    }

    // MARK: - Load Transactions Tests

    func testLoadTransactions_Success_PopulatesTransactionsArray() async {
        // Arrange
        let expectedTransactions = [
            Transaction(
                id: UUID(),
                childId: testChildId,
                amount: 10.00,
                type: .credit,
                description: "Allowance",
                balanceAfter: 35.50,
                createdAt: Date(),
                createdByName: "Parent"
            ),
            Transaction(
                id: UUID(),
                childId: testChildId,
                amount: 5.00,
                type: .debit,
                description: "Candy",
                balanceAfter: 30.50,
                createdAt: Date(),
                createdByName: "Child"
            )
        ]
        mockAPIService.transactionsResult = .success(expectedTransactions)

        // Act
        await sut.loadTransactions()

        // Assert
        XCTAssertEqual(sut.transactions.count, 2)
        XCTAssertEqual(sut.transactions[0].description, "Allowance")
        XCTAssertEqual(sut.transactions[1].description, "Candy")
        XCTAssertFalse(sut.isLoading)
        XCTAssertNil(sut.errorMessage)
    }

    func testLoadTransactions_Failure_SetsErrorMessage() async {
        // Arrange
        mockAPIService.transactionsResult = .failure(APIError.unauthorized)

        // Act
        await sut.loadTransactions()

        // Assert
        XCTAssertTrue(sut.transactions.isEmpty)
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertFalse(sut.isLoading)
    }

    func testLoadTransactions_EmptyResponse_HandlesGracefully() async {
        // Arrange
        mockAPIService.transactionsResult = .success([])

        // Act
        await sut.loadTransactions()

        // Assert
        XCTAssertTrue(sut.transactions.isEmpty)
        XCTAssertNil(sut.errorMessage)
        XCTAssertFalse(sut.isLoading)
    }

    // MARK: - Create Transaction Tests

    func testCreateTransaction_Success_RefreshesTransactionList() async {
        // Arrange
        let initialTransactions = [
            Transaction(id: UUID(), childId: testChildId, amount: 10.00, type: .credit, description: "Initial", balanceAfter: 10.00, createdAt: Date(), createdByName: "Parent")
        ]
        let newTransaction = Transaction(
            id: UUID(),
            childId: testChildId,
            amount: 5.00,
            type: .debit,
            description: "New Transaction",
            balanceAfter: 5.00,
            createdAt: Date(),
            createdByName: "Parent"
        )

        mockAPIService.transactionsResult = .success(initialTransactions)
        await sut.loadTransactions()

        mockAPIService.createTransactionResult = .success(newTransaction)
        mockAPIService.transactionsResult = .success(initialTransactions + [newTransaction])

        // Act
        _ = await sut.createTransaction(amount: 5.00, type: .debit, category: "Shopping", description: "New Transaction")

        // Assert
        XCTAssertEqual(sut.transactions.count, 2)
        XCTAssertNil(sut.errorMessage)
    }

    func testCreateTransaction_Failure_SetsErrorMessage() async {
        // Arrange
        mockAPIService.createTransactionResult = .failure(APIError.validationError("Insufficient funds"))

        // Act
        _ = await sut.createTransaction(amount: 100.00, type: .debit, category: "Shopping", description: "Too much")

        // Assert
        XCTAssertNotNil(sut.errorMessage)
    }

    // MARK: - Refresh Tests

    func testRefresh_Success_UpdatesTransactions() async {
        // Arrange
        let initialTransactions = [
            Transaction(id: UUID(), childId: testChildId, amount: 10.00, type: .credit, description: "First", balanceAfter: 10.00, createdAt: Date(), createdByName: "Parent")
        ]
        let updatedTransactions = [
            Transaction(id: UUID(), childId: testChildId, amount: 10.00, type: .credit, description: "First", balanceAfter: 10.00, createdAt: Date(), createdByName: "Parent"),
            Transaction(id: UUID(), childId: testChildId, amount: 5.00, type: .credit, description: "Second", balanceAfter: 15.00, createdAt: Date(), createdByName: "Parent")
        ]

        mockAPIService.transactionsResult = .success(initialTransactions)
        await sut.loadTransactions()

        mockAPIService.transactionsResult = .success(updatedTransactions)

        // Act
        await sut.refresh()

        // Assert
        XCTAssertEqual(sut.transactions.count, 2)
    }

    // MARK: - Transaction Type Tests

    func testLoadTransactions_HandlesMultipleTransactionTypes() async {
        // Arrange
        let mixedTransactions = [
            Transaction(id: UUID(), childId: testChildId, amount: 10.00, type: .credit, description: "Credit", balanceAfter: 10.00, createdAt: Date(), createdByName: "Parent"),
            Transaction(id: UUID(), childId: testChildId, amount: 5.00, type: .debit, description: "Debit", balanceAfter: 5.00, createdAt: Date(), createdByName: "Child")
        ]
        mockAPIService.transactionsResult = .success(mixedTransactions)

        // Act
        await sut.loadTransactions()

        // Assert
        XCTAssertEqual(sut.transactions.count, 2)
        XCTAssertEqual(sut.transactions[0].type, .credit)
        XCTAssertEqual(sut.transactions[1].type, .debit)
    }

    // MARK: - State Management Tests

    func testInitialState_IsCorrect() {
        // Assert
        XCTAssertTrue(sut.transactions.isEmpty)
        XCTAssertFalse(sut.isLoading)
        XCTAssertNil(sut.errorMessage)
    }

    func testChildId_IsSetCorrectly() {
        // Assert
        XCTAssertEqual(sut.childId, testChildId)
    }
}

// MARK: - Mock API Service Extension for Transactions

extension MockAPIService {
    var transactionsResult: Result<[Transaction], Error>? {
        get { nil }
        set {
            if let result = newValue {
                transactionsResponse = result
            }
        }
    }

    var createTransactionResult: Result<Transaction, Error>? {
        get { nil }
        set {
            if let result = newValue {
                createTransactionResponse = result
            }
        }
    }
}
