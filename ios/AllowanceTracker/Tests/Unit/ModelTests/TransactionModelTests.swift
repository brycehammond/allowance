import XCTest
@testable import AllowanceTracker

final class TransactionModelTests: XCTestCase {

    // MARK: - Initialization Tests

    func testTransaction_InitializesWithAllProperties() {
        // Arrange
        let id = UUID()
        let childId = UUID()
        let amount: Decimal = 25.50
        let type = TransactionType.credit
        let description = "Weekly allowance"
        let balanceAfter: Decimal = 75.50
        let createdAt = Date()
        let createdByName = "Parent"

        // Act
        let transaction = Transaction(
            id: id,
            childId: childId,
            amount: amount,
            type: type,
            description: description,
            balanceAfter: balanceAfter,
            createdAt: createdAt,
            createdByName: createdByName
        )

        // Assert
        XCTAssertEqual(transaction.id, id)
        XCTAssertEqual(transaction.childId, childId)
        XCTAssertEqual(transaction.amount, amount)
        XCTAssertEqual(transaction.type, type)
        XCTAssertEqual(transaction.description, description)
        XCTAssertEqual(transaction.balanceAfter, balanceAfter)
        XCTAssertEqual(transaction.createdAt, createdAt)
        XCTAssertEqual(transaction.createdByName, createdByName)
    }

    // MARK: - Computed Property Tests

    func testIsCredit_ReturnsTrueForCreditType() {
        // Arrange
        let transaction = Transaction(
            id: UUID(),
            childId: UUID(),
            amount: 10.00,
            type: .credit,
            description: "Test",
            balanceAfter: 10.00,
            createdAt: Date(),
            createdByName: "Parent"
        )

        // Assert
        XCTAssertTrue(transaction.isCredit)
    }

    func testIsCredit_ReturnsFalseForDebitType() {
        // Arrange
        let transaction = Transaction(
            id: UUID(),
            childId: UUID(),
            amount: 10.00,
            type: .debit,
            description: "Test",
            balanceAfter: 0.00,
            createdAt: Date(),
            createdByName: "Child"
        )

        // Assert
        XCTAssertFalse(transaction.isCredit)
    }

    func testFormattedAmount_AddsPositiveSignForCredit() {
        // Arrange
        let transaction = Transaction(
            id: UUID(),
            childId: UUID(),
            amount: 25.50,
            type: .credit,
            description: "Test",
            balanceAfter: 25.50,
            createdAt: Date(),
            createdByName: "Parent"
        )

        // Act
        let formatted = transaction.formattedAmount

        // Assert
        XCTAssertTrue(formatted.hasPrefix("+"))
        XCTAssertTrue(formatted.contains("25.50"))
    }

    func testFormattedAmount_AddsNegativeSignForDebit() {
        // Arrange
        let transaction = Transaction(
            id: UUID(),
            childId: UUID(),
            amount: 10.00,
            type: .debit,
            description: "Test",
            balanceAfter: 15.50,
            createdAt: Date(),
            createdByName: "Child"
        )

        // Act
        let formatted = transaction.formattedAmount

        // Assert
        XCTAssertTrue(formatted.hasPrefix("-"))
        XCTAssertTrue(formatted.contains("10.00"))
    }

    // MARK: - TransactionType Tests

    func testTransactionType_CreditRawValue() {
        // Assert
        XCTAssertEqual(TransactionType.credit.rawValue, "Credit")
    }

    func testTransactionType_DebitRawValue() {
        // Assert
        XCTAssertEqual(TransactionType.debit.rawValue, "Debit")
    }

    func testTransactionType_InitFromRawValue() {
        // Assert
        XCTAssertEqual(TransactionType(rawValue: "Credit"), .credit)
        XCTAssertEqual(TransactionType(rawValue: "Debit"), .debit)
        XCTAssertNil(TransactionType(rawValue: "Invalid"))
    }

    // MARK: - Codable Tests

    func testTransaction_EncodesAndDecodes() throws {
        // Arrange
        let original = Transaction(
            id: UUID(),
            childId: UUID(),
            amount: 50.00,
            type: .credit,
            description: "Test Transaction",
            balanceAfter: 100.00,
            createdAt: Date(),
            createdByName: "Parent"
        )

        // Act
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        let data = try encoder.encode(original)

        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        let decoded = try decoder.decode(Transaction.self, from: data)

        // Assert
        XCTAssertEqual(decoded.id, original.id)
        XCTAssertEqual(decoded.childId, original.childId)
        XCTAssertEqual(decoded.amount, original.amount)
        XCTAssertEqual(decoded.type, original.type)
        XCTAssertEqual(decoded.description, original.description)
        XCTAssertEqual(decoded.balanceAfter, original.balanceAfter)
        XCTAssertEqual(decoded.createdByName, original.createdByName)
    }

    // MARK: - Identifiable Tests

    func testTransaction_ConformsToIdentifiable() {
        // Arrange
        let id = UUID()
        let transaction = Transaction(
            id: id,
            childId: UUID(),
            amount: 10.00,
            type: .credit,
            description: "Test",
            balanceAfter: 10.00,
            createdAt: Date(),
            createdByName: "Parent"
        )

        // Assert
        XCTAssertEqual(transaction.id, id)
    }

    // MARK: - Edge Case Tests

    func testTransaction_HandlesZeroAmount() {
        // Arrange
        let transaction = Transaction(
            id: UUID(),
            childId: UUID(),
            amount: 0.00,
            type: .credit,
            description: "Zero amount",
            balanceAfter: 50.00,
            createdAt: Date(),
            createdByName: "Parent"
        )

        // Assert
        XCTAssertEqual(transaction.amount, 0.00)
        XCTAssertTrue(transaction.formattedAmount.contains("0.00"))
    }

    func testTransaction_HandlesLargeAmount() {
        // Arrange
        let largeAmount: Decimal = 999999.99
        let transaction = Transaction(
            id: UUID(),
            childId: UUID(),
            amount: largeAmount,
            type: .credit,
            description: "Large amount",
            balanceAfter: largeAmount,
            createdAt: Date(),
            createdByName: "Parent"
        )

        // Assert
        XCTAssertEqual(transaction.amount, largeAmount)
    }
}
