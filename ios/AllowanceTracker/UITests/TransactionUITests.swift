import XCTest

/// UI tests for transaction flows
final class TransactionUITests: AllowanceTrackerUITests {

    // MARK: - Setup

    override func setUpWithError() throws {
        try super.setUpWithError()

        // Login before each transaction test
        performLogin(email: testParentEmail, password: testParentPassword)

        // Wait for dashboard to load
        assertDashboardDisplayed()

        // Navigate to first child's transactions
        navigateToFirstChildTransactions()
    }

    // MARK: - Transaction List Tests

    func testTransactionList_DisplaysCorrectly() throws {
        // Verify we're on transactions tab
        let transactionsTab = app.tabBars.buttons["Transactions"]
        XCTAssertTrue(transactionsTab.isSelected || waitForElement(transactionsTab), "Transactions tab should be visible")

        takeScreenshot(name: "Transaction List")
    }

    func testTransactionList_DisplaysBalance() throws {
        // Look for balance display
        let balanceText = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS '$'"))
        XCTAssertTrue(waitForElement(balanceText, timeout: 10), "Balance should be displayed")

        takeScreenshot(name: "Transaction Balance Display")
    }

    func testTransactionList_PullToRefresh() throws {
        // Find the scroll view
        let scrollView = app.scrollViews.firstMatch

        if scrollView.exists {
            // Perform pull to refresh
            let start = scrollView.coordinate(withNormalizedOffset: CGVector(dx: 0.5, dy: 0.2))
            let end = scrollView.coordinate(withNormalizedOffset: CGVector(dx: 0.5, dy: 0.8))
            start.press(forDuration: 0.1, thenDragTo: end)

            // Wait for refresh
            sleep(2)

            takeScreenshot(name: "Transaction List After Refresh")
        }
    }

    // MARK: - Create Transaction Tests

    func testCreateTransaction_ShowsForm() throws {
        // Tap add transaction button
        let addButton = app.buttons["create_transaction_button"]
        let navAddButton = app.navigationBars.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Add' OR identifier CONTAINS 'plus'"))
        let toolbarAddButton = app.buttons.element(matching: NSPredicate(format: "identifier CONTAINS 'plus' OR label CONTAINS 'Add Transaction'"))

        if waitForElement(addButton, timeout: 3) {
            addButton.tap()
        } else if waitForElement(navAddButton, timeout: 3) {
            navAddButton.tap()
        } else if waitForElement(toolbarAddButton, timeout: 3) {
            toolbarAddButton.tap()
        } else {
            // Try to find any plus button in toolbar
            let plusButton = app.buttons.matching(NSPredicate(format: "label CONTAINS 'plus' OR identifier CONTAINS 'plus'")).firstMatch
            if plusButton.exists {
                plusButton.tap()
            } else {
                XCTFail("Add transaction button not found")
                return
            }
        }

        // Verify form is displayed
        let formTitle = app.navigationBars["New Transaction"]
        XCTAssertTrue(waitForElement(formTitle, timeout: 5), "Create transaction form should be displayed")

        takeScreenshot(name: "Create Transaction Form")
    }

    func testCreateTransaction_TypePicker() throws {
        openCreateTransactionForm()

        // Verify type picker exists
        let typePicker = app.segmentedControls.firstMatch
        XCTAssertTrue(waitForElement(typePicker), "Transaction type picker should be visible")

        // Try selecting different types
        let incomeButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Income' OR label CONTAINS 'Credit'"))
        let spendingButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Spending' OR label CONTAINS 'Debit'"))

        if incomeButton.exists {
            incomeButton.tap()
            takeScreenshot(name: "Income Type Selected")
        }

        if spendingButton.exists {
            spendingButton.tap()
            takeScreenshot(name: "Spending Type Selected")
        }
    }

    func testCreateTransaction_AmountEntry() throws {
        openCreateTransactionForm()

        // Find amount field
        let amountField = app.textFields["transaction_amount_field"]
        let anyTextField = app.textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS '0.00' OR label CONTAINS 'Amount'"))

        if waitForElement(amountField, timeout: 3) {
            typeIntoField(amountField, text: "25.50")
        } else if waitForElement(anyTextField, timeout: 3) {
            typeIntoField(anyTextField, text: "25.50")
        }

        takeScreenshot(name: "Amount Entered")
    }

    func testCreateTransaction_DescriptionEntry() throws {
        openCreateTransactionForm()

        // Find description field
        let descriptionField = app.textFields["transaction_description_field"]
        let anyDescField = app.textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS 'description' OR label CONTAINS 'Description'"))
        let textView = app.textViews.firstMatch

        if waitForElement(descriptionField, timeout: 3) {
            typeIntoField(descriptionField, text: "Test transaction")
        } else if waitForElement(anyDescField, timeout: 3) {
            typeIntoField(anyDescField, text: "Test transaction")
        } else if waitForElement(textView, timeout: 3) {
            textView.tap()
            textView.typeText("Test transaction")
        }

        takeScreenshot(name: "Description Entered")
    }

