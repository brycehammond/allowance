import XCTest

/// UI Tests specifically for generating App Store screenshots
/// Run with: fastlane snapshot
class ScreenshotUITests: XCTestCase {

    var app: XCUIApplication!

    override func setUpWithError() throws {
        try super.setUpWithError()
        continueAfterFailure = true

        app = XCUIApplication()

        // Configure snapshot if available
        setupSnapshot(app)

        app.launchArguments.append("--uitesting")
        app.launchArguments.append("--screenshots")
        app.launchEnvironment["UITEST_MODE"] = "1"

        app.launch()
    }

    override func tearDownWithError() throws {
        app = nil
        try super.tearDownWithError()
    }

    // MARK: - Screenshot Tests

    func testTakeAppStoreScreenshots() throws {
        // 1. Login Screen
        snapshot("01_Login")

        // Attempt login
        let emailField = app.textFields["login_email_field"]
        let passwordField = app.secureTextFields["login_password_field"]
        let loginButton = app.buttons["login_button"]

        // Check if we're on login screen
        if emailField.waitForExistence(timeout: 5) {
            emailField.tap()
            emailField.typeText("demo@earnandlearn.app")

            passwordField.tap()
            passwordField.typeText("DemoPass123!")

            loginButton.tap()
        }

        // Wait for dashboard to load
        sleep(3)

        // 2. Dashboard - Family Overview
        snapshot("02_Dashboard")

        // Try to navigate to child detail
        let childCard = app.buttons.matching(identifier: "child_card").firstMatch
        if childCard.waitForExistence(timeout: 5) {
            childCard.tap()
            sleep(2)

            // 3. Child Detail - Balance View
            snapshot("03_ChildDetail")

            // 4. Transactions Tab
            let transactionsTab = app.buttons["Transactions"]
            if transactionsTab.exists {
                transactionsTab.tap()
                sleep(1)
                snapshot("04_Transactions")
            }

            // 5. Wish List Tab
            let wishListTab = app.buttons["Wish List"]
            if wishListTab.exists {
                wishListTab.tap()
                sleep(1)
                snapshot("05_WishList")
            }

            // 6. Savings Tab
            let savingsTab = app.buttons["Savings"]
            if savingsTab.exists {
                savingsTab.tap()
                sleep(1)
                snapshot("06_Savings")
            }

            // 7. Analytics Tab
            let analyticsTab = app.buttons["Analytics"]
            if analyticsTab.exists {
                analyticsTab.tap()
                sleep(2) // Charts need time to render
                snapshot("07_Analytics")
            }

            // Go back to dashboard
            let backButton = app.navigationBars.buttons.firstMatch
            if backButton.exists {
                backButton.tap()
                sleep(1)
            }
        }

        // 8. Add Transaction Sheet (if accessible)
        let addButton = app.buttons["add_transaction_button"]
        if addButton.waitForExistence(timeout: 3) {
            addButton.tap()
            sleep(1)
            snapshot("08_AddTransaction")

            // Dismiss
            let cancelButton = app.buttons["Cancel"]
            if cancelButton.exists {
                cancelButton.tap()
            }
        }

        // 9. Profile/Settings (via tab bar)
        let profileTab = app.tabBars.buttons["Profile"]
        if profileTab.exists {
            profileTab.tap()
            sleep(1)
            snapshot("09_Profile")
        }
    }

    // MARK: - Individual Screenshot Tests (for selective runs)

    func testLoginScreenshot() throws {
        snapshot("Login")
    }

    func testDashboardScreenshot() throws {
        performLogin()
        sleep(2)
        snapshot("Dashboard")
    }

    func testChildDetailScreenshot() throws {
        performLogin()
        navigateToFirstChild()
        snapshot("ChildDetail")
    }

    func testTransactionsScreenshot() throws {
        performLogin()
        navigateToFirstChild()
        tapTab("Transactions")
        snapshot("Transactions")
    }

    func testWishListScreenshot() throws {
        performLogin()
        navigateToFirstChild()
        tapTab("Wish List")
        snapshot("WishList")
    }

    func testAnalyticsScreenshot() throws {
        performLogin()
        navigateToFirstChild()
        tapTab("Analytics")
        sleep(2) // Wait for charts
        snapshot("Analytics")
    }

    // MARK: - Helper Methods

    private func performLogin() {
        let emailField = app.textFields["login_email_field"]
        let passwordField = app.secureTextFields["login_password_field"]
        let loginButton = app.buttons["login_button"]

        guard emailField.waitForExistence(timeout: 5) else { return }

        emailField.tap()
        emailField.typeText("demo@earnandlearn.app")
        passwordField.tap()
        passwordField.typeText("DemoPass123!")
        loginButton.tap()

        sleep(3)
    }

    private func navigateToFirstChild() {
        let childCard = app.buttons.matching(identifier: "child_card").firstMatch
        if childCard.waitForExistence(timeout: 5) {
            childCard.tap()
            sleep(2)
        }
    }

    private func tapTab(_ name: String) {
        let tab = app.buttons[name]
        if tab.waitForExistence(timeout: 3) {
            tab.tap()
            sleep(1)
        }
    }
}
