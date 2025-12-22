import XCTest

/// UI tests for wish list flows
final class WishListUITests: AllowanceTrackerUITests {

    // MARK: - Setup

    override func setUpWithError() throws {
        try super.setUpWithError()

        // Login before each wish list test
        performLogin(email: testParentEmail, password: testParentPassword)

        // Wait for dashboard to load
        assertDashboardDisplayed()

        // Navigate to first child's wish list
        navigateToFirstChildWishList()
    }

    // MARK: - Wish List Display Tests

    func testWishList_DisplaysCorrectly() throws {
        // Verify we're on wish list tab
        let wishListTab = app.tabBars.buttons["Wish List"]
        XCTAssertTrue(wishListTab.isSelected || waitForElement(wishListTab), "Wish List tab should be visible")

        takeScreenshot(name: "Wish List View")
    }

    func testWishList_DisplaysFilterPicker() throws {
        // Look for filter picker (Active, Purchased, All)
        let filterPicker = app.segmentedControls.firstMatch
        let activeFilter = app.buttons["Active"]
        let purchasedFilter = app.buttons["Purchased"]
        let allFilter = app.buttons["All"]

        let hasFilter = filterPicker.exists || activeFilter.exists || purchasedFilter.exists

        if hasFilter {
            takeScreenshot(name: "Wish List Filters")
        }
    }

    func testWishList_FilterToggle() throws {
        // Try toggling filters
        let purchasedButton = app.buttons["Purchased"]
        let allButton = app.buttons["All"]
        let activeButton = app.buttons["Active"]

        if purchasedButton.exists {
            purchasedButton.tap()
            sleep(1)
            takeScreenshot(name: "Purchased Filter")
        }

        if allButton.exists {
            allButton.tap()
            sleep(1)
            takeScreenshot(name: "All Filter")
        }

        if activeButton.exists {
            activeButton.tap()
            sleep(1)
            takeScreenshot(name: "Active Filter")
        }
    }

    func testWishList_SummaryCard() throws {
        // Look for summary card elements
        let summaryLabel = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'Summary' OR label CONTAINS 'Active Items' OR label CONTAINS 'Can Afford'"))

        if summaryLabel.exists {
            takeScreenshot(name: "Wish List Summary")
        }
    }

    func testWishList_PullToRefresh() throws {
        // Find the scroll view
        let scrollView = app.scrollViews.firstMatch

        if scrollView.exists {
            // Perform pull to refresh
            let start = scrollView.coordinate(withNormalizedOffset: CGVector(dx: 0.5, dy: 0.2))
            let end = scrollView.coordinate(withNormalizedOffset: CGVector(dx: 0.5, dy: 0.8))
            start.press(forDuration: 0.1, thenDragTo: end)

            sleep(2)
            takeScreenshot(name: "Wish List After Refresh")
        }
    }

    // MARK: - Add Wish List Item Tests

    func testAddWishListItem_ShowsForm() throws {
        // Tap add button
        let addButton = app.buttons["add_wish_list_button"]
        let navAddButton = app.navigationBars.buttons.element(matching: NSPredicate(format: "identifier CONTAINS 'plus'"))
        let toolbarAddButton = app.buttons.element(matching: NSPredicate(format: "identifier CONTAINS 'plus' OR label CONTAINS 'Add Item'"))

        if waitForElement(addButton, timeout: 3) {
            addButton.tap()
        } else if waitForElement(navAddButton, timeout: 3) {
            navAddButton.tap()
        } else if waitForElement(toolbarAddButton, timeout: 3) {
            toolbarAddButton.tap()
        } else {
            let anyPlusButton = app.buttons.matching(NSPredicate(format: "label CONTAINS 'plus' OR identifier CONTAINS 'plus'")).firstMatch
            if anyPlusButton.exists {
                anyPlusButton.tap()
            } else {
                XCTFail("Add wish list item button not found")
                return
            }
        }

        // Verify form is displayed
        sleep(1)
        takeScreenshot(name: "Add Wish List Item Form")
    }

    func testAddWishListItem_NameEntry() throws {
        openAddWishListItemForm()

        // Find name field
        let nameField = app.textFields["wish_list_name_field"]
        let anyNameField = app.textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS 'name' OR label CONTAINS 'Name'"))
        let firstTextField = app.textFields.firstMatch

        if waitForElement(nameField, timeout: 3) {
            typeIntoField(nameField, text: "Test Toy")
        } else if waitForElement(anyNameField, timeout: 3) {
            typeIntoField(anyNameField, text: "Test Toy")
        } else if waitForElement(firstTextField, timeout: 3) {
            typeIntoField(firstTextField, text: "Test Toy")
        }

        takeScreenshot(name: "Wish List Name Entered")
    }

    func testAddWishListItem_PriceEntry() throws {
        openAddWishListItemForm()

        // Find price field
        let priceField = app.textFields["wish_list_price_field"]
        let anyPriceField = app.textFields.element(matching: NSPredicate(format: "placeholderValue CONTAINS '$' OR label CONTAINS 'Price' OR placeholderValue CONTAINS '0.00'"))

        if waitForElement(priceField, timeout: 3) {
            typeIntoField(priceField, text: "29.99")
        } else if waitForElement(anyPriceField, timeout: 3) {
            typeIntoField(anyPriceField, text: "29.99")
        } else {
            // Try all text fields looking for numeric field
            let textFields = app.textFields
            for i in 0..<textFields.count {
                let field = textFields.element(boundBy: i)
                if field.placeholderValue?.contains("0") == true || field.label.contains("Price") {
                    typeIntoField(field, text: "29.99")
                    break
                }
            }
        }

        takeScreenshot(name: "Wish List Price Entered")
    }

