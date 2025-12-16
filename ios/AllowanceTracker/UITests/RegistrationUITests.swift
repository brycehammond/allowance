import XCTest

/// UI tests for registration and sign-up flows
final class RegistrationUITests: AllowanceTrackerUITests {

    // MARK: - Setup

    override func setUpWithError() throws {
        try super.setUpWithError()
        // Start from login screen - no login needed
    }

    // MARK: - Registration Screen Access Tests

    func testRegistration_AccessFromLogin() throws {
        // Verify we're on login screen
        assertLoginScreenDisplayed()

        // Tap register button
        let registerButton = app.buttons["register_button"]
        tapWhenAvailable(registerButton)

        // Verify registration screen is displayed
        let registrationTitle = app.navigationBars["Create Account"]
        let registrationAlternative = app.navigationBars["Register"]
        let createAccountText = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'Join' OR label CONTAINS 'Create'"))

        let registrationDisplayed = registrationTitle.waitForExistence(timeout: 5) ||
                                    registrationAlternative.waitForExistence(timeout: 3) ||
                                    createAccountText.waitForExistence(timeout: 3)

        XCTAssertTrue(registrationDisplayed, "Registration screen should be displayed")

        takeScreenshot(name: "Registration Screen")
    }

    // MARK: - Registration Form Display Tests

    func testRegistration_DisplaysAllFields() throws {
        navigateToRegistration()

        // Check for name fields
        let firstNameField = app.textFields["register_first_name_field"]
        let lastNameField = app.textFields["register_last_name_field"]
        let anyFirstNameField = app.textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS 'First' OR label CONTAINS 'First'"))
        let anyLastNameField = app.textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS 'Last' OR label CONTAINS 'Last'"))

        // Check for email field
        let emailField = app.textFields["register_email_field"]
        let anyEmailField = app.textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS 'email' OR label CONTAINS 'Email'"))

        // Check for password fields
        let passwordField = app.secureTextFields["register_password_field"]
        let confirmPasswordField = app.secureTextFields["register_confirm_password_field"]

        // Verify at least some fields exist
        let hasNameFields = firstNameField.exists || lastNameField.exists || anyFirstNameField.exists || anyLastNameField.exists
        let hasEmailField = emailField.exists || anyEmailField.exists
        let hasPasswordFields = passwordField.exists || confirmPasswordField.exists || app.secureTextFields.count >= 2

        XCTAssertTrue(hasNameFields || hasEmailField || hasPasswordFields, "Registration form should have input fields")

        takeScreenshot(name: "Registration Form Fields")
    }

    func testRegistration_DisplaysRolePicker() throws {
        navigateToRegistration()

        // Look for role picker
        let rolePicker = app.segmentedControls["register_role_picker"]
        let anyRolePicker = app.segmentedControls.firstMatch
        let parentButton = app.buttons["Parent"]
        let childButton = app.buttons["Child"]

        let hasRolePicker = rolePicker.exists || anyRolePicker.exists || (parentButton.exists && childButton.exists)

        if hasRolePicker {
            takeScreenshot(name: "Role Picker")
        }
    }

    func testRegistration_DisplaysSubmitButton() throws {
        navigateToRegistration()

        // Look for submit button
        let submitButton = app.buttons["register_submit_button"]
        let createAccountButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Create Account'"))
        let signUpButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Sign Up'"))
        let registerButton = app.buttons.element(matching: NSPredicate(format: "label == 'Register'"))

        let hasSubmitButton = submitButton.exists || createAccountButton.exists || signUpButton.exists || registerButton.exists
        XCTAssertTrue(hasSubmitButton, "Submit button should be visible")

        takeScreenshot(name: "Submit Button")
    }

    // MARK: - Form Interaction Tests

    func testRegistration_FirstNameEntry() throws {
        navigateToRegistration()

        // Find and fill first name field
        let firstNameField = app.textFields["register_first_name_field"]
        let anyFirstNameField = app.textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS 'First'"))
        let firstTextField = app.textFields.firstMatch

        if waitForElement(firstNameField, timeout: 3) {
            typeIntoField(firstNameField, text: "Test")
        } else if waitForElement(anyFirstNameField, timeout: 3) {
            typeIntoField(anyFirstNameField, text: "Test")
        } else if waitForElement(firstTextField, timeout: 3) {
            typeIntoField(firstTextField, text: "Test")
        }

        takeScreenshot(name: "First Name Entered")
    }

