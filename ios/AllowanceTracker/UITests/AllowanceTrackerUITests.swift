import XCTest

/// Base class for UI tests with common setup and helper methods.
/// Supports both mock API mode (fast, deterministic) and real API mode (integration testing).
///
/// ## Real API Mode
/// To run UI tests against a real API server, set these environment variables:
/// - `TEST_API_BASE_URL`: The API server URL (e.g., "http://localhost:5000")
/// - `TEST_API_KEY`: The API key for test account cleanup
/// - `UITEST_REAL_API`: Set to "1" to enable real API mode
///
/// In real API mode, a fresh test account is created before each test and deleted after.
class AllowanceTrackerUITests: XCTestCase {

    // MARK: - Properties

    var app: XCUIApplication!

    /// The current test account (only populated in real API mode)
    var testAccount: UITestAPIClient.TestAccount?

    /// Whether we're running in real API mode
    var isRealAPIMode: Bool {
        ProcessInfo.processInfo.environment["UITEST_REAL_API"] == "1"
    }

    /// Test API client for account management
    let apiClient = UITestAPIClient.shared

    // MARK: - Legacy Test Credentials (for mock mode)

    /// Test credentials for parent user (mock mode only)
    var testParentEmail: String {
        testAccount?.email ?? "testuser@example.com"
    }

    var testParentPassword: String {
        testAccount?.password ?? "Password123@"
    }

    /// Test credentials for child user (mock mode only)
    let testChildEmail = "testchild@example.com"
    let testChildPassword = "ChildPass123@"

    // MARK: - Setup & Teardown

    override func setUpWithError() throws {
        try super.setUpWithError()

        // Don't continue after failures
        continueAfterFailure = false

        // Initialize the app
        app = XCUIApplication()

        if isRealAPIMode {
            // Real API mode: create a fresh test account
            try setUpRealAPIMode()
        } else {
            // Mock mode: use mock services
            setUpMockMode()
        }

        // Launch the app
        app.launch()
    }

    override func tearDownWithError() throws {
        if isRealAPIMode {
            // Clean up the test account
            tearDownRealAPIMode()
        }

        app = nil
        testAccount = nil
        try super.tearDownWithError()
    }

    // MARK: - Setup Helpers

    /// Set up for real API integration testing
    private func setUpRealAPIMode() throws {
        // Generate a unique email for this test
        let testEmail = UITestAPIClient.generateTestEmail(prefix: "uitest_\(name)")

        // Create the test account synchronously
        let expectation = XCTestExpectation(description: "Create test account")
        var creationError: Error?

        Task {
            do {
                // First check if API is reachable
                guard await apiClient.isAPIReachable() else {
                    creationError = UITestAPIClient.APIError.serverError("API not reachable at \(UITestAPIClient.baseURL)")
                    expectation.fulfill()
                    return
                }

                // Create the test account
                let account = try await apiClient.createTestParentAccount(
                    email: testEmail,
                    password: "TestPass123!",
                    firstName: "Test",
                    lastName: "User",
                    familyName: "Test Family \(Int.random(in: 1000...9999))"
                )
                self.testAccount = account
                print("Created test account: \(testEmail)")
            } catch {
                creationError = error
                print("Failed to create test account: \(error)")
            }
            expectation.fulfill()
        }

        wait(for: [expectation], timeout: 30.0)

        if let error = creationError {
            throw error
        }

        // Configure app for real API mode
        app.launchArguments.append("--uitesting")
        app.launchEnvironment["UITEST_MODE"] = "1"
        app.launchEnvironment["UITEST_REAL_API"] = "1"

        // Pass the API URL to the app
        if let apiURL = ProcessInfo.processInfo.environment["TEST_API_BASE_URL"] {
            app.launchEnvironment["TEST_API_BASE_URL"] = apiURL
        }
    }

    /// Set up for mock mode testing
    private func setUpMockMode() {
        app.launchArguments.append("--uitesting")
        app.launchEnvironment["UITEST_MODE"] = "1"
        // Don't set UITEST_REAL_API - this will use mock services
    }

    /// Clean up test account after test
    private func tearDownRealAPIMode() {
        guard let account = testAccount else { return }

        let expectation = XCTestExpectation(description: "Delete test account")

        Task {
            do {
                try await apiClient.deleteTestAccount(email: account.email)
                print("Deleted test account: \(account.email)")
            } catch {
                // Log but don't fail - cleanup errors shouldn't fail tests
                print("Warning: Failed to delete test account \(account.email): \(error)")
            }
            expectation.fulfill()
        }

        wait(for: [expectation], timeout: 10.0)
    }

