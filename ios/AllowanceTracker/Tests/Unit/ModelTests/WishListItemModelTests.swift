import XCTest
@testable import AllowanceTracker

final class WishListItemModelTests: XCTestCase {

    // MARK: - Initialization Tests

    func testWishListItem_InitializesWithAllProperties() {
        // Arrange
        let id = UUID()
        let childId = UUID()
        let name = "Bicycle"
        let price: Decimal = 150.00
        let url = "https://example.com/bicycle"
        let notes = "Red mountain bike"
        let isPurchased = false
        let purchasedAt: Date? = nil
        let createdAt = Date()
        let canAfford = false

        // Act
        let item = WishListItem(
            id: id,
            childId: childId,
            name: name,
            price: price,
            url: url,
            notes: notes,
            isPurchased: isPurchased,
            purchasedAt: purchasedAt,
            createdAt: createdAt,
            canAfford: canAfford
        )

        // Assert
        XCTAssertEqual(item.id, id)
        XCTAssertEqual(item.childId, childId)
        XCTAssertEqual(item.name, name)
        XCTAssertEqual(item.price, price)
        XCTAssertEqual(item.url, url)
        XCTAssertEqual(item.notes, notes)
        XCTAssertEqual(item.isPurchased, isPurchased)
        XCTAssertNil(item.purchasedAt)
        XCTAssertEqual(item.createdAt, createdAt)
        XCTAssertEqual(item.canAfford, canAfford)
    }

    func testWishListItem_InitializesWithOptionalProperties() {
        // Arrange & Act
        let item = WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "Video Game",
            price: 60.00,
            url: nil,
            notes: nil,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: true
        )

        // Assert
        XCTAssertNil(item.url)
        XCTAssertNil(item.notes)
        XCTAssertNil(item.purchasedAt)
    }

    // MARK: - Purchased State Tests

    func testWishListItem_UnpurchasedState() {
        // Arrange & Act
        let item = WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "Test Item",
            price: 50.00,
            url: nil,
            notes: nil,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: true
        )

        // Assert
        XCTAssertFalse(item.isPurchased)
        XCTAssertNil(item.purchasedAt)
    }

    func testWishListItem_PurchasedState() {
        // Arrange
        let purchaseDate = Date()

        // Act
        let item = WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "Test Item",
            price: 50.00,
            url: nil,
            notes: nil,
            isPurchased: true,
            purchasedAt: purchaseDate,
            createdAt: Date(),
            canAfford: true
        )

        // Assert
        XCTAssertTrue(item.isPurchased)
        XCTAssertEqual(item.purchasedAt, purchaseDate)
    }

    // MARK: - CanAfford Tests

    func testWishListItem_CanAffordTrue() {
        // Arrange & Act
        let item = WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "Affordable Item",
            price: 10.00,
            url: nil,
            notes: nil,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: true
        )

        // Assert
        XCTAssertTrue(item.canAfford)
    }

    func testWishListItem_CanAffordFalse() {
        // Arrange & Act
        let item = WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "Expensive Item",
            price: 1000.00,
            url: nil,
            notes: nil,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: false
        )

        // Assert
        XCTAssertFalse(item.canAfford)
    }

    // MARK: - Codable Tests

    func testWishListItem_EncodesAndDecodes() throws {
        // Arrange
        let original = WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "Test Item",
            price: 75.00,
            url: "https://example.com/item",
            notes: "Test notes",
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: true
        )

        // Act
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        let data = try encoder.encode(original)

        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        let decoded = try decoder.decode(WishListItem.self, from: data)

        // Assert
        XCTAssertEqual(decoded.id, original.id)
        XCTAssertEqual(decoded.childId, original.childId)
        XCTAssertEqual(decoded.name, original.name)
        XCTAssertEqual(decoded.price, original.price)
        XCTAssertEqual(decoded.url, original.url)
        XCTAssertEqual(decoded.notes, original.notes)
        XCTAssertEqual(decoded.isPurchased, original.isPurchased)
        XCTAssertEqual(decoded.canAfford, original.canAfford)
    }

    func testWishListItem_EncodesAndDecodesWithNilValues() throws {
        // Arrange
        let original = WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "Minimal Item",
            price: 25.00,
            url: nil,
            notes: nil,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: false
        )

        // Act
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        let data = try encoder.encode(original)

        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        let decoded = try decoder.decode(WishListItem.self, from: data)

        // Assert
        XCTAssertNil(decoded.url)
        XCTAssertNil(decoded.notes)
        XCTAssertNil(decoded.purchasedAt)
    }

    // MARK: - Identifiable Tests

    func testWishListItem_ConformsToIdentifiable() {
        // Arrange
        let id = UUID()
        let item = WishListItem(
            id: id,
            childId: UUID(),
            name: "Test",
            price: 10.00,
            url: nil,
            notes: nil,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: true
        )

        // Assert
        XCTAssertEqual(item.id, id)
    }

    // MARK: - Edge Case Tests

    func testWishListItem_HandlesZeroPrice() {
        // Arrange & Act
        let item = WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "Free Item",
            price: 0.00,
            url: nil,
            notes: nil,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: true
        )

        // Assert
        XCTAssertEqual(item.price, 0.00)
    }

    func testWishListItem_HandlesLargePrice() {
        // Arrange
        let largePrice: Decimal = 999999.99

        // Act
        let item = WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "Very Expensive Item",
            price: largePrice,
            url: nil,
            notes: nil,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: false
        )

        // Assert
        XCTAssertEqual(item.price, largePrice)
    }

    func testWishListItem_HandlesEmptyName() {
        // Arrange & Act
        let item = WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "",
            price: 10.00,
            url: nil,
            notes: nil,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: true
        )

        // Assert
        XCTAssertEqual(item.name, "")
    }

    func testWishListItem_HandlesLongName() {
        // Arrange
        let longName = String(repeating: "a", count: 1000)

        // Act
        let item = WishListItem(
            id: UUID(),
            childId: UUID(),
            name: longName,
            price: 10.00,
            url: nil,
            notes: nil,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: true
        )

        // Assert
        XCTAssertEqual(item.name.count, 1000)
    }

    func testWishListItem_HandlesLongNotes() {
        // Arrange
        let longNotes = String(repeating: "note ", count: 200)

        // Act
        let item = WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "Item",
            price: 10.00,
            url: nil,
            notes: longNotes,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: true
        )

        // Assert
        XCTAssertEqual(item.notes, longNotes)
    }
}