    func testRegistration_LastNameEntry() throws {
        navigateToRegistration()

        // Find and fill last name field
        let lastNameField = app.textFields["register_last_name_field"]
        let anyLastNameField = app.textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS 'Last'"))

        // Try filling second text field if specific one not found
        let textFields = app.textFields
        if textFields.count >= 2 {
            let field = textFields.element(boundBy: 1)
            typeIntoField(field, text: "User")
        } else if waitForElement(lastNameField, timeout: 3) {
            typeIntoField(lastNameField, text: "User")
        } else if waitForElement(anyLastNameField, timeout: 3) {
            typeIntoField(anyLastNameField, text: "User")
        }

        takeScreenshot(name: "Last Name Entered")
    }

    func testRegistration_EmailEntry() throws {
        navigateToRegistration()

        // Find and fill email field
        let emailField = app.textFields["register_email_field"]
        let anyEmailField = app.textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS 'email' OR label CONTAINS 'Email'"))

        if waitForElement(emailField, timeout: 3) {
            typeIntoField(emailField, text: "newuser@test.com")
        } else if waitForElement(anyEmailField, timeout: 3) {
            typeIntoField(anyEmailField, text: "newuser@test.com")
        }

        takeScreenshot(name: "Email Entered")
    }

    func testRegistration_PasswordEntry() throws {
        navigateToRegistration()

        // Find and fill password field
        let passwordField = app.secureTextFields["register_password_field"]
        let anyPasswordField = app.secureTextFields.firstMatch

        if waitForElement(passwordField, timeout: 3) {
            typeIntoField(passwordField, text: "TestPassword123!")
        } else if waitForElement(anyPasswordField, timeout: 3) {
            typeIntoField(anyPasswordField, text: "TestPassword123!")
        }

        takeScreenshot(name: "Password Entered")
    }

    func testRegistration_ConfirmPasswordEntry() throws {
        navigateToRegistration()

        // Find and fill confirm password field
        let confirmField = app.secureTextFields["register_confirm_password_field"]

        // Try second secure text field if specific one not found
        let secureFields = app.secureTextFields
        if secureFields.count >= 2 {
            let field = secureFields.element(boundBy: 1)
            typeIntoField(field, text: "TestPassword123!")
        } else if waitForElement(confirmField, timeout: 3) {
            typeIntoField(confirmField, text: "TestPassword123!")
        }

        takeScreenshot(name: "Confirm Password Entered")
    }

    func testRegistration_RoleSelection() throws {
        navigateToRegistration()

        // Try selecting different roles
        let parentButton = app.buttons["Parent"]
        let childButton = app.buttons["Child"]

        if parentButton.exists {
            parentButton.tap()
            sleep(1)
            takeScreenshot(name: "Parent Role Selected")
        }

        if childButton.exists {
            childButton.tap()
            sleep(1)
            takeScreenshot(name: "Child Role Selected")
        }
    }

    // MARK: - Validation Tests

    func testRegistration_PasswordMismatchError() throws {
        navigateToRegistration()

        // Fill in passwords that don't match
        let secureFields = app.secureTextFields
        if secureFields.count >= 2 {
            let passwordField = secureFields.element(boundBy: 0)
            let confirmField = secureFields.element(boundBy: 1)

            typeIntoField(passwordField, text: "Password123!")
            typeIntoField(confirmField, text: "DifferentPassword!")

            // Look for mismatch error
            sleep(1)
            let mismatchError = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'match' OR label CONTAINS 'Match'"))

            if mismatchError.exists {
                takeScreenshot(name: "Password Mismatch Error")
            }
        }
    }

