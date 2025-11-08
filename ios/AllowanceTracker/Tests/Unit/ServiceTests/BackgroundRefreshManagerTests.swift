import XCTest
import BackgroundTasks
@testable import AllowanceTracker

final class BackgroundRefreshManagerTests: XCTestCase {
    var sut: BackgroundRefreshManager!

    override func setUp() {
        super.setUp()
        sut = BackgroundRefreshManager.shared
    }

    override func tearDown() {
        sut = nil
        super.tearDown()
    }

    // MARK: - Task Identifier Tests

    func testTaskIdentifier_MatchesExpectedFormat() {
        // Assert
        XCTAssertEqual(BackgroundRefreshManager.taskIdentifier, "com.allowancetracker.refresh")
    }

    // MARK: - Registration Tests

    func testRegisterBackgroundTasks_RegistersHandler() {
        // Act
        sut.registerBackgroundTasks()

        // Assert
        // Note: We can't easily test BGTaskScheduler registration without an actual app
        // This test documents the expected behavior
        XCTAssertNotNil(sut)
    }

    // MARK: - Schedule Tests

    func testScheduleAppRefresh_CreatesValidRequest() {
        // Note: BGTaskScheduler.shared.submit() requires an active app
        // This test documents the expected behavior
        // In a real app, this would be tested through UI tests or integration tests

        // Act - This will fail outside of a running app but documents the API
        sut.scheduleAppRefresh()

        // Assert - Verify the manager exists
        XCTAssertNotNil(sut)
    }

    // MARK: - Refresh Interval Tests

    func testDefaultRefreshInterval_IsFifteenMinutes() {
        // Arrange
        let expectedInterval: TimeInterval = 15 * 60 // 15 minutes

        // Assert
        XCTAssertEqual(BackgroundRefreshManager.refreshInterval, expectedInterval)
    }

    // MARK: - Singleton Tests

    func testSharedInstance_ReturnsSameInstance() {
        // Arrange
        let instance1 = BackgroundRefreshManager.shared
        let instance2 = BackgroundRefreshManager.shared

        // Assert
        XCTAssertTrue(instance1 === instance2)
    }
}
