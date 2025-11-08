import XCTest
@testable import AllowanceTracker

@MainActor
final class WishListViewModelTests: XCTestCase {
    var sut: WishListViewModel!
    var mockAPIService: MockAPIService!
    let testChildId = UUID()

    override func setUp() {
        super.setUp()
        mockAPIService = MockAPIService()
        sut = WishListViewModel(childId: testChildId, apiService: mockAPIService)
    }

    override func tearDown() {
        sut = nil
        mockAPIService = nil
        super.tearDown()
    }

    // MARK: - Load Items Tests

    func testLoadItems_Success_PopulatesItemsArray() async {
        // Arrange
        let expectedItems = [
            WishListItem(
                id: UUID(),
                childId: testChildId,
                name: "Bicycle",
                price: 150.00,
                url: nil,
                notes: "Red one",
                isPurchased: false,
                purchasedAt: nil,
                createdAt: Date(),
                canAfford: false
            ),
            WishListItem(
                id: UUID(),
                childId: testChildId,
                name: "Video Game",
                price: 60.00,
                url: nil,
                notes: nil,
                isPurchased: false,
                purchasedAt: nil,
                createdAt: Date(),
                canAfford: true
            )
        ]
        mockAPIService.wishListResult = .success(expectedItems)

        // Act
        await sut.loadItems()

        // Assert
        XCTAssertEqual(sut.items.count, 2)
        XCTAssertEqual(sut.items[0].name, "Bicycle")
        XCTAssertEqual(sut.items[1].name, "Video Game")
        XCTAssertFalse(sut.isLoading)
        XCTAssertNil(sut.errorMessage)
    }

    func testLoadItems_Failure_SetsErrorMessage() async {
        // Arrange
        mockAPIService.wishListResult = .failure(APIError.unauthorized)

        // Act
        await sut.loadItems()

        // Assert
        XCTAssertTrue(sut.items.isEmpty)
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertFalse(sut.isLoading)
    }

    func testLoadItems_EmptyResponse_HandlesGracefully() async {
        // Arrange
        mockAPIService.wishListResult = .success([])

        // Act
        await sut.loadItems()

        // Assert
        XCTAssertTrue(sut.items.isEmpty)
        XCTAssertNil(sut.errorMessage)
        XCTAssertFalse(sut.isLoading)
    }

    // MARK: - Add Item Tests

    func testAddItem_Success_RefreshesItemList() async {
        // Arrange
        let initialItems: [WishListItem] = []
        let newItem = WishListItem(
            id: UUID(),
            childId: testChildId,
            name: "New Item",
            price: 50.00,
            url: nil,
            notes: nil,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: false
        )

        mockAPIService.wishListResult = .success(initialItems)
        await sut.loadItems()

        mockAPIService.addWishListItemResult = .success(newItem)
        mockAPIService.wishListResult = .success([newItem])

        // Act
        await sut.addItem(name: "New Item", price: 50.00, url: nil, notes: nil)

        // Assert
        XCTAssertEqual(sut.items.count, 1)
        XCTAssertEqual(sut.items[0].name, "New Item")
        XCTAssertNil(sut.errorMessage)
    }

    func testAddItem_Failure_SetsErrorMessage() async {
        // Arrange
        mockAPIService.addWishListItemResult = .failure(APIError.validationError("Invalid price"))

        // Act
        await sut.addItem(name: "Test", price: -10.00, url: nil, notes: nil)

        // Assert
        XCTAssertNotNil(sut.errorMessage)
    }

    // MARK: - Toggle Purchase Tests

    func testTogglePurchase_UnpurchasedItem_MarkesAsPurchased() async {
        // Arrange
        let item = WishListItem(
            id: UUID(),
            childId: testChildId,
            name: "Bicycle",
            price: 150.00,
            url: nil,
            notes: nil,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: true
        )
        let purchasedItem = WishListItem(
            id: item.id,
            childId: testChildId,
            name: "Bicycle",
            price: 150.00,
            url: nil,
            notes: nil,
            isPurchased: true,
            purchasedAt: Date(),
            createdAt: Date(),
            canAfford: true
        )

        mockAPIService.wishListResult = .success([item])
        await sut.loadItems()

        mockAPIService.togglePurchaseResult = .success(purchasedItem)
        mockAPIService.wishListResult = .success([purchasedItem])

        // Act
        await sut.togglePurchase(item)

        // Assert
        XCTAssertEqual(sut.items.count, 1)
        XCTAssertTrue(sut.items[0].isPurchased)
    }

