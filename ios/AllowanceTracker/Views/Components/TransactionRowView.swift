import SwiftUI

/// A row view displaying a single transaction
struct TransactionRowView: View {
    let transaction: Transaction

    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            // Icon
            Image(systemName: transaction.isCredit ? "arrow.down.circle.fill" : "arrow.up.circle.fill")
                .font(.title2)
                .foregroundStyle(transaction.isCredit ? Color.green500 : Color.error)
                .accessibilityHidden()

            // Transaction details
            VStack(alignment: .leading, spacing: 4) {
                Text(transaction.description)
                    .font(.scalable(.body, weight: .medium))
                    .accessibilityHidden()

                HStack(spacing: 8) {
                    Text(transaction.category)
                        .font(.scalable(.caption))
                        .padding(.horizontal, 8)
                        .padding(.vertical, 4)
                        .background(Color.green500.opacity(0.1))
                        .foregroundStyle(Color.green600)
                        .clipShape(RoundedRectangle(cornerRadius: 6))
                        .accessibilityHidden()

                    Text(transaction.createdByName)
                        .font(.scalable(.caption))
                        .foregroundStyle(.secondary)
                        .accessibilityHidden()
                }

                Text(transaction.formattedDate)
                    .font(.scalable(.caption2))
                    .foregroundStyle(.secondary)
                    .accessibilityHidden()
            }

            Spacer()

            // Amount and balance
            VStack(alignment: .trailing, spacing: 4) {
                Text(transaction.formattedAmount)
                    .font(.scalable(.body, weight: .semibold))
                    .fontDesign(.monospaced)
                    .foregroundStyle(transaction.isCredit ? Color.green500 : Color.error)
                    .accessibilityHidden()

                Text("Balance: \(transaction.balanceAfter.currencyFormatted)")
                    .font(.scalable(.caption2))
                    .foregroundStyle(.secondary)
                    .fontDesign(.monospaced)
                    .accessibilityHidden()
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 1)
        .accessibility(
            label: accessibilityLabel,
            value: accessibilityValue
        )
        .accessibilityIdentifier("\(AccessibilityIdentifier.transactionRow)\(transaction.id.uuidString)")
    }

    // MARK: - Accessibility

    /// Comprehensive accessibility label
    private var accessibilityLabel: String {
        var parts: [String] = []

        // Transaction type
        parts.append(transaction.type.accessibilityLabel)

        // Amount
        parts.append(transaction.amount.accessibilityCurrencyLabel)

        // Description
        parts.append(transaction.description)

        // Category
        parts.append("Category: \(transaction.category)")

        // Created by
        parts.append("Created by \(transaction.createdByName)")

        // Date
        parts.append(transaction.createdAt.accessibilityLabel)

        return parts.joined(separator: ". ")
    }

    /// Accessibility value showing resulting balance
    private var accessibilityValue: String {
        "Resulting balance: \(transaction.balanceAfter.accessibilityCurrencyLabel)"
    }
}

// MARK: - Preview Provider

#Preview("Credit Transaction") {
    TransactionRowView(
        transaction: Transaction(
            id: UUID(),
            childId: UUID(),
            amount: 10.00,
            type: .credit,
            category: "Allowance",
            description: "Weekly allowance",
            balanceAfter: 110.00,
            createdAt: Date(),
            createdByName: "John Doe"
        )
    )
    .padding()
    .previewLayout(.sizeThatFits)
}

#Preview("Debit Transaction") {
    TransactionRowView(
        transaction: Transaction(
            id: UUID(),
            childId: UUID(),
            amount: 5.50,
            type: .debit,
            category: "Toys",
            description: "Toy car purchase",
            balanceAfter: 104.50,
            createdAt: Date(),
            createdByName: "Jane Doe"
        )
    )
    .padding()
    .previewLayout(.sizeThatFits)
}

#Preview("Multiple Transactions") {
    VStack(spacing: 8) {
        TransactionRowView(
            transaction: Transaction(
                id: UUID(),
                childId: UUID(),
                amount: 10.00,
                type: .credit,
                category: "Allowance",
                description: "Weekly allowance",
                balanceAfter: 110.00,
                createdAt: Date(),
                createdByName: "John Doe"
            )
        )

        TransactionRowView(
            transaction: Transaction(
                id: UUID(),
                childId: UUID(),
                amount: 5.50,
                type: .debit,
                category: "Toys",
                description: "Toy car purchase",
                balanceAfter: 104.50,
                createdAt: Date().addingTimeInterval(-3600),
                createdByName: "Jane Doe"
            )
        )

        TransactionRowView(
            transaction: Transaction(
                id: UUID(),
                childId: UUID(),
                amount: 2.25,
                type: .debit,
                category: "Snacks",
                description: "Candy at store",
                balanceAfter: 102.25,
                createdAt: Date().addingTimeInterval(-7200),
                createdByName: "Alice Smith"
            )
        )
    }
    .padding()
}
