import XCTest
@testable import AllowanceTracker

final class ChildModelTests: XCTestCase {

    func testChildDecoding() throws {
        // Arrange
        let json = """
        {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "firstName": "Alice",
            "lastName": "Johnson",
            "weeklyAllowance": 10.50,
            "currentBalance": 25.75,
            "lastAllowanceDate": "2025-01-01T12:00:00Z"
        }
        """.data(using: .utf8)!

        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601

        // Act
        let child = try decoder.decode(Child.self, from: json)

        // Assert
        XCTAssertEqual(child.firstName, "Alice")
        XCTAssertEqual(child.lastName, "Johnson")
        XCTAssertEqual(child.weeklyAllowance, Decimal(string: "10.50")!)
        XCTAssertEqual(child.currentBalance, Decimal(string: "25.75")!)
        XCTAssertNotNil(child.lastAllowanceDate)
    }

    func testChildFullName() throws {
        // Arrange
        let child = Child(
            id: UUID(),
            firstName: "Bob",
            lastName: "Smith",
            weeklyAllowance: 15.00,
            currentBalance: 50.00,
            lastAllowanceDate: nil
        )

        // Act
        let fullName = child.fullName

        // Assert
        XCTAssertEqual(fullName, "Bob Smith")
    }

    func testChildFormattedBalance() throws {
        // Arrange
        let child = Child(
            id: UUID(),
            firstName: "Charlie",
            lastName: "Brown",
            weeklyAllowance: 10.00,
            currentBalance: 123.45,
            lastAllowanceDate: nil
        )

        // Act
        let formatted = child.formattedBalance

        // Assert
        XCTAssertEqual(formatted, "$123.45")
    }

    func testChildDecodingWithNullLastAllowanceDate() throws {
        // Arrange
        let json = """
        {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "firstName": "David",
            "lastName": "Wilson",
            "weeklyAllowance": 20.00,
            "currentBalance": 0.00,
            "lastAllowanceDate": null
        }
        """.data(using: .utf8)!

        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601

        // Act
        let child = try decoder.decode(Child.self, from: json)

        // Assert
        XCTAssertNil(child.lastAllowanceDate)
        XCTAssertEqual(child.currentBalance, 0.00)
    }
}
