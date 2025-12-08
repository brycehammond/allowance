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

    func testLoadWishList_Success_PopulatesItemsArray() async {
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
        mockAPIService.wishListResponse = .success(expectedItems)

        // Act
        await sut.loadWishList()

        // Assert
        XCTAssertEqual(sut.items.count, 2)
        XCTAssertEqual(sut.items[0].name, "Bicycle")
        XCTAssertEqual(sut.items[1].name, "Video Game")
        XCTAssertFalse(sut.isLoading)
        XCTAssertNil(sut.errorMessage)
    }

    func testLoadWishList_Failure_SetsErrorMessage() async {
        // Arrange
        mockAPIService.wishListResponse = .failure(APIError.unauthorized)

        // Act
        await sut.loadWishList()

        // Assert
        XCTAssertTrue(sut.items.isEmpty)
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertFalse(sut.isLoading)
    }

    func testLoadWishList_EmptyResponse_HandlesGracefully() async {
        // Arrange
        mockAPIService.wishListResponse = .success([])

        // Act
        await sut.loadWishList()

        // Assert
        XCTAssertTrue(sut.items.isEmpty)
        XCTAssertNil(sut.errorMessage)
        XCTAssertFalse(sut.isLoading)
    }

    // MARK: - Create Item Tests

    func testCreateItem_Success_AddsToItemList() async {
        // Arrange
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

        mockAPIService.createWishListItemResponse = .success(newItem)

        // Act
        let result = await sut.createItem(name: "New Item", price: 50.00, url: nil, notes: nil)

        // Assert
        XCTAssertTrue(result)
        XCTAssertEqual(sut.items.count, 1)
        XCTAssertEqual(sut.items[0].name, "New Item")
        XCTAssertNil(sut.errorMessage)
    }

    func testCreateItem_InvalidPrice_SetsErrorMessage() async {
        // Act
        let result = await sut.createItem(name: "Test", price: -10.00, url: nil, notes: nil)

        // Assert
        XCTAssertFalse(result)
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertEqual(sut.errorMessage, "Price must be greater than zero.")
    }

    func testCreateItem_EmptyName_SetsErrorMessage() async {
        // Act
        let result = await sut.createItem(name: "", price: 10.00, url: nil, notes: nil)

        // Assert
        XCTAssertFalse(result)
        XCTAssertNotNil(sut.errorMessage)
        XCTAssertEqual(sut.errorMessage, "Please enter an item name.")
    }

    // MARK: - Mark As Purchased Tests

    func testMarkAsPurchased_Success_UpdatesItem() async {
        // Arrange
        let itemId = UUID()
        let item = WishListItem(
            id: itemId,
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
            id: itemId,
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

        mockAPIService.wishListResponse = .success([item])
        await sut.loadWishList()

        mockAPIService.markPurchasedResponse = .success(purchasedItem)

        // Act
        let result = await sut.markAsPurchased(id: itemId)

        // Assert
        XCTAssertTrue(result)
        XCTAssertEqual(sut.items.count, 1)
        XCTAssertTrue(sut.items[0].isPurchased)
    }

    func testMarkAsPurchased_Failure_SetsErrorMessage() async {
        // Arrange
        mockAPIService.markPurchasedResponse = .failure(APIError.notFound)

        // Act
        let result = await sut.markAsPurchased(id: UUID())

        // Assert
        XCTAssertFalse(result)
        XCTAssertNotNil(sut.errorMessage)
    }

    // MARK: - Delete Item Tests

    func testDeleteItem_Success_RemovesFromList() async {
        // Arrange
        let itemId = UUID()
        let items = [
            WishListItem(id: UUID(), childId: testChildId, name: "Item 1", price: 10.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: false),
            WishListItem(id: itemId, childId: testChildId, name: "Item 2", price: 20.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: false),
            WishListItem(id: UUID(), childId: testChildId, name: "Item 3", price: 30.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: false)
        ]

        mockAPIService.wishListResponse = .success(items)
        await sut.loadWishList()

        mockAPIService.deleteWishListItemResponse = .success(())

        // Act
        let result = await sut.deleteItem(id: itemId)

        // Assert
        XCTAssertTrue(result)
        XCTAssertEqual(sut.items.count, 2)
        XCTAssertEqual(sut.items[0].name, "Item 1")
        XCTAssertEqual(sut.items[1].name, "Item 3")
    }

    func testDeleteItem_Failure_SetsErrorMessage() async {
        // Arrange
        mockAPIService.deleteWishListItemResponse = .failure(APIError.notFound)

        // Act
        let result = await sut.deleteItem(id: UUID())

        // Assert
        XCTAssertFalse(result)
        XCTAssertNotNil(sut.errorMessage)
    }

    // MARK: - Can Afford Tests

    func testLoadWishList_SetsCanAffordCorrectly() async {
        // Arrange
        let items = [
            WishListItem(id: UUID(), childId: testChildId, name: "Affordable", price: 10.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: true),
            WishListItem(id: UUID(), childId: testChildId, name: "Too Expensive", price: 1000.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: false)
        ]
        mockAPIService.wishListResponse = .success(items)

        // Act
        await sut.loadWishList()

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
        XCTAssertFalse(sut.isProcessing)
    }

    func testClearError_RemovesErrorMessage() async {
        // Arrange
        mockAPIService.wishListResponse = .failure(APIError.networkError)
        await sut.loadWishList()
        XCTAssertNotNil(sut.errorMessage)

        // Act
        sut.clearError()

        // Assert
        XCTAssertNil(sut.errorMessage)
    }

    // MARK: - Computed Properties Tests

    func testAffordableItems_ReturnsOnlyAffordableItems() async {
        // Arrange
        let items = [
            WishListItem(id: UUID(), childId: testChildId, name: "Affordable 1", price: 10.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: true),
            WishListItem(id: UUID(), childId: testChildId, name: "Expensive", price: 1000.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: false),
            WishListItem(id: UUID(), childId: testChildId, name: "Affordable 2", price: 20.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: true)
        ]
        mockAPIService.wishListResponse = .success(items)
        await sut.loadWishList()

        // Assert
        XCTAssertEqual(sut.affordableItems.count, 2)
        XCTAssertTrue(sut.affordableItems.allSatisfy { $0.canAfford })
    }

    func testPurchasedItems_ReturnsOnlyPurchasedItems() async {
        // Arrange
        let items = [
            WishListItem(id: UUID(), childId: testChildId, name: "Purchased", price: 10.00, url: nil, notes: nil, isPurchased: true, purchasedAt: Date(), createdAt: Date(), canAfford: true),
            WishListItem(id: UUID(), childId: testChildId, name: "Not Purchased", price: 20.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: true)
        ]
        mockAPIService.wishListResponse = .success(items)
        await sut.loadWishList()

        // Assert
        XCTAssertEqual(sut.purchasedItems.count, 1)
        XCTAssertEqual(sut.purchasedItems[0].name, "Purchased")
    }

    func testActiveItems_ReturnsOnlyUnpurchasedItems() async {
        // Arrange
        let items = [
            WishListItem(id: UUID(), childId: testChildId, name: "Purchased", price: 10.00, url: nil, notes: nil, isPurchased: true, purchasedAt: Date(), createdAt: Date(), canAfford: true),
            WishListItem(id: UUID(), childId: testChildId, name: "Active", price: 20.00, url: nil, notes: nil, isPurchased: false, purchasedAt: nil, createdAt: Date(), canAfford: true)
        ]
        mockAPIService.wishListResponse = .success(items)
        await sut.loadWishList()

        // Assert
        XCTAssertEqual(sut.activeItems.count, 1)
        XCTAssertEqual(sut.activeItems[0].name, "Active")
    }
}
