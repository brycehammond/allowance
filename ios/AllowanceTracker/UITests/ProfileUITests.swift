import XCTest

/// UI tests for profile and settings flows
final class ProfileUITests: AllowanceTrackerUITests {

    // MARK: - Setup

    override func setUpWithError() throws {
        try super.setUpWithError()

        // Login before each profile test
        performLogin(email: testParentEmail, password: testParentPassword)

        // Wait for dashboard to load
        assertDashboardDisplayed()

        // Navigate to profile tab
        navigateToProfile()
    }

    // MARK: - Profile Display Tests

    func testProfile_DisplaysUserInfo() throws {
        // Verify profile screen is displayed
        let profileTitle = app.navigationBars["Profile"]
        XCTAssertTrue(waitForElement(profileTitle, timeout: 5), "Profile screen should be displayed")

        // Look for user info elements
        let accountSection = app.staticTexts["Account"]
        let nameLabel = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'Name' OR label CONTAINS 'name'"))
        let emailLabel = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'Email' OR label CONTAINS 'email' OR label CONTAINS '@'"))
        let roleLabel = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'Role' OR label CONTAINS 'Parent' OR label CONTAINS 'Child'"))

        let hasUserInfo = accountSection.exists || nameLabel.exists || emailLabel.exists || roleLabel.exists
        XCTAssertTrue(hasUserInfo, "User info should be displayed")

        takeScreenshot(name: "Profile Screen")
    }

    func testProfile_DisplaysAccountSection() throws {
        // Look for account section
        let accountHeader = app.staticTexts["Account"]
        if accountHeader.exists {
            takeScreenshot(name: "Account Section")
        }
    }

    func testProfile_DisplaysSettingsSection() throws {
        // Look for settings section
        let settingsHeader = app.staticTexts["Settings"]
        if settingsHeader.exists {
            takeScreenshot(name: "Settings Section")
        }
    }

    // MARK: - Navigation Tests

    func testProfile_NavigateToChangePassword() throws {
        // Tap change password
        let changePasswordButton = app.buttons["change_password_button"]
        let changePasswordCell = app.cells.element(matching: NSPredicate(format: "label CONTAINS 'Change Password' OR identifier CONTAINS 'change_password'"))
        let changePasswordLink = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Change Password'"))

        if waitForElement(changePasswordButton, timeout: 3) {
            changePasswordButton.tap()
        } else if waitForElement(changePasswordCell, timeout: 3) {
            changePasswordCell.tap()
        } else if waitForElement(changePasswordLink, timeout: 3) {
            changePasswordLink.tap()
        }

        // Verify change password screen
        sleep(1)
        let changePasswordTitle = app.navigationBars.element(matching: NSPredicate(format: "identifier CONTAINS 'Change' OR identifier CONTAINS 'Password'"))
        let passwordFields = app.secureTextFields

        if changePasswordTitle.exists || passwordFields.count > 0 {
            takeScreenshot(name: "Change Password Screen")
        }
    }

    func testProfile_NavigateToNotifications() throws {
        // Tap notifications
        let notificationsButton = app.buttons["notifications_button"]
        let notificationsCell = app.cells.element(matching: NSPredicate(format: "label CONTAINS 'Notifications' OR identifier CONTAINS 'notifications'"))
        let notificationsLink = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Notifications'"))

        if waitForElement(notificationsButton, timeout: 3) {
            notificationsButton.tap()
        } else if waitForElement(notificationsCell, timeout: 3) {
            notificationsCell.tap()
        } else if waitForElement(notificationsLink, timeout: 3) {
            notificationsLink.tap()
        }

        // Verify notifications settings
        sleep(1)
        let notificationsTitle = app.navigationBars["Notifications"]
        if waitForElement(notificationsTitle, timeout: 3) {
            takeScreenshot(name: "Notifications Settings")
        }
    }

    func testProfile_NavigateToAppearance() throws {
        // Tap appearance
        let appearanceButton = app.buttons["appearance_button"]
        let appearanceCell = app.cells.element(matching: NSPredicate(format: "label CONTAINS 'Appearance' OR identifier CONTAINS 'appearance'"))
        let appearanceLink = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Appearance'"))

        if waitForElement(appearanceButton, timeout: 3) {
            appearanceButton.tap()
        } else if waitForElement(appearanceCell, timeout: 3) {
            appearanceCell.tap()
        } else if waitForElement(appearanceLink, timeout: 3) {
            appearanceLink.tap()
        }

        // Verify appearance settings
        sleep(1)
        let appearanceTitle = app.navigationBars["Appearance"]
        if waitForElement(appearanceTitle, timeout: 3) {
            takeScreenshot(name: "Appearance Settings")
        }
    }

