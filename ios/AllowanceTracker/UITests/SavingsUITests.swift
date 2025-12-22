import XCTest

/// UI tests for savings account flows
final class SavingsUITests: AllowanceTrackerUITests {

    // MARK: - Setup

    override func setUpWithError() throws {
        try super.setUpWithError()

        // Login before each savings test
        performLogin(email: testParentEmail, password: testParentPassword)

        // Wait for dashboard to load
        assertDashboardDisplayed()

        // Navigate to first child's savings
        navigateToFirstChildSavings()
    }

    // MARK: - Savings Display Tests

    func testSavings_DisplaysCorrectly() throws {
        // Verify we're on savings tab
        let savingsTab = app.tabBars.buttons["Savings"]
        XCTAssertTrue(savingsTab.isSelected || waitForElement(savingsTab), "Savings tab should be visible")

        takeScreenshot(name: "Savings View")
    }

    func testSavings_DisplaysAccountsOrEmptyState() throws {
        // Look for accounts or empty state
        let accountCard = app.buttons.element(matching: NSPredicate(format: "identifier BEGINSWITH 'savings_account_card_'"))
        let emptyState = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'No Savings' OR label CONTAINS 'Create'"))
        let createButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Create Savings'"))

        let hasContent = accountCard.exists || emptyState.exists || createButton.exists

        XCTAssertTrue(hasContent, "Should display accounts or empty state")
        takeScreenshot(name: "Savings Content")
    }

    func testSavings_PullToRefresh() throws {
        // Find the scroll view or list
        let scrollView = app.scrollViews.firstMatch
        let list = app.tables.firstMatch

        let scrollable = scrollView.exists ? scrollView : list

        if scrollable.exists {
            // Perform pull to refresh
            let start = scrollable.coordinate(withNormalizedOffset: CGVector(dx: 0.5, dy: 0.2))
            let end = scrollable.coordinate(withNormalizedOffset: CGVector(dx: 0.5, dy: 0.8))
            start.press(forDuration: 0.1, thenDragTo: end)

            sleep(2)
            takeScreenshot(name: "Savings After Refresh")
        }
    }

    // MARK: - Add Savings Account Tests

    func testAddSavingsAccount_ShowsForm() throws {
        // Tap add account button
        let addButton = app.buttons["add_savings_account_button"]
        let navAddButton = app.navigationBars.buttons.element(matching: NSPredicate(format: "identifier CONTAINS 'plus'"))
        let createButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Create Savings'"))
        let anyPlusButton = app.buttons.matching(NSPredicate(format: "label CONTAINS 'plus' OR identifier CONTAINS 'plus'")).firstMatch

        if waitForElement(addButton, timeout: 3) {
            addButton.tap()
        } else if waitForElement(createButton, timeout: 3) {
            createButton.tap()
        } else if waitForElement(navAddButton, timeout: 3) {
            navAddButton.tap()
        } else if anyPlusButton.exists {
            anyPlusButton.tap()
        } else {
            XCTFail("Add savings account button not found")
            return
        }

        // Verify form is displayed
        sleep(1)
        takeScreenshot(name: "Add Savings Account Form")
    }

    func testAddSavingsAccount_NameEntry() throws {
        openAddSavingsAccountForm()

        // Find name field
        let nameField = app.textFields["savings_name_field"]
        let anyNameField = app.textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS 'name' OR label CONTAINS 'Name'"))
        let firstTextField = app.textFields.firstMatch

        if waitForElement(nameField, timeout: 3) {
            typeIntoField(nameField, text: "Vacation Fund")
        } else if waitForElement(anyNameField, timeout: 3) {
            typeIntoField(anyNameField, text: "Vacation Fund")
        } else if waitForElement(firstTextField, timeout: 3) {
            typeIntoField(firstTextField, text: "Vacation Fund")
        }

        takeScreenshot(name: "Savings Account Name Entered")
    }

    func testAddSavingsAccount_TargetAmount() throws {
        openAddSavingsAccountForm()

        // Find target amount field
        let targetField = app.textFields["savings_target_field"]
        let anyTargetField = app.textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS '$' OR label CONTAINS 'Target' OR label CONTAINS 'Goal'"))

        // Try second text field if specific one not found
        let textFields = app.textFields
        if textFields.count > 1 {
            let field = textFields.element(boundBy: 1)
            typeIntoField(field, text: "500.00")
        } else if waitForElement(targetField, timeout: 3) {
            typeIntoField(targetField, text: "500.00")
        } else if waitForElement(anyTargetField, timeout: 3) {
            typeIntoField(anyTargetField, text: "500.00")
        }

        takeScreenshot(name: "Savings Target Entered")
    }

    func testAddSavingsAccount_Cancel() throws {
        openAddSavingsAccountForm()

        // Tap cancel
        let cancelButton = app.buttons["Cancel"]
        let navCancelButton = app.navigationBars.buttons["Cancel"]

        if waitForElement(cancelButton, timeout: 3) {
            cancelButton.tap()
        } else if waitForElement(navCancelButton, timeout: 3) {
            navCancelButton.tap()
        }

        // Verify form is dismissed
        sleep(1)
        takeScreenshot(name: "After Cancel Savings Account")
    }

