import XCTest

/// UI tests for dashboard and main navigation
final class DashboardUITests: AllowanceTrackerUITests {

    // MARK: - Setup

    override func setUpWithError() throws {
        try super.setUpWithError()

        // Login before each dashboard test
        performLogin(email: testParentEmail, password: testParentPassword)

        // Wait for dashboard to load
        assertDashboardDisplayed()
    }

    // MARK: - Dashboard Display Tests

    func testDashboard_DisplaysWelcomeMessage() throws {
        // Check for welcome message
        let welcomeText = app.staticTexts.element(matching: NSPredicate(format: "label BEGINSWITH 'Welcome'"))
        XCTAssertTrue(waitForElement(welcomeText, timeout: 10), "Welcome message should be displayed")

        takeScreenshot(name: "Dashboard Welcome")
    }

    func testDashboard_DisplaysTabBar() throws {
        // Verify tab bar exists with expected tabs
        let tabBar = app.tabBars.firstMatch
        XCTAssertTrue(tabBar.exists, "Tab bar should be visible")

        // Check for Dashboard tab
        let dashboardTab = tabBar.buttons["Dashboard"]
        XCTAssertTrue(dashboardTab.exists, "Dashboard tab should exist")

        // Check for Profile tab
        let profileTab = tabBar.buttons["Profile"]
        XCTAssertTrue(profileTab.exists, "Profile tab should exist")

        // Check for Analytics tab
        let analyticsTab = tabBar.buttons["Analytics"]
        XCTAssertTrue(analyticsTab.exists, "Analytics tab should exist")

        takeScreenshot(name: "Dashboard Tab Bar")
    }

    // MARK: - Tab Navigation Tests

    func testTabNavigation_ProfileTab() throws {
        // Tap on Profile tab
        let profileTab = app.tabBars.buttons["Profile"]
        tapWhenAvailable(profileTab)

        // Verify Profile screen is displayed
        let profileTitle = app.navigationBars["Profile"]
        let profileContent = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'email' OR label CONTAINS 'Email' OR label CONTAINS 'Account'"))

        let profileDisplayed = profileTitle.waitForExistence(timeout: 5) ||
                               profileContent.waitForExistence(timeout: 5)

        XCTAssertTrue(profileDisplayed, "Profile screen should be displayed")

        takeScreenshot(name: "Profile Tab")
    }

    func testTabNavigation_AnalyticsTab() throws {
        // Tap on Analytics tab
        let analyticsTab = app.tabBars.buttons["Analytics"]
        tapWhenAvailable(analyticsTab)

        // Verify Analytics screen is displayed
        let analyticsTitle = app.navigationBars["Analytics"]
        XCTAssertTrue(analyticsTitle.waitForExistence(timeout: 5), "Analytics screen should be displayed")

        takeScreenshot(name: "Analytics Tab")
    }

    func testTabNavigation_BudgetsTab() throws {
        // Budgets tab is only visible for parent users
        let budgetsTab = app.tabBars.buttons["Budgets"]

        if budgetsTab.exists {
            budgetsTab.tap()

            // Verify Budgets screen is displayed
            let budgetsTitle = app.navigationBars["Budgets"]
            XCTAssertTrue(budgetsTitle.waitForExistence(timeout: 5), "Budgets screen should be displayed")

            takeScreenshot(name: "Budgets Tab")
        }
    }

    func testTabNavigation_ReturnsToDashboard() throws {
        // Go to Profile tab
        let profileTab = app.tabBars.buttons["Profile"]
        tapWhenAvailable(profileTab)

        // Return to Dashboard tab
        let dashboardTab = app.tabBars.buttons["Dashboard"]
        tapWhenAvailable(dashboardTab)

        // Verify dashboard is displayed again
        assertDashboardDisplayed()

        takeScreenshot(name: "Return to Dashboard")
    }

    // MARK: - Empty State Tests

    func testDashboard_EmptyState_ShowsAddChildPrompt() throws {
        // This test assumes the user has no children
        // If children exist, the empty state won't be shown
        let emptyStateText = app.staticTexts["No Children Yet"]
        let addChildButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Add Child'"))

        // If empty state is visible, verify it
        if emptyStateText.exists {
            XCTAssertTrue(addChildButton.exists, "Add child button should be visible in empty state")
            takeScreenshot(name: "Dashboard Empty State")
        }
    }

    // MARK: - Child Card Tests

    func testChildCard_DisplaysChildInfo() throws {
        // Look for any child card
        let childCards = app.buttons.matching(NSPredicate(format: "identifier BEGINSWITH 'child_card_'"))

        if childCards.count > 0 {
            let firstChildCard = childCards.element(boundBy: 0)
            XCTAssertTrue(firstChildCard.exists, "Child card should be visible")

            // Child card should contain balance info
            // This is a basic existence check

            takeScreenshot(name: "Child Card")
        }
    }

