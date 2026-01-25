import XCTest
@testable import AllowanceTracker

@MainActor
final class CacheServiceTests: XCTestCase {
    var sut: CacheService!

    override func setUp() {
        super.setUp()
        sut = CacheService()
    }

    override func tearDown() {
        // No need to clear cache - each test creates a fresh CacheService instance
        // and CacheService is an actor with in-memory storage only
        sut = nil
        super.tearDown()
    }

    // MARK: - Children Caching Tests

    func testCacheChildren_StoresChildrenSuccessfully() async {
        // Arrange
        let children = [
            Child.makeForTest(firstName: "Alice", lastName: "Smith", weeklyAllowance: 10.00, currentBalance: 25.50),
            Child.makeForTest(firstName: "Bob", lastName: "Smith", weeklyAllowance: 15.00, currentBalance: 30.00)
        ]

        // Act
        await sut.cacheChildren(children)
        let cached = await sut.getCachedChildren()

        // Assert
        XCTAssertEqual(cached.count, 2)
        XCTAssertEqual(cached[0].firstName, "Alice")
        XCTAssertEqual(cached[1].firstName, "Bob")
    }

    func testGetCachedChildren_ReturnsEmptyArrayWhenNoCacheExists() async {
        // Act
        let cached = await sut.getCachedChildren()

        // Assert
        XCTAssertTrue(cached.isEmpty)
    }

    func testCacheChildren_OverwritesPreviousCache() async {
        // Arrange
        let firstBatch = [Child.makeForTest(firstName: "Alice", lastName: "Smith", weeklyAllowance: 10.00, currentBalance: 25.50)]
        let secondBatch = [Child.makeForTest(firstName: "Charlie", lastName: "Brown", weeklyAllowance: 20.00, currentBalance: 50.00)]

        // Act
        await sut.cacheChildren(firstBatch)
        await sut.cacheChildren(secondBatch)
        let cached = await sut.getCachedChildren()

        // Assert
        XCTAssertEqual(cached.count, 1)
        XCTAssertEqual(cached[0].firstName, "Charlie")
    }

    // MARK: - Transaction Caching Tests

    func testCacheTransactions_StoresTransactionsForChildId() async {
        // Arrange
        let childId = UUID()
        let transactions = [
            Transaction(
                id: UUID(),
                childId: childId,
                amount: 10.00,
                type: .credit,
                description: "Allowance",
                balanceAfter: 35.50,
                createdAt: Date(),
                createdByName: "Parent"
            ),
            Transaction(
                id: UUID(),
                childId: childId,
                amount: 5.00,
                type: .debit,
                description: "Candy",
                balanceAfter: 30.50,
                createdAt: Date(),
                createdByName: "Child"
            )
        ]

        // Act
        await sut.cacheTransactions(transactions, for: childId)
        let cached = await sut.getCachedTransactions(for: childId)

        // Assert
        XCTAssertEqual(cached.count, 2)
        XCTAssertEqual(cached[0].description, "Allowance")
        XCTAssertEqual(cached[1].description, "Candy")
    }

    func testGetCachedTransactions_ReturnsEmptyArrayWhenNoCacheExists() async {
        // Arrange
        let childId = UUID()

        // Act
        let cached = await sut.getCachedTransactions(for: childId)

        // Assert
        XCTAssertTrue(cached.isEmpty)
    }

    func testCacheTransactions_HandlesMultipleChildren() async {
        // Arrange
        let child1Id = UUID()
        let child2Id = UUID()
        let child1Transactions = [
            Transaction(id: UUID(), childId: child1Id, amount: 10.00, type: .credit, description: "Child 1", balanceAfter: 10.00, createdAt: Date(), createdByName: "Parent")
        ]
        let child2Transactions = [
            Transaction(id: UUID(), childId: child2Id, amount: 15.00, type: .credit, description: "Child 2", balanceAfter: 15.00, createdAt: Date(), createdByName: "Parent")
        ]

        // Act
        await sut.cacheTransactions(child1Transactions, for: child1Id)
        await sut.cacheTransactions(child2Transactions, for: child2Id)

        let cached1 = await sut.getCachedTransactions(for: child1Id)
        let cached2 = await sut.getCachedTransactions(for: child2Id)

        // Assert
        XCTAssertEqual(cached1.count, 1)
        XCTAssertEqual(cached1[0].description, "Child 1")
        XCTAssertEqual(cached2.count, 1)
        XCTAssertEqual(cached2[0].description, "Child 2")
    }

    // MARK: - Cache Freshness Tests

    func testNeedsRefresh_ReturnsTrueWhenNoLastSyncDate() async {
        // Act
        let needsRefresh = await sut.needsRefresh()

        // Assert
        XCTAssertTrue(needsRefresh)
    }

    func testNeedsRefresh_ReturnsFalseWhenCacheIsFresh() async {
        // Arrange
        let children = [Child.makeForTest(firstName: "Alice", lastName: "Smith", weeklyAllowance: 10.00, currentBalance: 25.50)]
        await sut.cacheChildren(children)

        // Act
        let needsRefresh = await sut.needsRefresh(maxAge: 300) // 5 minutes

        // Assert
        XCTAssertFalse(needsRefresh)
    }

    func testNeedsRefresh_ReturnsTrueWhenCacheIsStale() async {
        // Arrange
        let children = [Child.makeForTest(firstName: "Alice", lastName: "Smith", weeklyAllowance: 10.00, currentBalance: 25.50)]
        await sut.cacheChildren(children)

        // Act - Use very short maxAge to simulate stale cache
        let needsRefresh = await sut.needsRefresh(maxAge: 0.001) // 1 millisecond

        // Wait a tiny bit to ensure cache is stale
        try? await Task.sleep(nanoseconds: 2_000_000) // 2 milliseconds

        let needsRefreshAfterWait = await sut.needsRefresh(maxAge: 0.001)

        // Assert
        XCTAssertTrue(needsRefreshAfterWait)
    }

    // MARK: - Clear Cache Tests

    func testClearCache_RemovesAllCachedData() async {
        // Arrange
        let children = [Child.makeForTest(firstName: "Alice", lastName: "Smith", weeklyAllowance: 10.00, currentBalance: 25.50)]
        let childId = UUID()
        let transactions = [Transaction(id: UUID(), childId: childId, amount: 10.00, type: .credit, description: "Test", balanceAfter: 10.00, createdAt: Date(), createdByName: "Parent")]

        await sut.cacheChildren(children)
        await sut.cacheTransactions(transactions, for: childId)

        // Act
        await sut.clearCache()

        let cachedChildren = await sut.getCachedChildren()
        let cachedTransactions = await sut.getCachedTransactions(for: childId)
        let needsRefresh = await sut.needsRefresh()

        // Assert
        XCTAssertTrue(cachedChildren.isEmpty)
        XCTAssertTrue(cachedTransactions.isEmpty)
        XCTAssertTrue(needsRefresh)
    }

}
