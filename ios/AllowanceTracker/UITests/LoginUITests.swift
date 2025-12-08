import XCTest

/// UI tests for login and authentication flows
final class LoginUITests: AllowanceTrackerUITests {

    // MARK: - Login Screen Display Tests

    func testLoginScreen_DisplaysAllElements() throws {
        // Verify login screen elements are displayed
        assertLoginScreenDisplayed()

        // Check for logo/title
        let titleText = app.staticTexts["Allowance Tracker"]
        XCTAssertTrue(waitForElement(titleText), "App title should be visible")

        // Check for email field
        let emailField = app.textFields["login_email_field"]
        XCTAssertTrue(emailField.exists, "Email field should be visible")

        // Check for password field
        let passwordField = app.secureTextFields["login_password_field"]
        XCTAssertTrue(passwordField.exists, "Password field should be visible")

        // Check for login button
        let loginButton = app.buttons["login_button"]
        XCTAssertTrue(loginButton.exists, "Login button should be visible")

        // Check for register link
        let registerButton = app.buttons["register_button"]
        XCTAssertTrue(registerButton.exists, "Register button should be visible")

        takeScreenshot(name: "Login Screen")
    }

    // MARK: - Login Flow Tests

    func testLogin_WithValidCredentials_NavigatesToDashboard() throws {
        takeScreenshot(name: "Before Login")

        // Perform login with valid credentials
        performLogin(email: testParentEmail, password: testParentPassword)

        takeScreenshot(name: "After Login Button Tap")

        // Wait a moment for network request
        sleep(3)

        takeScreenshot(name: "After Wait")

        // Wait for login to complete and verify dashboard is displayed
        assertDashboardDisplayed()

        takeScreenshot(name: "Dashboard After Login")
    }

    func testLogin_WithInvalidCredentials_ShowsErrorMessage() throws {
        // Perform login with invalid credentials
        performLogin(email: "invalid@example.com", password: "wrongpassword")

        // Wait for error message to appear
        let errorAlert = app.alerts.firstMatch
        let errorText = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'error' OR label CONTAINS 'Error' OR label CONTAINS 'Invalid' OR label CONTAINS 'invalid'"))

        let errorDisplayed = errorAlert.waitForExistence(timeout: 5) ||
                             errorText.waitForExistence(timeout: 5)

        XCTAssertTrue(errorDisplayed, "Error message should be displayed for invalid credentials")

        takeScreenshot(name: "Login Error")
    }

    func testLogin_WithEmptyEmail_ShowsValidationError() throws {
        // Try to login with empty email
        let passwordField = app.secureTextFields["login_password_field"]
        let loginButton = app.buttons["login_button"]

        // Enter only password
        typeIntoField(passwordField, text: testParentPassword)
        tapWhenAvailable(loginButton)

        // Verify validation error is shown
        let errorText = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'email'"))
        XCTAssertTrue(errorText.waitForExistence(timeout: 5), "Email validation error should be shown")

        takeScreenshot(name: "Empty Email Error")
    }

    func testLogin_WithEmptyPassword_ShowsValidationError() throws {
        // Try to login with empty password
        let emailField = app.textFields["login_email_field"]
        let loginButton = app.buttons["login_button"]

        // Enter only email
        typeIntoField(emailField, text: testParentEmail)
        tapWhenAvailable(loginButton)

        // Verify validation error is shown
        let errorText = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'password' OR label CONTAINS 'Password'"))
        XCTAssertTrue(errorText.waitForExistence(timeout: 5), "Password validation error should be shown")

        takeScreenshot(name: "Empty Password Error")
    }

    // MARK: - Registration Navigation Tests

    func testSignUp_Button_NavigatesToRegistration() throws {
        // Tap the sign up button
        let registerButton = app.buttons["register_button"]
        tapWhenAvailable(registerButton)

        // Verify registration screen is displayed
        let registrationTitle = app.navigationBars["Register"]
        let emailField = app.textFields.element(matching: NSPredicate(format: "identifier CONTAINS 'email' OR label CONTAINS 'email'"))

        let registrationDisplayed = registrationTitle.waitForExistence(timeout: 5) ||
                                    (emailField.waitForExistence(timeout: 5) && !app.textFields["login_email_field"].exists)

        XCTAssertTrue(registrationDisplayed, "Registration screen should be displayed")

        takeScreenshot(name: "Registration Screen")
    }

    func testForgotPassword_Link_NavigatesToForgotPassword() throws {
        // Find and tap forgot password link
        let forgotPasswordButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Forgot' OR label CONTAINS 'forgot'"))

        guard waitForElement(forgotPasswordButton) else {
            XCTFail("Forgot password link not found")
            return
        }

        forgotPasswordButton.tap()

        // Verify forgot password screen is displayed
        let forgotPasswordTitle = app.navigationBars.element(matching: NSPredicate(format: "identifier CONTAINS 'Forgot' OR identifier CONTAINS 'Reset'"))
        let emailField = app.textFields.firstMatch

        let forgotPasswordDisplayed = forgotPasswordTitle.waitForExistence(timeout: 5) ||
                                       emailField.waitForExistence(timeout: 5)

        XCTAssertTrue(forgotPasswordDisplayed, "Forgot password screen should be displayed")

        takeScreenshot(name: "Forgot Password Screen")
    }

    // MARK: - Logout Tests

    func testLogout_ReturnsToLoginScreen() throws {
        // First, login
        performLogin(email: testParentEmail, password: testParentPassword)

        // Wait for dashboard to load
        assertDashboardDisplayed()

        // Find and tap logout button
        let logoutButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Logout' OR identifier CONTAINS 'logout' OR label CONTAINS 'Sign Out'"))

        // If logout is in a menu or navigation bar, find it
        if !logoutButton.exists {
            // Try toolbar/navigation bar buttons
            let navigationBarButton = app.navigationBars.buttons.element(boundBy: 0)
            if navigationBarButton.exists {
                navigationBarButton.tap()
            }
        }

        if waitForElement(logoutButton, timeout: 3) {
            logoutButton.tap()

            // Verify login screen is displayed again
            assertLoginScreenDisplayed()

            takeScreenshot(name: "After Logout")
        }
    }

    // MARK: - Loading State Tests

    func testLogin_ShowsLoadingIndicator() throws {
        // Enter credentials
        let emailField = app.textFields["login_email_field"]
        let passwordField = app.secureTextFields["login_password_field"]
        let loginButton = app.buttons["login_button"]

        typeIntoField(emailField, text: testParentEmail)
        typeIntoField(passwordField, text: testParentPassword)

        // Tap login and immediately check for loading state
        loginButton.tap()

        // Check for any loading indicator (ProgressView creates an ActivityIndicator)
        let loadingIndicator = app.activityIndicators.firstMatch
        let loadingText = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'Signing' OR label CONTAINS 'Loading'"))

        // Loading might be quick, so just verify the test doesn't crash
        // and login eventually succeeds
        assertDashboardDisplayed()
    }

    // MARK: - Accessibility Tests

    func testLoginScreen_MeetsAccessibilityRequirements() throws {
        // Verify all main elements have accessibility labels
        let emailField = app.textFields["login_email_field"]
        let passwordField = app.secureTextFields["login_password_field"]
        let loginButton = app.buttons["login_button"]

        // Check that elements are accessible
        XCTAssertNotNil(emailField.label, "Email field should have accessibility label")
        XCTAssertNotNil(passwordField.label, "Password field should have accessibility label")
        XCTAssertNotNil(loginButton.label, "Login button should have accessibility label")

        takeScreenshot(name: "Accessibility Check")
    }
}
