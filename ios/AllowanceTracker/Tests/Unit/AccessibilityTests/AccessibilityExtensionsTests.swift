import XCTest
import SwiftUI
@testable import AllowanceTracker

final class AccessibilityExtensionsTests: XCTestCase {

    // MARK: - Currency Accessibility Tests

    func testAccessibilityCurrencyLabel_WithWholeDollars() {
        // Arrange
        let amount: Decimal = 25.00

        // Act
        let label = amount.accessibilityCurrencyLabel

        // Assert
        XCTAssertEqual(label, "25 dollars")
    }

    func testAccessibilityCurrencyLabel_WithCents() {
        // Arrange
        let amount: Decimal = 25.50

        // Act
        let label = amount.accessibilityCurrencyLabel

        // Assert
        XCTAssertEqual(label, "25 dollars and 50 cents")
    }

    func testAccessibilityCurrencyLabel_WithOneDollar() {
        // Arrange
        let amount: Decimal = 1.00

        // Act
        let label = amount.accessibilityCurrencyLabel

        // Assert
        XCTAssertEqual(label, "1 dollar")
    }

    func testAccessibilityCurrencyLabel_WithOneCent() {
        // Arrange
        let amount: Decimal = 0.01

        // Act
        let label = amount.accessibilityCurrencyLabel

        // Assert
        XCTAssertEqual(label, "0 dollars and 1 cent")
    }

    func testAccessibilityCurrencyLabel_WithZero() {
        // Arrange
        let amount: Decimal = 0.00

        // Act
        let label = amount.accessibilityCurrencyLabel

        // Assert
        XCTAssertEqual(label, "0 dollars")
    }

    func testAccessibilityCurrencyLabel_WithLargeAmount() {
        // Arrange
        let amount: Decimal = 1234.56

        // Act
        let label = amount.accessibilityCurrencyLabel

        // Assert
        XCTAssertEqual(label, "1234 dollars and 56 cents")
    }

    // MARK: - Transaction Type Accessibility Tests

    func testTransactionType_CreditAccessibilityLabel() {
        // Arrange
        let type = TransactionType.credit

        // Act
        let label = type.accessibilityLabel

        // Assert
        XCTAssertEqual(label, "Money added")
    }

    func testTransactionType_DebitAccessibilityLabel() {
        // Arrange
        let type = TransactionType.debit

        // Act
        let label = type.accessibilityLabel

        // Assert
        XCTAssertEqual(label, "Money spent")
    }

    // MARK: - Date Accessibility Tests

    func testDateAccessibilityLabel_Today() {
        // Arrange
        let date = Date()

        // Act
        let label = date.accessibilityLabel

        // Assert
        XCTAssertTrue(label.hasPrefix("Today at"))
    }

    func testDateAccessibilityLabel_Yesterday() {
        // Arrange
        let calendar = Calendar.current
        let date = calendar.date(byAdding: .day, value: -1, to: Date())!

        // Act
        let label = date.accessibilityLabel

        // Assert
        XCTAssertTrue(label.hasPrefix("Yesterday at"))
    }

    func testDateAccessibilityLabel_ThisWeek() {
        // Arrange
        let calendar = Calendar.current
        let date = calendar.date(byAdding: .day, value: -3, to: Date())!

        // Act
        let label = date.accessibilityLabel

        // Assert
        // Should include day of week
        XCTAssertFalse(label.hasPrefix("Today"))
        XCTAssertFalse(label.hasPrefix("Yesterday"))
        XCTAssertTrue(label.contains(" at "))
    }

    func testDateAccessibilityLabel_OlderDate() {
        // Arrange
        let calendar = Calendar.current
        let date = calendar.date(byAdding: .month, value: -2, to: Date())!

        // Act
        let label = date.accessibilityLabel

        // Assert
        // Should be a formatted date
        XCTAssertFalse(label.contains("Today"))
        XCTAssertFalse(label.contains("Yesterday"))
    }

    // MARK: - Color Contrast Tests
    // Note: These tests use explicit RGB colors instead of system colors because
    // SwiftUI system colors (Color.green, etc.) can't resolve RGB components
    // in headless CI test environments without a UI context.

    func testColor_BlackHasContrastWithWhite() {
        // Arrange - Use explicit RGB black instead of Color.black
        let black = Color(red: 0, green: 0, blue: 0)

        // Assert
        XCTAssertTrue(black.hasContrastWithWhite)
    }

    func testColor_WhiteHasContrastWithBlack() {
        // Arrange - Use explicit RGB white instead of Color.white
        let white = Color(red: 1, green: 1, blue: 1)

        // Assert
        XCTAssertTrue(white.hasContrastWithBlack)
    }

    func testColor_GreenHasContrastWithWhite() {
        // Arrange - Use a dark green that has sufficient contrast with white
        let green = Color(red: 0, green: 0.5, blue: 0)

        // Assert
        XCTAssertTrue(green.hasContrastWithWhite)
    }

    func testColor_RedHasContrastWithWhite() {
        // Arrange - Use a dark red that has sufficient contrast with white
        let red = Color(red: 0.7, green: 0, blue: 0)

        // Assert
        XCTAssertTrue(red.hasContrastWithWhite)
    }

    func testColor_BlueHasContrastWithWhite() {
        // Arrange - Use a dark blue that has sufficient contrast with white
        let blue = Color(red: 0, green: 0, blue: 0.7)

        // Assert
        XCTAssertTrue(blue.hasContrastWithWhite)
    }

    // MARK: - Accessibility Identifier Tests

    func testAccessibilityIdentifiers_AreUnique() {
        // Arrange
        let identifiers = [
            AccessibilityIdentifier.loginEmailField,
            AccessibilityIdentifier.loginPasswordField,
            AccessibilityIdentifier.loginButton,
            AccessibilityIdentifier.registerButton,
            AccessibilityIdentifier.addChildButton,
            AccessibilityIdentifier.createTransactionButton,
            AccessibilityIdentifier.addWishListButton,
            AccessibilityIdentifier.logoutButton
        ]

        // Act
        let uniqueIdentifiers = Set(identifiers)

        // Assert
        XCTAssertEqual(identifiers.count, uniqueIdentifiers.count, "All accessibility identifiers should be unique")
    }

    func testAccessibilityIdentifiers_FollowNamingConvention() {
        // Assert - Check that identifiers use snake_case
        XCTAssertTrue(AccessibilityIdentifier.loginEmailField.contains("_"))
        XCTAssertTrue(AccessibilityIdentifier.loginPasswordField.contains("_"))
        XCTAssertTrue(AccessibilityIdentifier.loginButton.contains("_"))
        XCTAssertFalse(AccessibilityIdentifier.loginEmailField.contains(" "))
    }

    // MARK: - Edge Cases

    func testAccessibilityCurrencyLabel_WithNegativeAmount() {
        // Arrange
        let amount: Decimal = -10.50

        // Act
        let label = amount.accessibilityCurrencyLabel

        // Assert
        // Should handle negative amounts gracefully
        XCTAssertTrue(label.contains("dollar"))
    }

    func testAccessibilityCurrencyLabel_WithFractionalCent() {
        // Arrange
        let amount: Decimal = 10.555

        // Act
        let label = amount.accessibilityCurrencyLabel

        // Assert
        // Should round cents properly
        XCTAssertTrue(label.contains("dollar"))
        XCTAssertTrue(label.contains("cent"))
    }
}