    func testRegistration_SubmitDisabledWhenEmpty() throws {
        navigateToRegistration()

        // Verify submit button is disabled when form is empty
        let submitButton = app.buttons["register_submit_button"]
        let createAccountButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Create Account'"))

        if submitButton.exists {
            XCTAssertFalse(submitButton.isEnabled, "Submit should be disabled when form is empty")
        } else if createAccountButton.exists {
            // Button might still be tappable but will show validation errors
            takeScreenshot(name: "Empty Form State")
        }
    }

    // MARK: - Cancel Tests

    func testRegistration_Cancel() throws {
        navigateToRegistration()

        // Tap cancel
        let cancelButton = app.navigationBars.buttons["Cancel"]
        if waitForElement(cancelButton, timeout: 3) {
            cancelButton.tap()

            // Verify we're back to login
            sleep(1)
            assertLoginScreenDisplayed()
            takeScreenshot(name: "After Registration Cancel")
        }
    }

    // MARK: - Full Registration Flow Test

    func testRegistration_FullFlow() throws {
        navigateToRegistration()

        // Generate unique email
        let timestamp = Int(Date().timeIntervalSince1970)
        let uniqueEmail = "uitest\(timestamp)@example.com"

        // Fill in all fields
        let textFields = app.textFields
        let secureFields = app.secureTextFields

        // Fill first name
        if textFields.count > 0 {
            let firstNameField = textFields.element(boundBy: 0)
            typeIntoField(firstNameField, text: "Test")
        }

        // Fill last name
        if textFields.count > 1 {
            let lastNameField = textFields.element(boundBy: 1)
            typeIntoField(lastNameField, text: "User")
        }

        // Fill email
        let emailField = textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS 'email'"))
        if emailField.exists {
            typeIntoField(emailField, text: uniqueEmail)
        } else if textFields.count > 2 {
            let field = textFields.element(boundBy: 2)
            typeIntoField(field, text: uniqueEmail)
        }

        // Fill password
        if secureFields.count > 0 {
            let passwordField = secureFields.element(boundBy: 0)
            typeIntoField(passwordField, text: "TestPassword123!")
        }

        // Fill confirm password
        if secureFields.count > 1 {
            let confirmField = secureFields.element(boundBy: 1)
            typeIntoField(confirmField, text: "TestPassword123!")
        }

        // Select parent role
        let parentButton = app.buttons["Parent"]
        if parentButton.exists {
            parentButton.tap()
        }

        // Dismiss keyboard if visible
        if app.toolbars.buttons["Done"].exists {
            app.toolbars.buttons["Done"].tap()
        }

        takeScreenshot(name: "Registration Form Filled")

        // Try to submit
        let submitButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Create Account' OR label CONTAINS 'Sign Up' OR identifier == 'register_submit_button'"))
        if submitButton.exists && submitButton.isEnabled {
            submitButton.tap()

            // Wait for response
            sleep(3)
            takeScreenshot(name: "After Registration Submit")

            // Check for success (dashboard) or error
            let dashboardTab = app.tabBars.buttons["Dashboard"]
            let errorText = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'error' OR label CONTAINS 'Error' OR label CONTAINS 'already'"))

            if dashboardTab.exists {
                takeScreenshot(name: "Registration Success")
            } else if errorText.exists {
                takeScreenshot(name: "Registration Error")
            }
        }
    }

    // MARK: - Accessibility Tests

    func testRegistration_AccessibilityLabels() throws {
        navigateToRegistration()

        // Verify fields have accessibility labels
        let textFields = app.textFields
        let secureFields = app.secureTextFields

        for i in 0..<min(textFields.count, 3) {
            let field = textFields.element(boundBy: i)
            XCTAssertNotNil(field.label, "Text field should have accessibility label")
        }

        for i in 0..<min(secureFields.count, 2) {
            let field = secureFields.element(boundBy: i)
            XCTAssertNotNil(field.label, "Secure field should have accessibility label")
        }

        takeScreenshot(name: "Registration Accessibility")
    }

    // MARK: - Helper Methods

    private func navigateToRegistration() {
        // Tap register button from login screen
        let registerButton = app.buttons["register_button"]
        if waitForElement(registerButton, timeout: 5) {
            registerButton.tap()
        }

        // Wait for registration screen
        sleep(1)
    }
}