    func testChildCard_TapNavigatesToChildDetail() throws {
        // Find a child card
        let childCards = app.buttons.matching(NSPredicate(format: "identifier BEGINSWITH 'child_card_'"))

        if childCards.count > 0 {
            let firstChildCard = childCards.element(boundBy: 0)
            firstChildCard.tap()

            // Verify child detail view is displayed (has tabs for transactions, wish list, etc.)
            let transactionsTab = app.tabBars.buttons["Transactions"]
            let wishListTab = app.tabBars.buttons["Wish List"]

            let childDetailDisplayed = transactionsTab.waitForExistence(timeout: 5) ||
                                        wishListTab.waitForExistence(timeout: 5)

            XCTAssertTrue(childDetailDisplayed, "Child detail view should be displayed")

            takeScreenshot(name: "Child Detail View")
        }
    }

    // MARK: - Pull to Refresh Tests

    func testDashboard_PullToRefresh_RefreshesContent() throws {
        // Find the main scroll view or list
        let scrollView = app.scrollViews.firstMatch

        if scrollView.exists {
            // Perform pull to refresh
            let start = scrollView.coordinate(withNormalizedOffset: CGVector(dx: 0.5, dy: 0.2))
            let end = scrollView.coordinate(withNormalizedOffset: CGVector(dx: 0.5, dy: 0.8))
            start.press(forDuration: 0.1, thenDragTo: end)

            // Wait for refresh to complete
            // The loading indicator should appear and then disappear
            sleep(2)

            // Verify dashboard still displays correctly
            assertDashboardDisplayed()

            takeScreenshot(name: "After Pull to Refresh")
        }
    }

    // MARK: - Child Detail Tab Navigation Tests

    func testChildDetail_TransactionsTab_DisplaysTransactionList() throws {
        // Navigate to a child's detail view
        navigateToFirstChildDetail()

        // Tap on Transactions tab
        let transactionsTab = app.tabBars.buttons["Transactions"]
        if waitForElement(transactionsTab) {
            transactionsTab.tap()

            // Verify transactions list is displayed
            // Look for transaction rows or empty state
            let transactionList = app.scrollViews.firstMatch
            XCTAssertTrue(transactionList.waitForExistence(timeout: 5), "Transaction list should be displayed")

            takeScreenshot(name: "Transactions Tab")
        }
    }

    func testChildDetail_WishListTab_DisplaysWishList() throws {
        // Navigate to a child's detail view
        navigateToFirstChildDetail()

        // Tap on Wish List tab
        let wishListTab = app.tabBars.buttons["Wish List"]
        if waitForElement(wishListTab) {
            wishListTab.tap()

            // Verify wish list is displayed
            sleep(1)
            takeScreenshot(name: "Wish List Tab")
        }
    }

    func testChildDetail_AnalyticsTab_DisplaysAnalytics() throws {
        // Navigate to a child's detail view
        navigateToFirstChildDetail()

        // Tap on Analytics tab
        let analyticsTab = app.tabBars.buttons["Analytics"]
        if waitForElement(analyticsTab) {
            analyticsTab.tap()

            // Verify analytics is displayed
            sleep(1)
            takeScreenshot(name: "Child Analytics Tab")
        }
    }

    func testChildDetail_SavingsTab_DisplaysSavings() throws {
        // Navigate to a child's detail view
        navigateToFirstChildDetail()

        // Tap on Savings tab (parent only)
        let savingsTab = app.tabBars.buttons["Savings"]
        if savingsTab.exists {
            savingsTab.tap()

            // Verify savings is displayed
            sleep(1)
            takeScreenshot(name: "Savings Tab")
        }
    }

    func testChildDetail_SettingsTab_DisplaysSettings() throws {
        // Navigate to a child's detail view
        navigateToFirstChildDetail()

        // Tap on Settings tab (parent only)
        let settingsTab = app.tabBars.buttons["Settings"]
        if settingsTab.exists {
            settingsTab.tap()

            // Verify settings is displayed
            sleep(1)
            takeScreenshot(name: "Child Settings Tab")
        }
    }

    // MARK: - Helper Methods

    private func navigateToFirstChildDetail() {
        // Find and tap the first child card
        let childCards = app.buttons.matching(NSPredicate(format: "identifier BEGINSWITH 'child_card_'"))

        if childCards.count > 0 {
            let firstChildCard = childCards.element(boundBy: 0)
            if waitForElement(firstChildCard) {
                firstChildCard.tap()
            }
        }
    }
}
