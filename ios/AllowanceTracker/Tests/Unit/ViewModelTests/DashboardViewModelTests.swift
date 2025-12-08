import XCTest
@testable import AllowanceTracker

@MainActor
final class DashboardViewModelTests: XCTestCase {
    var sut: DashboardViewModel!
    var mockAPIService: MockAPIService!

    override func setUp() {
        super.setUp()
        mockAPIService = MockAPIService()
        sut = DashboardViewModel(apiService: mockAPIService)
    }

    override func tearDown() {
        sut = nil
        mockAPIService = nil
        super.tearDown()
    }

    // MARK: - Load Children Tests

    func testLoadChildren_Success_PopulatesChildrenArray() async {
        // Arrange
        let expectedChildren = [
            Child(id: UUID(), firstName: "Alice", lastName: "Smith", weeklyAllowance: 10.00, currentBalance: 25.50, lastAllowanceDate: nil),
            Child(id: UUID(), firstName: "Bob", lastName: "Smith", weeklyAllowance: 15.00, currentBalance: 30.00, lastAllowanceDate: nil)
        ]
        mockAPIService.childrenResult = .success(expectedChildren)

        // Act
        await sut.loadChildren()

        // Assert
        XCTAssertEqual(sut.children.count, 2)
        XCTAssertEqual(sut.children[0].firstName, "Alice")
        XCTAssertEqual(sut.children[1].firstName, "Bob")
        XCTAssertFalse(sut.isLoading)
        XCTAssertNil(sut.errorMessage)
    }

    func testLoadChildren_Failure_SetsErrorMessage() async {
        // Arrange
        mockAPIService.childrenResult = .failure(APIError.unauthorized)

        // Act
        await sut.loadChildren()

        // Assert
        XCTAssertTrue(sut.children.isEmpty)
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertFalse(sut.isLoading)
    }

    func testLoadChildren_SetsLoadingStateDuringExecution() async {
        // Arrange
        mockAPIService.childrenResult = .success([])
        mockAPIService.shouldDelay = true

        // Act
        let loadTask = Task {
            await sut.loadChildren()
        }

        // Assert loading state
        try? await Task.sleep(nanoseconds: 10_000_000) // 10ms
        XCTAssertTrue(sut.isLoading)

        // Wait for completion
        await loadTask.value
        XCTAssertFalse(sut.isLoading)
    }

    func testLoadChildren_EmptyResponse_HandlesGracefully() async {
        // Arrange
        mockAPIService.childrenResult = .success([])

        // Act
        await sut.loadChildren()

        // Assert
        XCTAssertTrue(sut.children.isEmpty)
        XCTAssertNil(sut.errorMessage)
        XCTAssertFalse(sut.isLoading)
    }

    // MARK: - Refresh Tests

    func testRefresh_Success_UpdatesChildren() async {
        // Arrange
        let initialChildren = [
            Child(id: UUID(), firstName: "Alice", lastName: "Smith", weeklyAllowance: 10.00, currentBalance: 25.50, lastAllowanceDate: nil)
        ]
        let updatedChildren = [
            Child(id: UUID(), firstName: "Alice", lastName: "Smith", weeklyAllowance: 10.00, currentBalance: 35.50, lastAllowanceDate: Date()),
            Child(id: UUID(), firstName: "Charlie", lastName: "Brown", weeklyAllowance: 20.00, currentBalance: 50.00, lastAllowanceDate: nil)
        ]

        mockAPIService.childrenResult = .success(initialChildren)
        await sut.loadChildren()

        mockAPIService.childrenResult = .success(updatedChildren)

        // Act
        await sut.refresh()

        // Assert
        XCTAssertEqual(sut.children.count, 2)
        XCTAssertEqual(sut.children[0].currentBalance, 35.50)
        XCTAssertEqual(sut.children[1].firstName, "Charlie")
    }

    func testRefresh_ClearsErrorMessage() async {
        // Arrange
        mockAPIService.childrenResult = .failure(APIError.networkError)
        await sut.loadChildren()
        XCTAssertNotNil(sut.errorMessage)

        mockAPIService.childrenResult = .success([])

        // Act
        await sut.refresh()

        // Assert
        XCTAssertNil(sut.errorMessage)
    }

    // MARK: - Add Child Tests

    func testShowAddChild_TogglesState() {
        // Arrange
        XCTAssertFalse(sut.showAddChild)

        // Act
        sut.showAddChild = true

        // Assert
        XCTAssertTrue(sut.showAddChild)
    }

    // MARK: - Error Handling Tests

    func testLoadChildren_NetworkError_SetsAppropriateMessage() async {
        // Arrange
        mockAPIService.childrenResult = .failure(APIError.networkError)

        // Act
        await sut.loadChildren()

        // Assert
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertTrue(sut.errorMessage?.contains("network") ?? false || sut.errorMessage?.contains("Network") ?? false)
    }

    func testLoadChildren_ServerError_SetsAppropriateMessage() async {
        // Arrange
        mockAPIService.childrenResult = .failure(APIError.httpError(500))

        // Act
        await sut.loadChildren()

        // Assert
        XCTAssertNotNil(sut.errorMessage)
    }

    // MARK: - State Management Tests

    func testInitialState_IsCorrect() {
        // Assert
        XCTAssertTrue(sut.children.isEmpty)
        XCTAssertFalse(sut.isLoading)
        XCTAssertNil(sut.errorMessage)
        XCTAssertFalse(sut.showAddChild)
    }

    func testMultipleLoadCalls_HandlesGracefully() async {
        // Arrange
        let children = [
            Child(id: UUID(), firstName: "Alice", lastName: "Smith", weeklyAllowance: 10.00, currentBalance: 25.50, lastAllowanceDate: nil)
        ]
        mockAPIService.childrenResult = .success(children)

        // Act - Call loadChildren multiple times
        await sut.loadChildren()
        await sut.loadChildren()
        await sut.loadChildren()

        // Assert - Should handle multiple calls without issues
        XCTAssertEqual(sut.children.count, 1)
        XCTAssertFalse(sut.isLoading)
    }
}

// MARK: - Mock API Service Extension

extension MockAPIService {
    var childrenResult: Result<[Child], Error> {
        get { childrenResponse }
        set { childrenResponse = newValue }
    }
}