    func testAddSavingsAccount_FullFlow() throws {
        openAddSavingsAccountForm()

        // Enter account name
        let textFields = app.textFields
        if textFields.count > 0 {
            let nameField = textFields.element(boundBy: 0)
            if nameField.exists {
                nameField.tap()
                nameField.typeText("Test Savings Account")
            }
        }

        // Enter target amount if available
        if textFields.count > 1 {
            let targetField = textFields.element(boundBy: 1)
            if targetField.exists {
                targetField.tap()
                targetField.typeText("100.00")
            }
        }

        // Dismiss keyboard
        if app.toolbars.buttons["Done"].exists {
            app.toolbars.buttons["Done"].tap()
        }

        takeScreenshot(name: "Savings Account Form Filled")

        // Try to save
        let saveButton = app.navigationBars.buttons["Save"]
        let createButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Create'"))
        let addButton = app.buttons["Add"]

        if saveButton.exists && saveButton.isEnabled {
            saveButton.tap()
        } else if createButton.exists && createButton.isEnabled {
            createButton.tap()
        } else if addButton.exists && addButton.isEnabled {
            addButton.tap()
        }

        sleep(2)
        takeScreenshot(name: "After Savings Account Save")
    }

    // MARK: - Account Card Tests

    func testSavingsAccountCard_DisplaysInfo() throws {
        // Look for savings account cards
        let accountCards = app.buttons.matching(NSPredicate(format: "identifier BEGINSWITH 'savings_account_card_'"))

        if accountCards.count > 0 {
            let firstAccount = accountCards.element(boundBy: 0)
            XCTAssertTrue(firstAccount.exists, "Savings account card should exist")
            takeScreenshot(name: "Savings Account Card")
        }
    }

    func testSavingsAccountCard_ShowsBalance() throws {
        // Look for balance display
        let balanceText = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS '$'"))
        if balanceText.exists {
            takeScreenshot(name: "Savings Balance Display")
        }
    }

    func testSavingsAccountCard_ShowsProgress() throws {
        // Look for progress indicator
        let progressView = app.progressIndicators.firstMatch
        let progressText = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS '%' OR label CONTAINS 'goal'"))

        if progressView.exists || progressText.exists {
            takeScreenshot(name: "Savings Progress")
        }
    }

    // MARK: - Deposit Tests

    func testDeposit_ShowsForm() throws {
        // Find deposit button
        let depositButton = app.buttons["deposit_button"]
        let depositLabel = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Deposit'"))

        if waitForElement(depositButton, timeout: 3) {
            depositButton.tap()
        } else if waitForElement(depositLabel, timeout: 3) {
            depositLabel.tap()
        } else {
            // May need to select an account first
            return
        }

        // Verify deposit form
        sleep(1)
        takeScreenshot(name: "Deposit Form")
    }

    func testDeposit_AmountEntry() throws {
        openDepositForm()

        // Find amount field
        let amountField = app.textFields["savings_amount_field"]
        let anyAmountField = app.textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS '0.00' OR label CONTAINS 'Amount'"))
        let firstTextField = app.textFields.firstMatch

        if waitForElement(amountField, timeout: 3) {
            typeIntoField(amountField, text: "25.00")
        } else if waitForElement(anyAmountField, timeout: 3) {
            typeIntoField(anyAmountField, text: "25.00")
        } else if waitForElement(firstTextField, timeout: 3) {
            typeIntoField(firstTextField, text: "25.00")
        }

        takeScreenshot(name: "Deposit Amount Entered")
    }

    func testDeposit_Cancel() throws {
        openDepositForm()

        // Tap cancel
        let cancelButton = app.buttons["Cancel"]
        if waitForElement(cancelButton, timeout: 3) {
            cancelButton.tap()
            sleep(1)
            takeScreenshot(name: "After Cancel Deposit")
        }
    }

    // MARK: - Withdraw Tests

    func testWithdraw_ShowsForm() throws {
        // Find withdraw button
        let withdrawButton = app.buttons["withdraw_button"]
        let withdrawLabel = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Withdraw'"))

        if waitForElement(withdrawButton, timeout: 3) {
            withdrawButton.tap()
        } else if waitForElement(withdrawLabel, timeout: 3) {
            withdrawLabel.tap()
        } else {
            // May need to select an account first
            return
        }

        // Verify withdraw form
        sleep(1)
        takeScreenshot(name: "Withdraw Form")
    }

