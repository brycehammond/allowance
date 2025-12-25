import SwiftUI

/// A card view displaying a wish list item with progress
struct WishListItemCard: View {
    let item: WishListItem
    let currentBalance: Decimal
    let isParent: Bool
    let onEdit: () -> Void
    let onDelete: () -> Void
    let onMarkPurchased: () -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            // Header
            HStack(alignment: .top) {
                VStack(alignment: .leading, spacing: 4) {
                    Text(item.name)
                        .font(.headline)
                        .lineLimit(2)

                    Text(item.formattedPrice)
                        .font(.title3)
                        .fontWeight(.bold)
                        .fontDesign(.monospaced)
                        .foregroundStyle(Color.amber500)
                }

                Spacer()

                if item.isPurchased {
                    Image(systemName: "checkmark.circle.fill")
                        .font(.title2)
                        .foregroundStyle(Color.green500)
                }
            }

            // Progress bar (if not purchased)
            if !item.isPurchased {
                VStack(alignment: .leading, spacing: 8) {
                    HStack {
                        Text("Progress")
                            .font(.caption)
                            .foregroundStyle(.secondary)

                        Spacer()

                        Text("\(Int(item.progressPercentage(currentBalance: currentBalance) * 100))%")
                            .font(.caption)
                            .fontWeight(.medium)
                            .foregroundStyle(item.canAfford ? Color.green500 : .secondary)
                    }

                    ProgressView(value: item.progressPercentage(currentBalance: currentBalance))
                        .tint(item.canAfford ? Color.green500 : Color.amber500)

                    HStack {
                        Text("Your balance:")
                            .font(.caption2)
                            .foregroundStyle(.secondary)

                        Text(currentBalance.currencyFormatted)
                            .font(.caption2)
                            .fontWeight(.medium)
                            .fontDesign(.monospaced)

                        Spacer()

                        if item.canAfford {
                            Text("You can afford this!")
                                .font(.caption2)
                                .fontWeight(.semibold)
                                .foregroundStyle(Color.green500)
                        } else {
                            let needed = item.price - currentBalance
                            Text("Need \(needed.currencyFormatted) more")
                                .font(.caption2)
                                .foregroundStyle(.secondary)
                        }
                    }
                }
            }

            // Notes (if available)
            if let notes = item.notes, !notes.isEmpty {
                Text(notes)
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .lineLimit(3)
            }

            // URL (if available)
            if let url = item.url, !url.isEmpty, let urlObj = URL(string: url) {
                Link(destination: urlObj) {
                    Label("View Item", systemImage: "link")
                        .font(.caption)
                        .foregroundStyle(Color.green600)
                }
            }

            // Purchase date (if purchased)
            if item.isPurchased, let purchasedAt = item.purchasedAt {
                HStack {
                    Label("Purchased", systemImage: "checkmark.circle.fill")
                        .font(.caption)
                        .foregroundStyle(Color.green500)

                    Spacer()

                    Text(purchasedAt.formattedDisplay)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }

            Divider()

            // Actions
            HStack(spacing: 8) {
                if !item.isPurchased {
                    Button(action: onEdit) {
                        Label("Edit", systemImage: "pencil")
                            .font(.caption)
                    }
                    .buttonStyle(.bordered)
                    .controlSize(.small)

                    // Only parents can mark items as purchased
                    if isParent && item.canAfford {
                        Button(action: onMarkPurchased) {
                            Label("Mark Purchased", systemImage: "checkmark")
                                .font(.caption)
                        }
                        .buttonStyle(.borderedProminent)
                        .controlSize(.small)
                    }
                }

                Spacer()

                Button(role: .destructive, action: onDelete) {
                    Label("Delete", systemImage: "trash")
                        .font(.caption)
                }
                .buttonStyle(.bordered)
                .controlSize(.small)
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
        .opacity(item.isPurchased ? 0.7 : 1.0)
    }
}

// MARK: - Preview Provider

#Preview("Wish List Item - Affordable (Parent)") {
    WishListItemCard(
        item: WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "LEGO Star Wars Set",
            price: 49.99,
            url: "https://example.com",
            notes: "The big one with the Millennium Falcon",
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: true
        ),
        currentBalance: 125.50,
        isParent: true,
        onEdit: { print("Edit") },
        onDelete: { print("Delete") },
        onMarkPurchased: { print("Mark Purchased") }
    )
    .padding()
    .previewLayout(.sizeThatFits)
}

#Preview("Wish List Item - Affordable (Child)") {
    WishListItemCard(
        item: WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "LEGO Star Wars Set",
            price: 49.99,
            url: "https://example.com",
            notes: "The big one with the Millennium Falcon",
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: true
        ),
        currentBalance: 125.50,
        isParent: false,
        onEdit: { print("Edit") },
        onDelete: { print("Delete") },
        onMarkPurchased: { print("Mark Purchased") }
    )
    .padding()
    .previewLayout(.sizeThatFits)
}

#Preview("Wish List Item - Need More") {
    WishListItemCard(
        item: WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "Nintendo Switch Game",
            price: 59.99,
            url: nil,
            notes: "The new Zelda game",
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: false
        ),
        currentBalance: 25.00,
        isParent: true,
        onEdit: { print("Edit") },
        onDelete: { print("Delete") },
        onMarkPurchased: { print("Mark Purchased") }
    )
    .padding()
    .previewLayout(.sizeThatFits)
}

#Preview("Wish List Item - Purchased") {
    WishListItemCard(
        item: WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "Art Supplies Kit",
            price: 29.99,
            url: "https://example.com",
            notes: "Paint, brushes, and canvas",
            isPurchased: true,
            purchasedAt: Date(),
            createdAt: Date().addingTimeInterval(-86400 * 7),
            canAfford: true
        ),
        currentBalance: 95.50,
        isParent: true,
        onEdit: { print("Edit") },
        onDelete: { print("Delete") },
        onMarkPurchased: { print("Mark Purchased") }
    )
    .padding()
    .previewLayout(.sizeThatFits)
}