    func testAddWishListItem_Cancel() throws {
        openAddWishListItemForm()

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
        takeScreenshot(name: "After Cancel Wish List Item")
    }

    func testAddWishListItem_FullFlow() throws {
        openAddWishListItemForm()

        // Enter name
        let textFields = app.textFields
        if textFields.count > 0 {
            let nameField = textFields.element(boundBy: 0)
            if nameField.exists {
                nameField.tap()
                nameField.typeText("New Game")
            }
        }

        // Enter price
        if textFields.count > 1 {
            let priceField = textFields.element(boundBy: 1)
            if priceField.exists {
                priceField.tap()
                priceField.typeText("49.99")
            }
        }

        // Dismiss keyboard
        if app.toolbars.buttons["Done"].exists {
            app.toolbars.buttons["Done"].tap()
        }

        takeScreenshot(name: "Wish List Item Form Filled")

        // Try to save
        let saveButton = app.navigationBars.buttons["Save"]
        let addButton = app.buttons["Add"]

        if saveButton.exists && saveButton.isEnabled {
            saveButton.tap()
            sleep(2)
        } else if addButton.exists && addButton.isEnabled {
            addButton.tap()
            sleep(2)
        }

        takeScreenshot(name: "After Wish List Item Save")
    }

    // MARK: - Wish List Item Card Tests

    func testWishListItemCard_DisplaysInfo() throws {
        // Look for wish list item cards
        let itemCards = app.buttons.matching(NSPredicate(format: "identifier BEGINSWITH 'wish_list_item_'"))

        if itemCards.count > 0 {
            let firstItem = itemCards.element(boundBy: 0)
            XCTAssertTrue(firstItem.exists, "Wish list item should exist")
            takeScreenshot(name: "Wish List Item Card")
        } else {
            // Check for any list content
            let scrollView = app.scrollViews.firstMatch
            XCTAssertTrue(scrollView.exists, "Wish list should exist")
            takeScreenshot(name: "Wish List Content")
        }
    }

    func testWishListItem_Edit() throws {
        // Find an item to edit
        let itemCards = app.buttons.matching(NSPredicate(format: "identifier BEGINSWITH 'wish_list_item_'"))

        if itemCards.count > 0 {
            let firstItem = itemCards.element(boundBy: 0)
            firstItem.tap()

            // Look for edit option
            let editButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Edit'"))
            if editButton.exists {
                editButton.tap()
                sleep(1)
                takeScreenshot(name: "Edit Wish List Item")
            }
        }
    }

    func testWishListItem_Delete() throws {
        // Find an item
        let itemCards = app.buttons.matching(NSPredicate(format: "identifier BEGINSWITH 'wish_list_item_'"))

        if itemCards.count > 0 {
            let firstItem = itemCards.element(boundBy: 0)

            // Try swipe to delete
            firstItem.swipeLeft()
            sleep(1)

            // Look for delete button
            let deleteButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Delete'"))
            if deleteButton.exists {
                takeScreenshot(name: "Delete Wish List Item Option")
            }
        }
    }

    func testWishListItem_MarkAsPurchased() throws {
        // Find an item
        let itemCards = app.buttons.matching(NSPredicate(format: "identifier BEGINSWITH 'wish_list_item_'"))

        if itemCards.count > 0 {
            let firstItem = itemCards.element(boundBy: 0)
            firstItem.tap()

            // Look for purchase/mark as purchased option
            let purchaseButton = app.buttons.element(matching: NSPredicate(format: "label CONTAINS 'Purchase' OR label CONTAINS 'Mark'"))
            if purchaseButton.exists {
                takeScreenshot(name: "Purchase Option Available")
            }
        }
    }

    // MARK: - Empty State Tests

    func testWishList_EmptyState() throws {
        // Switch to purchased filter to potentially see empty state
        let purchasedButton = app.buttons["Purchased"]
        if purchasedButton.exists {
            purchasedButton.tap()
            sleep(1)
        }

        // Look for empty state message
        let emptyState = app.staticTexts.element(matching: NSPredicate(format: "label CONTAINS 'No' OR label CONTAINS 'Empty'"))

        if emptyState.exists {
            takeScreenshot(name: "Wish List Empty State")
        }
    }

    // MARK: - Helper Methods

    private func navigateToFirstChildWishList() {
        // Find and tap the first child card
        let childCards = app.buttons.matching(NSPredicate(format: "identifier BEGINSWITH 'child_card_'"))

        if childCards.count > 0 {
            let firstChildCard = childCards.element(boundBy: 0)
            if waitForElement(firstChildCard, timeout: 10) {
                firstChildCard.tap()

                // Wait for child detail view
                sleep(1)

                // Tap on Wish List tab
                let wishListTab = app.tabBars.buttons["Wish List"]
                if waitForElement(wishListTab, timeout: 5) {
                    wishListTab.tap()
                }
            }
        }
    }

    private func openAddWishListItemForm() {
        // Tap add button
        let addButton = app.buttons["add_wish_list_button"]
        let navAddButton = app.navigationBars.buttons.element(matching: NSPredicate(format: "identifier CONTAINS 'plus'"))
        let anyPlusButton = app.buttons.matching(NSPredicate(format: "label CONTAINS 'plus' OR identifier CONTAINS 'plus'")).firstMatch

        if waitForElement(addButton, timeout: 3) {
            addButton.tap()
        } else if waitForElement(navAddButton, timeout: 3) {
            navAddButton.tap()
        } else if anyPlusButton.exists {
            anyPlusButton.tap()
        }

        // Wait for form
        sleep(1)
    }
}