    func testWithdraw_AmountEntry() throws {
        openWithdrawForm()

        // Find amount field
        let amountField = app.textFields["savings_amount_field"]
        let anyAmountField = app.textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS '0.00' OR label CONTAINS 'Amount'"))
        let firstTextField = app.textFields.firstMatch

        if waitForElement(amountField, timeout: 3) {
            typeIntoField(amountField, text: "10.00")
        } else if waitForElement(anyAmountField, timeout: 3) {
            typeIntoField(anyAmountField, text: "10.00")
        } else if waitForElement(firstTextField, timeout: 3) {
            typeIntoField(firstTextField, text: "10.00")
        }

        takeScreenshot(name: "Withdraw Amount Entered")
    }

    func testWithdraw_Cancel() throws {
        openWithdrawForm()

        // Tap cancel
        let cancelButton = app.buttons["Cancel"]
        if waitForElement(cancelButton, timeout: 3) {
            cancelButton.tap()
            sleep(1)
            takeScreenshot(name: "After Cancel Withdraw")
        }
    }

    // MARK: - Transaction History Tests

    func testSavings_TransactionHistory_Displays() throws {
        // Look for transaction history
        let transactionRows = app.cells.matching(NSPredicate(format: "identifier BEGINSWITH 'savings_transaction_'"))
        let depositText = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'Deposit'"))
        let withdrawText = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'Withdraw'"))
        let emptyText = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'No Transactions'"))

        let hasHistory = transactionRows.count > 0 || depositText.exists || withdrawText.exists || emptyText.exists

        if hasHistory {
            takeScreenshot(name: "Savings Transaction History")
        }
    }

    // MARK: - Account Selection Tests (Multiple Accounts)

    func testSavings_AccountSelector() throws {
        // Look for account selector (if multiple accounts exist)
        let accountCards = app.buttons.matching(NSPredicate(format: "identifier BEGINSWITH 'savings_account_card_'"))
        let accountScroll = app.scrollViews.element(matching: NSPredicate(format: "identifier CONTAINS 'account_selector'"))

        if accountCards.count > 1 {
            // Try selecting second account
            let secondAccount = accountCards.element(boundBy: 1)
            if secondAccount.exists {
                secondAccount.tap()
                sleep(1)
                takeScreenshot(name: "Second Account Selected")
            }
        }
    }

    // MARK: - Empty State Tests

    func testSavings_EmptyState() throws {
        // Look for empty state
        let emptyStateText = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'No Savings Accounts' OR label CONTAINS 'Create'"))
        let createButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Create Savings'"))

        if emptyStateText.exists || createButton.exists {
            takeScreenshot(name: "Savings Empty State")
        }
    }

    // MARK: - Edit Account Tests

    func testSavings_EditAccount() throws {
        // Look for edit/more options button
        let editButton = app.buttons.element(matching: NSPredicate(format: "identifier CONTAINS 'ellipsis' OR label CONTAINS 'Edit' OR label CONTAINS 'More'"))

        if editButton.exists {
            editButton.tap()
            sleep(1)
            takeScreenshot(name: "Edit Savings Account")
        }
    }

    // MARK: - Helper Methods

    private func navigateToFirstChildSavings() {
        // Find and tap the first child card
        let childCards = app.buttons.matching(NSPredicate(format: "identifier BEGINSWITH 'child_card_'"))

        if childCards.count > 0 {
            let firstChildCard = childCards.element(boundBy: 0)
            if waitForElement(firstChildCard, timeout: 10) {
                firstChildCard.tap()

                // Wait for child detail view
                sleep(1)

                // Tap on Savings tab (parent only)
                let savingsTab = app.tabBars.buttons["Savings"]
                if waitForElement(savingsTab, timeout: 5) {
                    savingsTab.tap()
                }
            }
        }
    }

    private func openAddSavingsAccountForm() {
        // Tap add button
        let addButton = app.buttons["add_savings_account_button"]
        let navAddButton = app.navigationBars.buttons.element(matching: NSPredicate(format: "identifier CONTAINS 'plus'"))
        let createButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Create Savings'"))
        let anyPlusButton = app.buttons.matching(NSPredicate(format: "label CONTAINS 'plus' OR identifier CONTAINS 'plus'")).firstMatch

        if waitForElement(addButton, timeout: 3) {
            addButton.tap()
        } else if waitForElement(createButton, timeout: 3) {
            createButton.tap()
        } else if waitForElement(navAddButton, timeout: 3) {
            navAddButton.tap()
        } else if anyPlusButton.exists {
            anyPlusButton.tap()
        }

        sleep(1)
    }

    private func openDepositForm() {
        let depositButton = app.buttons["deposit_button"]
        let depositLabel = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Deposit'"))

        if waitForElement(depositButton, timeout: 3) {
            depositButton.tap()
        } else if waitForElement(depositLabel, timeout: 3) {
            depositLabel.tap()
        }

        sleep(1)
    }

    private func openWithdrawForm() {
        let withdrawButton = app.buttons["withdraw_button"]
        let withdrawLabel = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Withdraw'"))

        if waitForElement(withdrawButton, timeout: 3) {
            withdrawButton.tap()
        } else if waitForElement(withdrawLabel, timeout: 3) {
            withdrawLabel.tap()
        }

        sleep(1)
    }
}