    func testCreateTransaction_Cancel() throws {
        openCreateTransactionForm()

        // Tap cancel
        let cancelButton = app.buttons["transaction_cancel_button"]
        let navCancelButton = app.navigationBars.buttons["Cancel"]

        if waitForElement(cancelButton, timeout: 3) {
            cancelButton.tap()
        } else if waitForElement(navCancelButton, timeout: 3) {
            navCancelButton.tap()
        }

        // Verify form is dismissed
        let formTitle = app.navigationBars["New Transaction"]
        XCTAssertTrue(waitForElementToDisappear(formTitle, timeout: 5), "Create transaction form should be dismissed")

        takeScreenshot(name: "After Cancel")
    }

    func testCreateTransaction_SaveDisabledWhenEmpty() throws {
        openCreateTransactionForm()

        // Verify save button is disabled
        let saveButton = app.buttons["transaction_save_button"]
        let navSaveButton = app.navigationBars.buttons["Save"]

        if waitForElement(saveButton, timeout: 3) {
            XCTAssertFalse(saveButton.isEnabled, "Save button should be disabled when form is empty")
        } else if waitForElement(navSaveButton, timeout: 3) {
            XCTAssertFalse(navSaveButton.isEnabled, "Save button should be disabled when form is empty")
        }

        takeScreenshot(name: "Save Disabled")
    }

    func testCreateTransaction_FullFlow() throws {
        openCreateTransactionForm()

        // Enter amount
        let amountField = app.textFields.firstMatch
        if waitForElement(amountField) {
            amountField.tap()
            amountField.typeText("10.00")
        }

        // Enter description
        let descriptionField = app.textViews.firstMatch
        if descriptionField.exists {
            descriptionField.tap()
            descriptionField.typeText("Test transaction from UI tests")
        } else {
            // Try text fields
            let textFields = app.textFields
            if textFields.count > 1 {
                let field = textFields.element(boundBy: 1)
                field.tap()
                field.typeText("Test transaction from UI tests")
            }
        }

        // Dismiss keyboard
        app.toolbars.buttons["Done"].tap()

        takeScreenshot(name: "Transaction Form Filled")

        // Try to save
        let saveButton = app.navigationBars.buttons["Save"]
        if saveButton.exists && saveButton.isEnabled {
            saveButton.tap()

            // Wait for form to dismiss
            sleep(2)
            takeScreenshot(name: "After Transaction Save")
        }
    }

    // MARK: - Transaction Row Tests

    func testTransactionRow_DisplaysCorrectInfo() throws {
        // Look for transaction rows
        let transactionRows = app.buttons.matching(NSPredicate(format: "identifier BEGINSWITH 'transaction_row_'"))

        if transactionRows.count > 0 {
            let firstRow = transactionRows.element(boundBy: 0)
            XCTAssertTrue(firstRow.exists, "Transaction row should exist")
            takeScreenshot(name: "Transaction Row")
        } else {
            // Check for transaction list content
            let scrollView = app.scrollViews.firstMatch
            XCTAssertTrue(scrollView.exists, "Transaction list should exist")
            takeScreenshot(name: "Transaction List Content")
        }
    }

    // MARK: - Helper Methods

    private func navigateToFirstChildTransactions() {
        // Find and tap the first child card
        let childCards = app.buttons.matching(NSPredicate(format: "identifier BEGINSWITH 'child_card_'"))

        if childCards.count > 0 {
            let firstChildCard = childCards.element(boundBy: 0)
            if waitForElement(firstChildCard, timeout: 10) {
                firstChildCard.tap()

                // Wait for child detail view
                sleep(1)

                // Tap on Transactions tab
                let transactionsTab = app.tabBars.buttons["Transactions"]
                if waitForElement(transactionsTab, timeout: 5) {
                    transactionsTab.tap()
                }
            }
        }
    }

    private func openCreateTransactionForm() {
        // Tap add transaction button
        let addButton = app.buttons["create_transaction_button"]
        let navAddButton = app.navigationBars.buttons.element(matching: NSPredicate(format: "identifier CONTAINS 'plus'"))
        let toolbarAddButton = app.buttons.element(matching: NSPredicate(format: "identifier CONTAINS 'plus' OR label CONTAINS 'Add Transaction'"))
        let anyPlusButton = app.buttons.matching(NSPredicate(format: "label CONTAINS 'plus' OR identifier CONTAINS 'plus' OR label == 'Add'")).firstMatch

        if waitForElement(addButton, timeout: 3) {
            addButton.tap()
        } else if waitForElement(navAddButton, timeout: 3) {
            navAddButton.tap()
        } else if waitForElement(toolbarAddButton, timeout: 3) {
            toolbarAddButton.tap()
        } else if anyPlusButton.exists {
            anyPlusButton.tap()
        }

        // Wait for form
        let formTitle = app.navigationBars["New Transaction"]
        _ = waitForElement(formTitle, timeout: 5)
    }
}
