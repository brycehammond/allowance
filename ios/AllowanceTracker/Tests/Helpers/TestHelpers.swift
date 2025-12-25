import Foundation
@testable import AllowanceTracker

// MARK: - Child Test Helper

extension Child {
    static func makeForTest(
        id: UUID = UUID(),
        firstName: String = "Test",
        lastName: String = "Child",
        weeklyAllowance: Decimal = 10.00,
        currentBalance: Decimal = 0.00,
        savingsBalance: Decimal = 0.00,
        lastAllowanceDate: Date? = nil,
        allowanceDay: Weekday? = nil,
        savingsAccountEnabled: Bool = false,
        savingsTransferType: SavingsTransferType = .none,
        savingsTransferPercentage: Decimal? = nil,
        savingsTransferAmount: Decimal? = nil,
        savingsBalanceVisibleToChild: Bool = true,
        allowDebt: Bool = false
    ) -> Child {
        Child(
            id: id,
            firstName: firstName,
            lastName: lastName,
            weeklyAllowance: weeklyAllowance,
            currentBalance: currentBalance,
            savingsBalance: savingsBalance,
            lastAllowanceDate: lastAllowanceDate,
            allowanceDay: allowanceDay,
            savingsAccountEnabled: savingsAccountEnabled,
            savingsTransferType: savingsTransferType,
            savingsTransferPercentage: savingsTransferPercentage,
            savingsTransferAmount: savingsTransferAmount,
            savingsBalanceVisibleToChild: savingsBalanceVisibleToChild,
            allowDebt: allowDebt
        )
    }
}
