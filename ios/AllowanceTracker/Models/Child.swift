import Foundation

struct Child: Codable, Identifiable, Equatable {
    let id: UUID
    let firstName: String
    let lastName: String
    let weeklyAllowance: Decimal
    let currentBalance: Decimal
    let lastAllowanceDate: Date?

    var fullName: String {
        "\(firstName) \(lastName)"
    }

    var formattedBalance: String {
        currentBalance.currencyFormatted
    }
}
