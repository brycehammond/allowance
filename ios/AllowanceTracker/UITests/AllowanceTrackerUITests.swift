import XCTest

/// Base class for UI tests with common setup and helper methods
class AllowanceTrackerUITests: XCTestCase {

    // MARK: - Properties

    var app: XCUIApplication!

    /// Test credentials for parent user
    let testParentEmail = "testuser@example.com"
    let testParentPassword = "Password123@"

    /// Test credentials for child user (if needed)
    let testChildEmail = "testchild@example.com"
    let testChildPassword = "ChildPass123@"

    // MARK: - Setup & Teardown

    override func setUpWithError() throws {
        try super.setUpWithError()

        // Don't continue after failures
        continueAfterFailure = false

        // Initialize the app
        app = XCUIApplication()

        // Set launch arguments for testing
        app.launchArguments.append("--uitesting")
        app.launchEnvironment["UITEST_MODE"] = "1"

        // Launch the app
        app.launch()
    }

    override func tearDownWithError() throws {
        app = nil
        try super.tearDownWithError()
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

    /// Perform login flow
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