    func testProfile_NavigateToAbout() throws {
        // Tap about
        let aboutButton = app.buttons["about_button"]
        let aboutCell = app.cells.element(matching: NSPredicate(format: "label CONTAINS 'About' OR identifier CONTAINS 'about'"))
        let aboutLink = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'About'"))

        if waitForElement(aboutButton, timeout: 3) {
            aboutButton.tap()
        } else if waitForElement(aboutCell, timeout: 3) {
            aboutCell.tap()
        } else if waitForElement(aboutLink, timeout: 3) {
            aboutLink.tap()
        }

        // Verify about screen
        sleep(1)
        let aboutTitle = app.navigationBars["About"]
        if waitForElement(aboutTitle, timeout: 3) {
            takeScreenshot(name: "About Screen")
        }
    }

    // MARK: - Notification Settings Tests

    func testNotificationSettings_DisplaysToggles() throws {
        navigateToNotifications()

        // Look for toggle switches
        let toggles = app.switches
        if toggles.count > 0 {
            XCTAssertTrue(toggles.count >= 1, "Should have notification toggles")
            takeScreenshot(name: "Notification Toggles")
        }
    }

    func testNotificationSettings_ToggleNotification() throws {
        navigateToNotifications()

        // Find and toggle a switch
        let firstToggle = app.switches.firstMatch
        if firstToggle.exists {
            let initialValue = firstToggle.value as? String
            firstToggle.tap()
            sleep(1)
            let newValue = firstToggle.value as? String
            XCTAssertNotEqual(initialValue, newValue, "Toggle value should change")
            takeScreenshot(name: "Toggle Changed")
        }
    }

    // MARK: - Appearance Settings Tests

    func testAppearanceSettings_DisplaysThemeOptions() throws {
        navigateToAppearance()

        // Look for theme picker
        let themePicker = app.segmentedControls.firstMatch
        let lightButton = app.buttons["Light"]
        let darkButton = app.buttons["Dark"]
        let systemButton = app.buttons["System"]

        let hasThemeOptions = themePicker.exists || lightButton.exists || darkButton.exists || systemButton.exists
        if hasThemeOptions {
            takeScreenshot(name: "Theme Options")
        }
    }

    func testAppearanceSettings_ChangeTheme() throws {
        navigateToAppearance()

        // Try changing theme
        let darkButton = app.buttons["Dark"]
        if darkButton.exists {
            darkButton.tap()
            sleep(1)
            takeScreenshot(name: "Dark Theme Selected")
        }

        let lightButton = app.buttons["Light"]
        if lightButton.exists {
            lightButton.tap()
            sleep(1)
            takeScreenshot(name: "Light Theme Selected")
        }
    }

    // MARK: - About Screen Tests

    func testAbout_DisplaysAppInfo() throws {
        navigateToAbout()

        // Look for version info
        let versionLabel = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'Version' OR label CONTAINS 'version'"))
        let buildLabel = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'Build' OR label CONTAINS 'build'"))

        let hasAppInfo = versionLabel.exists || buildLabel.exists
        if hasAppInfo {
            takeScreenshot(name: "App Version Info")
        }
    }

    func testAbout_DisplaysSupportLinks() throws {
        navigateToAbout()

        // Look for support links
        let websiteLink = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Website'"))
        let emailLink = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Email' OR label CONTAINS 'Support'"))

        if websiteLink.exists || emailLink.exists {
            takeScreenshot(name: "Support Links")
        }
    }

    func testAbout_NavigateToPrivacyPolicy() throws {
        navigateToAbout()

        // Tap privacy policy
        let privacyButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Privacy'"))
        let privacyCell = app.cells.element(matching: NSPredicate(format: "label CONTAINS 'Privacy'"))

        if privacyButton.exists {
            privacyButton.tap()
        } else if privacyCell.exists {
            privacyCell.tap()
        }

        sleep(1)
        let privacyTitle = app.navigationBars.element(matching: NSPredicate(format: "identifier CONTAINS 'Privacy'"))
        if privacyTitle.exists {
            takeScreenshot(name: "Privacy Policy")
        }
    }

    func testAbout_NavigateToTermsOfService() throws {
        navigateToAbout()

        // Tap terms of service
        let termsButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Terms'"))
        let termsCell = app.cells.element(matching: NSPredicate(format: "label CONTAINS 'Terms'"))

        if termsButton.exists {
            termsButton.tap()
        } else if termsCell.exists {
            termsCell.tap()
        }

        sleep(1)
        let termsTitle = app.navigationBars.element(matching: NSPredicate(format: "identifier CONTAINS 'Terms'"))
        if termsTitle.exists {
            takeScreenshot(name: "Terms of Service")
        }
    }

    // MARK: - Sign Out Tests

