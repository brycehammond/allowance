import Foundation
import SwiftUI

// MARK: - Category Budget

struct CategoryBudget: Codable, Identifiable, Equatable {
    let id: UUID
    let childId: UUID
    let category: TransactionCategory
    var limit: Decimal
    var period: BudgetPeriod
    var alertThresholdPercent: Int
    var enforceLimit: Bool
    let createdAt: Date
    var updatedAt: Date

    var formattedLimit: String {
        limit.currencyFormatted
    }
}

// MARK: - Budget Period

enum BudgetPeriod: String, Codable, CaseIterable, Identifiable {
    case weekly = "Weekly"
    case monthly = "Monthly"

    var id: String { rawValue }

    var icon: String {
        switch self {
        case .weekly: return "calendar.badge.clock"
        case .monthly: return "calendar"
        }
    }
}

// MARK: - Budget Status

enum BudgetStatus: String, Codable {
    case safe = "Safe"
    case warning = "Warning"
    case atLimit = "AtLimit"
    case overBudget = "OverBudget"
}

// MARK: - Set Budget Request

struct SetBudgetRequest: Codable {
    let childId: UUID
    let category: TransactionCategory
    let limit: Decimal
    let period: BudgetPeriod
    let alertThresholdPercent: Int
    let enforceLimit: Bool

    init(
        childId: UUID,
        category: TransactionCategory,
        limit: Decimal,
        period: BudgetPeriod,
        alertThresholdPercent: Int = 80,
        enforceLimit: Bool = false
    ) {
        self.childId = childId
        self.category = category
        self.limit = limit
        self.period = period
        self.alertThresholdPercent = alertThresholdPercent
        self.enforceLimit = enforceLimit
    }
}

// MARK: - Empty Response (for DELETE operations)

struct EmptyResponse: Codable {}
