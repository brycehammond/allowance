import SwiftUI

/// A row view displaying a single transaction
struct TransactionRowView: View {
    let transaction: Transaction

    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            // Icon
            Image(systemName: transaction.isCredit ? "arrow.down.circle.fill" : "arrow.up.circle.fill")
                .font(.title2)
                .foregroundStyle(transaction.isCredit ? .green : .red)

            // Transaction details
            VStack(alignment: .leading, spacing: 4) {
                Text(transaction.description)
                    .font(.body)
                    .fontWeight(.medium)

                HStack(spacing: 8) {
                    Text(transaction.category)
                        .font(.caption)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 4)
                        .background(Color.blue.opacity(0.1))
                        .foregroundStyle(.blue)
                        .clipShape(RoundedRectangle(cornerRadius: 6))

                    Text(transaction.createdByName)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Text(transaction.formattedDate)
                    .font(.caption2)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            // Amount and balance
            VStack(alignment: .trailing, spacing: 4) {
                Text(transaction.formattedAmount)
                    .font(.body)
                    .fontWeight(.semibold)
                    .fontDesign(.monospaced)
                    .foregroundStyle(transaction.isCredit ? .green : .red)

                Text("Balance: \(transaction.balanceAfter.currencyFormatted)")
                    .font(.caption2)
                    .foregroundStyle(.secondary)
                    .fontDesign(.monospaced)
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 1)
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