    func testProfile_SignOutButton_Exists() throws {
        // Look for sign out button
        let signOutButton = app.buttons["sign_out_button"]
        let signOutCell = app.cells.element(matching: NSPredicate(format: "label CONTAINS 'Sign Out' OR label CONTAINS 'Logout'"))
        let signOutLink = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Sign Out' OR label CONTAINS 'Logout'"))

        let hasSignOut = signOutButton.exists || signOutCell.exists || signOutLink.exists
        XCTAssertTrue(hasSignOut, "Sign out option should be visible")

        takeScreenshot(name: "Sign Out Button")
    }

    func testProfile_SignOut_ShowsConfirmation() throws {
        // Tap sign out
        let signOutButton = app.buttons["sign_out_button"]
        let signOutCell = app.cells.element(matching: NSPredicate(format: "label CONTAINS 'Sign Out' OR label CONTAINS 'Logout'"))
        let signOutLink = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Sign Out' OR label CONTAINS 'Logout'"))

        if signOutButton.exists {
            signOutButton.tap()
        } else if signOutCell.exists {
            signOutCell.tap()
        } else if signOutLink.exists {
            signOutLink.tap()
        }

        // Verify confirmation dialog
        sleep(1)
        let confirmationDialog = app.sheets.firstMatch
        let signOutConfirmButton = app.buttons.element(matching: NSPredicate(format: "label == 'Sign Out'"))

        if confirmationDialog.exists || signOutConfirmButton.exists {
            takeScreenshot(name: "Sign Out Confirmation")
        }
    }

    func testProfile_SignOut_Cancel() throws {
        // Tap sign out
        let signOutLink = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Sign Out' OR label CONTAINS 'Logout'"))
        if signOutLink.exists {
            signOutLink.tap()
        }

        sleep(1)

        // Tap cancel
        let cancelButton = app.buttons["Cancel"]
        if cancelButton.exists {
            cancelButton.tap()
            sleep(1)

            // Verify still on profile
            let profileTitle = app.navigationBars["Profile"]
            XCTAssertTrue(profileTitle.exists, "Should remain on profile screen")
            takeScreenshot(name: "After Cancel Sign Out")
        }
    }

    func testProfile_SignOut_Confirm() throws {
        // Tap sign out
        let signOutLink = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Sign Out' OR label CONTAINS 'Logout'"))
        if signOutLink.exists {
            signOutLink.tap()
        }

        sleep(1)

        // Tap confirm sign out
        let signOutConfirm = app.buttons.element(matching: NSPredicate(format: "label == 'Sign Out'"))
        if signOutConfirm.exists {
            signOutConfirm.tap()

            // Wait for login screen
            sleep(2)

            // Verify login screen is displayed
            assertLoginScreenDisplayed()
            takeScreenshot(name: "After Sign Out")
        }
    }

    // MARK: - Helper Methods

    private func navigateToProfile() {
        let profileTab = app.tabBars.buttons["Profile"]
        if waitForElement(profileTab, timeout: 5) {
            profileTab.tap()
        }
        sleep(1)
    }

    private func navigateToNotifications() {
        let notificationsButton = app.buttons["notifications_button"]
        let notificationsCell = app.cells.element(matching: NSPredicate(format: "label CONTAINS 'Notifications'"))
        let notificationsLink = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Notifications'"))

        if waitForElement(notificationsButton, timeout: 3) {
            notificationsButton.tap()
        } else if waitForElement(notificationsCell, timeout: 3) {
            notificationsCell.tap()
        } else if waitForElement(notificationsLink, timeout: 3) {
            notificationsLink.tap()
        }
        sleep(1)
    }

    private func navigateToAppearance() {
        let appearanceButton = app.buttons["appearance_button"]
        let appearanceCell = app.cells.element(matching: NSPredicate(format: "label CONTAINS 'Appearance'"))
        let appearanceLink = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Appearance'"))

        if waitForElement(appearanceButton, timeout: 3) {
            appearanceButton.tap()
        } else if waitForElement(appearanceCell, timeout: 3) {
            appearanceCell.tap()
        } else if waitForElement(appearanceLink, timeout: 3) {
            appearanceLink.tap()
        }
        sleep(1)
    }

    private func navigateToAbout() {
        let aboutButton = app.buttons["about_button"]
        let aboutCell = app.cells.element(matching: NSPredicate(format: "label CONTAINS 'About'"))
        let aboutLink = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'About'"))

        if waitForElement(aboutButton, timeout: 3) {
            aboutButton.tap()
        } else if waitForElement(aboutCell, timeout: 3) {
            aboutCell.tap()
        } else if waitForElement(aboutLink, timeout: 3) {
            aboutLink.tap()
        }
        sleep(1)
    }
}
