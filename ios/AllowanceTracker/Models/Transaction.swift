import Foundation

struct Transaction: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let amount: Decimal
    let type: TransactionType
    var category: String = "General"
    let description: String
    let balanceAfter: Decimal
    let createdAt: Date
    var createdByName: String = ""

    var isCredit: Bool {
        type == .credit
    }

    var formattedAmount: String {
        let prefix = isCredit ? "+" : "-"
        return "\(prefix)\(amount.currencyFormatted)"
    }

    var formattedDate: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        formatter.timeStyle = .short
        return formatter.string(from: createdAt)
    }
}

enum TransactionType: String, Codable {
    case credit = "Credit"
    case debit = "Debit"
}

// MARK: - DTOs for API requests

struct CreateTransactionRequest: Codable {
    let childId: UUID
    let amount: Decimal
    let type: TransactionType
    let category: String
    let description: String
    let drawFromSavings: Bool

    init(
        childId: UUID,
        amount: Decimal,
        type: TransactionType,
        category: String,
        description: String,
        drawFromSavings: Bool = false
    ) {
        self.childId = childId
        self.amount = amount
        self.type = type
        self.category = category
        self.description = description
        self.drawFromSavings = drawFromSavings
    }
}
