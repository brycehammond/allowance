import Foundation

struct WishListItem: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let name: String
    let price: Decimal
    let url: String?
    let notes: String?
    let isPurchased: Bool
    let purchasedAt: Date?
    let createdAt: Date
    let canAfford: Bool

    func progressPercentage(currentBalance: Decimal) -> Double {
        guard price > 0 else { return 1.0 }
        let progress = (currentBalance / price)
        return min(Double(truncating: progress as NSDecimalNumber), 1.0)
    }

    var formattedPrice: String {
        price.currencyFormatted
    }

    var formattedCreatedDate: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        return formatter.string(from: createdAt)
    }
}

// MARK: - DTOs for API requests

struct CreateWishListItemRequest: Codable {
    let childId: UUID
    let name: String
    let price: Decimal
    let url: String?
    let notes: String?
}

struct UpdateWishListItemRequest: Codable {
    let name: String
    let price: Decimal
    let url: String?
    let notes: String?
}
