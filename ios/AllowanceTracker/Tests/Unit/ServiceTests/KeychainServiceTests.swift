import XCTest
@testable import AllowanceTracker

final class KeychainServiceTests: XCTestCase {

    var sut: KeychainService!

    override func setUp() {
        super.setUp()
        sut = KeychainService.shared
        // Clean up any existing tokens before each test
        try? sut.deleteToken()
    }

    override func tearDown() {
        // Clean up after each test
        try? sut.deleteToken()
        super.tearDown()
    }

    func testSaveToken_SavesTokenSuccessfully() throws {
        // Arrange
        let token = "test-jwt-token-12345"

        // Act
        try sut.saveToken(token)

        // Assert
        let retrieved = try sut.getToken()
        XCTAssertEqual(retrieved, token)
    }

    func testGetToken_ThrowsErrorWhenTokenNotFound() {
        // Arrange - no token saved

        // Act & Assert
        XCTAssertThrowsError(try sut.getToken()) { error in
            XCTAssertEqual(error as? KeychainError, .notFound)
        }
    }

    func testSaveToken_OverwritesExistingToken() throws {
        // Arrange
        let firstToken = "first-token"
        let secondToken = "second-token"

        // Act
        try sut.saveToken(firstToken)
        try sut.saveToken(secondToken)

        // Assert
        let retrieved = try sut.getToken()
        XCTAssertEqual(retrieved, secondToken)
        XCTAssertNotEqual(retrieved, firstToken)
    }

    func testDeleteToken_RemovesToken() throws {
        // Arrange
        let token = "token-to-delete"
        try sut.saveToken(token)

        // Act
        try sut.deleteToken()

        // Assert
        XCTAssertThrowsError(try sut.getToken()) { error in
            XCTAssertEqual(error as? KeychainError, .notFound)
        }
    }

    func testDeleteToken_DoesNotThrowWhenTokenNotFound() {
        // Arrange - no token saved

        // Act & Assert
        XCTAssertNoThrow(try sut.deleteToken())
    }

    func testSaveToken_WithEmptyString_SavesSuccessfully() throws {
        // Arrange
        let emptyToken = ""

        // Act
        try sut.saveToken(emptyToken)

        // Assert
        let retrieved = try sut.getToken()
        XCTAssertEqual(retrieved, emptyToken)
    }

    func testSaveToken_WithLongToken_SavesSuccessfully() throws {
        // Arrange
        let longToken = String(repeating: "a", count: 1000)

        // Act
        try sut.saveToken(longToken)

        // Assert
        let retrieved = try sut.getToken()
        XCTAssertEqual(retrieved, longToken)
    }

    func testSaveToken_WithSpecialCharacters_SavesSuccessfully() throws {
        // Arrange
        let specialToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"

        // Act
        try sut.saveToken(specialToken)

        // Assert
        let retrieved = try sut.getToken()
        XCTAssertEqual(retrieved, specialToken)
    }
}
