import XCTest
@testable import AllowanceTracker

final class UserModelTests: XCTestCase {

    // MARK: - User Model Tests

    func testUserDecoding() throws {
        // Arrange
        let json = """
        {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "email": "test@example.com",
            "firstName": "John",
            "lastName": "Doe",
            "role": "Parent",
            "familyId": "223e4567-e89b-12d3-a456-426614174000"
        }
        """.data(using: .utf8)!

        // Act
        let user = try JSONDecoder().decode(User.self, from: json)

        // Assert
        XCTAssertEqual(user.email, "test@example.com")
        XCTAssertEqual(user.firstName, "John")
        XCTAssertEqual(user.lastName, "Doe")
        XCTAssertEqual(user.role, .parent)
        XCTAssertNotNil(user.familyId)
    }

    func testUserFullName() throws {
        // Arrange
        let user = User(
            id: UUID(),
            email: "test@example.com",
            firstName: "John",
            lastName: "Doe",
            role: .parent,
            familyId: UUID()
        )

        // Act
        let fullName = user.fullName

        // Assert
        XCTAssertEqual(fullName, "John Doe")
    }

    func testUserRoleDecoding() throws {
        // Arrange
        let parentJson = """
        {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "email": "parent@example.com",
            "firstName": "Jane",
            "lastName": "Parent",
            "role": "Parent",
            "familyId": "223e4567-e89b-12d3-a456-426614174000"
        }
        """.data(using: .utf8)!

        let childJson = """
        {
            "id": "123e4567-e89b-12d3-a456-426614174001",
            "email": "child@example.com",
            "firstName": "Tommy",
            "lastName": "Child",
            "role": "Child",
            "familyId": "223e4567-e89b-12d3-a456-426614174000"
        }
        """.data(using: .utf8)!

        // Act
        let parent = try JSONDecoder().decode(User.self, from: parentJson)
        let child = try JSONDecoder().decode(User.self, from: childJson)

        // Assert
        XCTAssertEqual(parent.role, .parent)
        XCTAssertEqual(child.role, .child)
    }

    // MARK: - AuthResponse Tests

    func testAuthResponseDecoding() throws {
        // Arrange
        let json = """
        {
            "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9",
            "expiresAt": "2025-12-31T23:59:59Z",
            "user": {
                "id": "123e4567-e89b-12d3-a456-426614174000",
                "email": "test@example.com",
                "firstName": "John",
                "lastName": "Doe",
                "role": "Parent",
                "familyId": "223e4567-e89b-12d3-a456-426614174000"
            }
        }
        """.data(using: .utf8)!

        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601

        // Act
        let response = try decoder.decode(AuthResponse.self, from: json)

        // Assert
        XCTAssertEqual(response.token, "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9")
        XCTAssertEqual(response.user.email, "test@example.com")
        XCTAssertNotNil(response.expiresAt)
    }

    // MARK: - LoginRequest Tests

    func testLoginRequestEncoding() throws {
        // Arrange
        let request = LoginRequest(
            email: "test@example.com",
            password: "Password123!"
        )

        // Act
        let data = try JSONEncoder().encode(request)
        let json = try JSONSerialization.jsonObject(with: data) as! [String: Any]

        // Assert
        XCTAssertEqual(json["email"] as? String, "test@example.com")
        XCTAssertEqual(json["password"] as? String, "Password123!")
    }

    // MARK: - RegisterRequest Tests

    func testRegisterRequestEncoding() throws {
        // Arrange
        let request = RegisterRequest(
            email: "new@example.com",
            password: "Password123!",
            firstName: "New",
            lastName: "User",
            role: .parent
        )

        // Act
        let data = try JSONEncoder().encode(request)
        let json = try JSONSerialization.jsonObject(with: data) as! [String: Any]

        // Assert
        XCTAssertEqual(json["email"] as? String, "new@example.com")
        XCTAssertEqual(json["password"] as? String, "Password123!")
        XCTAssertEqual(json["firstName"] as? String, "New")
        XCTAssertEqual(json["lastName"] as? String, "User")
        XCTAssertEqual(json["role"] as? String, "Parent")
    }
}