    // MARK: - Helper Methods

    /// Wait for element to exist with timeout
    func waitForElement(_ element: XCUIElement, timeout: TimeInterval = 5) -> Bool {
        return element.waitForExistence(timeout: timeout)
    }

    /// Wait for element to disappear
    func waitForElementToDisappear(_ element: XCUIElement, timeout: TimeInterval = 5) -> Bool {
        let predicate = NSPredicate(format: "exists == false")
        let expectation = XCTNSPredicateExpectation(predicate: predicate, object: element)
        let result = XCTWaiter().wait(for: [expectation], timeout: timeout)
        return result == .completed
    }

    /// Tap element when it becomes available
    func tapWhenAvailable(_ element: XCUIElement, timeout: TimeInterval = 5) {
        guard waitForElement(element, timeout: timeout) else {
            XCTFail("Element not available to tap: \(element)")
            return
        }
        element.tap()
    }

    /// Type text into a text field
    func typeIntoField(_ element: XCUIElement, text: String) {
        guard waitForElement(element) else {
            XCTFail("Text field not available: \(element)")
            return
        }
        element.tap()
        element.typeText(text)
    }

    /// Clear and type text into a text field
    func clearAndType(_ element: XCUIElement, text: String) {
        guard waitForElement(element) else {
            XCTFail("Text field not available: \(element)")
            return
        }
        element.tap()
        // Clear existing text
        if let existingText = element.value as? String, !existingText.isEmpty {
            let deleteString = String(repeating: XCUIKeyboardKey.delete.rawValue, count: existingText.count)
            element.typeText(deleteString)
        }
        element.typeText(text)
    }

    /// Perform login flow with the current test account
    func performLogin() {
        performLogin(email: testParentEmail, password: testParentPassword)
    }

    /// Perform login flow with specific credentials
    func performLogin(email: String, password: String) {
        let emailField = app.textFields["login_email_field"]
        let passwordField = app.secureTextFields["login_password_field"]
        let loginButton = app.buttons["login_button"]

        // Enter email
        typeIntoField(emailField, text: email)

        // Enter password
        typeIntoField(passwordField, text: password)

        // Tap login button
        tapWhenAvailable(loginButton)
    }

    /// Log out if logged in
    func performLogout() {
        let logoutButton = app.buttons["logout_button"]
        if logoutButton.exists {
            logoutButton.tap()
        }
    }

    /// Assert that login screen is displayed
    func assertLoginScreenDisplayed() {
        let emailField = app.textFields["login_email_field"]
        XCTAssertTrue(waitForElement(emailField), "Login email field should be visible")
    }

    /// Assert that dashboard is displayed
    func assertDashboardDisplayed() {
        // Wait for dashboard to load - check multiple indicators
        let dashboardTitle = app.navigationBars["Dashboard"]
        let welcomeText = app.staticTexts.element(matching: NSPredicate(format: "label BEGINSWITH 'Welcome'"))
        let tabBar = app.tabBars.firstMatch
        let profileTab = app.tabBars.buttons["Profile"]

        // Check for any indicator that we're past login
        let dashboardLoaded = dashboardTitle.waitForExistence(timeout: 15) ||
                             welcomeText.waitForExistence(timeout: 5) ||
                             (tabBar.waitForExistence(timeout: 5) && profileTab.exists)

        // Take screenshot for debugging
        takeScreenshot(name: "Dashboard Check")

        // If dashboard not loaded, check for error messages
        if !dashboardLoaded {
            let errorText = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS[c] 'error' OR label CONTAINS[c] 'failed' OR label CONTAINS[c] 'network'"))
            if errorText.exists {
                XCTFail("Login failed with error: \(errorText.label)")
                return
            }
        }

        XCTAssertTrue(dashboardLoaded, "Dashboard should be visible after login")
    }

    /// Take a screenshot and attach to test results
    func takeScreenshot(name: String) {
        let screenshot = XCUIScreen.main.screenshot()
        let attachment = XCTAttachment(screenshot: screenshot)
        attachment.name = name
        attachment.lifetime = .keepAlways
        add(attachment)
    }
}

// MARK: - Test Mode Helpers

extension AllowanceTrackerUITests {

    /// Skip test if not in real API mode
    func skipIfNotRealAPIMode() throws {
        try XCTSkipUnless(isRealAPIMode, "This test requires real API mode")
    }

    /// Skip test if in real API mode
    func skipIfRealAPIMode() throws {
        try XCTSkipIf(isRealAPIMode, "This test only runs in mock mode")
    }
}
