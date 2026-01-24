import Foundation
import SwiftUI

/// ViewModel for wish list management
@Observable
@MainActor
final class WishListViewModel {

    // MARK: - Observable Properties

    private(set) var items: [WishListItem] = []
    private(set) var currentBalance: Decimal = 0
    private(set) var isLoading = false
    private(set) var isProcessing = false
    var errorMessage: String?

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol
    private let childId: UUID

    // MARK: - Initialization

    init(childId: UUID, apiService: APIServiceProtocol = ServiceProvider.apiService) {
        self.childId = childId
        self.apiService = apiService
    }

    // MARK: - Public Methods

    /// Load wish list items for the child
    func loadWishList() async {
        // Clear previous errors
        errorMessage = nil

        // Set loading state
        isLoading = true
        defer { isLoading = false }

        do {
            items = try await apiService.getWishList(forChild: childId)

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load wish list. Please try again."
        }
    }

    /// Load current balance for affordability calculations
    func loadBalance() async {
        do {
            currentBalance = try await apiService.getBalance(forChild: childId)

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load balance. Please try again."
        }
    }

    /// Create a new wish list item
    /// - Parameters:
    ///   - name: Item name
    ///   - price: Item price
    ///   - url: Optional URL to purchase item
    ///   - notes: Optional notes about the item
    func createItem(
        name: String,
        price: Decimal,
        url: String?,
        notes: String?
    ) async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Validate inputs
        guard !name.isEmpty else {
            errorMessage = "Please enter an item name."
            return false
        }

        guard price > 0 else {
            errorMessage = "Price must be greater than zero."
            return false
        }

        // Set processing state
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = CreateWishListItemRequest(
                childId: childId,
                name: name,
                price: price,
                url: url,
                notes: notes
            )

            let newItem = try await apiService.createWishListItem(request)

            // Update local state
            items.append(newItem)

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to create wish list item. Please try again."
            return false
        }
    }

    /// Update an existing wish list item
    /// - Parameters:
    ///   - id: Item identifier
    ///   - name: Updated name
    ///   - price: Updated price
    ///   - url: Updated URL (optional)
    ///   - notes: Updated notes (optional)
    func updateItem(
        id: UUID,
        name: String,
        price: Decimal,
        url: String?,
        notes: String?
    ) async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Validate inputs
        guard !name.isEmpty else {
            errorMessage = "Please enter an item name."
            return false
        }

        guard price > 0 else {
            errorMessage = "Price must be greater than zero."
            return false
        }

        // Set processing state
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = UpdateWishListItemRequest(
                name: name,
                price: price,
                url: url,
                notes: notes
            )

            let updatedItem = try await apiService.updateWishListItem(forChild: childId, id: id, request)

            // Update local state
            if let index = items.firstIndex(where: { $0.id == id }) {
                items[index] = updatedItem
            }

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to update wish list item. Please try again."
            return false
        }
    }

    /// Delete a wish list item
    /// - Parameter id: Item identifier
    func deleteItem(id: UUID) async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Set processing state
        isProcessing = true
        defer { isProcessing = false }

        do {
            try await apiService.deleteWishListItem(forChild: childId, id: id)

            // Update local state
            items.removeAll { $0.id == id }

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to delete wish list item. Please try again."
            return false
        }
    }

    /// Mark wish list item as purchased
    /// - Parameter id: Item identifier
    func markAsPurchased(id: UUID) async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Set processing state
        isProcessing = true
        defer { isProcessing = false }

        do {
            let updatedItem = try await apiService.markWishListItemAsPurchased(forChild: childId, id: id)

            // Update local state
            if let index = items.firstIndex(where: { $0.id == id }) {
                items[index] = updatedItem
            }

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to mark item as purchased. Please try again."
            return false
        }
    }

    /// Refresh wish list and balance
    func refresh() async {
        await loadWishList()
        await loadBalance()
    }

    /// Clear error message
    func clearError() {
        errorMessage = nil
    }

    /// Get items that can be afforded with current balance
    var affordableItems: [WishListItem] {
        items.filter { $0.canAfford }
    }

    /// Get items that cannot be afforded yet
    var unaffordableItems: [WishListItem] {
        items.filter { !$0.canAfford }
    }

    /// Get purchased items
    var purchasedItems: [WishListItem] {
        items.filter { $0.isPurchased }
    }

    /// Get unpurchased items
    var activeItems: [WishListItem] {
        items.filter { !$0.isPurchased }
    }
}
