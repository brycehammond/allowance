import Foundation
import SwiftUI

// MARK: - Transaction Category Enum

enum TransactionCategory: String, Codable, CaseIterable, Identifiable {
    // Income Categories
    case allowance = "Allowance"
    case chores = "Chores"
    case gift = "Gift"
    case bonusReward = "BonusReward"
    case otherIncome = "OtherIncome"

    // Spending Categories
    case toys = "Toys"
    case games = "Games"
    case books = "Books"
    case clothes = "Clothes"
    case snacks = "Snacks"
    case candy = "Candy"
    case electronics = "Electronics"
    case entertainment = "Entertainment"
    case sports = "Sports"
    case crafts = "Crafts"
    case savings = "Savings"
    case charity = "Charity"
    case otherSpending = "OtherSpending"

    var id: String { rawValue }

    var displayName: String {
        // Convert camelCase to Title Case
        rawValue.replacingOccurrences(
            of: "([a-z])([A-Z])",
            with: "$1 $2",
            options: .regularExpression
        )
    }

    var icon: String {
        switch self {
        case .allowance: return "dollarsign.circle.fill"
        case .chores: return "list.bullet.clipboard.fill"
        case .gift: return "gift.fill"
        case .bonusReward: return "star.fill"
        case .otherIncome: return "plus.circle.fill"
        case .toys: return "teddybear.fill"
        case .games: return "gamecontroller.fill"
        case .books: return "book.fill"
        case .clothes: return "tshirt.fill"
        case .snacks: return "fork.knife"
        case .candy: return "birthday.cake.fill"
        case .electronics: return "iphone"
        case .entertainment: return "tv.fill"
        case .sports: return "sportscourt.fill"
        case .crafts: return "paintbrush.fill"
        case .savings: return "banknote.fill"
        case .charity: return "heart.fill"
        case .otherSpending: return "minus.circle.fill"
        }
    }

    var isIncome: Bool {
        [.allowance, .chores, .gift, .bonusReward, .otherIncome].contains(self)
    }

    static var incomeCategories: [TransactionCategory] {
        allCases.filter { $0.isIncome }
    }

    static var spendingCategories: [TransactionCategory] {
        allCases.filter { !$0.isIncome }
    }
}

// MARK: - Transaction Type

enum TransactionType: String, Codable {
    case credit = "Credit"
    case debit = "Debit"
}