    func testTogglePurchase_PurchasedItem_MarkesAsUnpurchased() async {
        // Arrange
        let purchasedItem = WishListItem(
            id: UUID(),
            childId: testChildId,
            name: "Bicycle",
            price: 150.00,
            url: nil,
            notes: nil,
            isPurchased: true,
            purchasedAt: Date(),
            createdAt: Date(),
            canAfford: true
        )
        let unpurchasedItem = WishListItem(
            id: purchasedItem.id,
            childId: testChildId,
            name: "Bicycle",
            price: 150.00,
            url: nil,
            notes: nil,
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: true
        )

        mockAPIService.wishListResult = .success([purchasedItem])
        await sut.loadItems()

        mockAPIService.togglePurchaseResult = .success(unpurchasedItem)
        mockAPIService.wishListResult = .success([unpurchasedItem])

        // Act
        await sut.togglePurchase(purchasedItem)

        // Assert
        XCTAssertEqual(sut.items.count, 1)
        XCTAssertFalse(sut.items[0].isPurchased)
    }

    // MARK: - Delete Items Tests

    func testDeleteItems_Success_RemovesItemsFromList() async {
        // Arrange
        let items = [
            WishListItem(id: UUID(), childId: testChildId, name: "Item 1", price: 10.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: false),
            WishListItem(id: UUID(), childId: testChildId, name: "Item 2", price: 20.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: false),
            WishListItem(id: UUID(), childId: testChildId, name: "Item 3", price: 30.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: false)
        ]

        mockAPIService.wishListResult = .success(items)
        await sut.loadItems()

        mockAPIService.deleteWishListItemResult = .success(())
        mockAPIService.wishListResult = .success([items[0], items[2]]) // Item at index 1 deleted

        // Act
        await sut.deleteItems(at: IndexSet(integer: 1))

        // Assert
        XCTAssertEqual(sut.items.count, 2)
        XCTAssertEqual(sut.items[0].name, "Item 1")
        XCTAssertEqual(sut.items[1].name, "Item 3")
    }

    // MARK: - Can Afford Tests

    func testLoadItems_SetsCanAffordCorrectly() async {
        // Arrange
        let items = [
            WishListItem(id: UUID(), childId: testChildId, name: "Affordable", price: 10.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: true),
            WishListItem(id: UUID(), childId: testChildId, name: "Too Expensive", price: 1000.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: false)
        ]
        mockAPIService.wishListResult = .success(items)

        // Act
        await sut.loadItems()

        // Assert
        XCTAssertTrue(sut.items[0].canAfford)
        XCTAssertFalse(sut.items[1].canAfford)
    }

    // MARK: - State Management Tests

    func testInitialState_IsCorrect() {
        // Assert
        XCTAssertTrue(sut.items.isEmpty)
        XCTAssertFalse(sut.isLoading)
        XCTAssertNil(sut.errorMessage)
        XCTAssertFalse(sut.showAddItem)
    }

    func testShowAddItem_TogglesState() {
        // Arrange
        XCTAssertFalse(sut.showAddItem)

        // Act
        sut.showAddItem = true

        // Assert
        XCTAssertTrue(sut.showAddItem)
    }
}

// MARK: - Mock API Service Extension for WishList

extension MockAPIService {
    var wishListResult: Result<[WishListItem], Error>? {
        get { nil }
        set {
            if let result = newValue {
                wishListResponse = result
            }
        }
    }

    var addWishListItemResult: Result<WishListItem, Error>? {
        get { nil }
        set {
            if let result = newValue {
                addWishListItemResponse = result
            }
        }
    }

    var togglePurchaseResult: Result<WishListItem, Error>? {
        get { nil }
        set {
            if let result = newValue {
                togglePurchaseResponse = result
            }
        }
    }

    var deleteWishListItemResult: Result<Void, Error>? {
        get { nil }
        set {
            if let result = newValue {
                deleteWishListItemResponse = result
            }
        }
    }
}
