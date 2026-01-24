import Foundation
import SwiftUI

/// ViewModel for analytics and financial insights
@Observable
@MainActor
final class AnalyticsViewModel {

    // MARK: - Observable Properties

    private(set) var balanceHistory: [BalancePoint] = []
    private(set) var incomeSpendingSummary: IncomeSpendingSummary?
    private(set) var spendingBreakdown: [CategoryBreakdown] = []
    private(set) var monthlyComparison: [MonthlyComparison] = []

    private(set) var isLoadingHistory = false
    private(set) var isLoadingSummary = false
    private(set) var isLoadingBreakdown = false
    private(set) var isLoadingComparison = false

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

    /// Load balance history for the child
    /// - Parameter days: Number of days to retrieve (default: 30)
    func loadBalanceHistory(days: Int = 30) async {
        // Clear previous errors
        errorMessage = nil

        // Set loading state
        isLoadingHistory = true
        defer { isLoadingHistory = false }

        do {
            balanceHistory = try await apiService.getBalanceHistory(forChild: childId, days: days)

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load balance history. Please try again."
        }
    }

    /// Load income vs spending summary
    func loadIncomeSpendingSummary() async {
        // Clear previous errors
        errorMessage = nil

        // Set loading state
        isLoadingSummary = true
        defer { isLoadingSummary = false }

        do {
            incomeSpendingSummary = try await apiService.getIncomeVsSpending(forChild: childId)

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load income vs spending. Please try again."
        }
    }

    /// Load spending breakdown by category
    func loadSpendingBreakdown() async {
        // Clear previous errors
        errorMessage = nil

        // Set loading state
        isLoadingBreakdown = true
        defer { isLoadingBreakdown = false }

        do {
            spendingBreakdown = try await apiService.getSpendingBreakdown(forChild: childId)

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load spending breakdown. Please try again."
        }
    }

    /// Load monthly comparison data
    /// - Parameter months: Number of months to retrieve (default: 6)
    func loadMonthlyComparison(months: Int = 6) async {
        // Clear previous errors
        errorMessage = nil

        // Set loading state
        isLoadingComparison = true
        defer { isLoadingComparison = false }

        do {
            monthlyComparison = try await apiService.getMonthlyComparison(forChild: childId, months: months)

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load monthly comparison. Please try again."
        }
    }

    /// Load all analytics data in parallel
    func loadAllData() async {
        await withTaskGroup(of: Void.self) { group in
            group.addTask { await self.loadBalanceHistory() }
            group.addTask { await self.loadIncomeSpendingSummary() }
            group.addTask { await self.loadSpendingBreakdown() }
            group.addTask { await self.loadMonthlyComparison() }
        }
    }

    /// Refresh all analytics data
    func refresh() async {
        await loadAllData()
    }

    /// Clear error message
    func clearError() {
        errorMessage = nil
    }

    // MARK: - Computed Properties

    /// Check if any data is currently loading
    var isLoading: Bool {
        isLoadingHistory || isLoadingSummary || isLoadingBreakdown || isLoadingComparison
    }

    /// Get the latest balance from history
    var latestBalance: Decimal? {
        balanceHistory.first?.balance
    }

    /// Get formatted latest balance
    var formattedLatestBalance: String? {
        latestBalance?.currencyFormatted
    }

    /// Get top spending categories (top 5)
    var topSpendingCategories: [CategoryBreakdown] {
        Array(spendingBreakdown.prefix(5))
    }

    /// Get most recent month comparison
    var currentMonthComparison: MonthlyComparison? {
        monthlyComparison.first
    }

    /// Get savings trend (positive if improving)
    var savingsTrend: Decimal? {
        guard monthlyComparison.count >= 2 else { return nil }
        let current = monthlyComparison[0].netSavings
        let previous = monthlyComparison[1].netSavings
        return current - previous
    }

    /// Check if savings are improving
    var isSavingsImproving: Bool {
        guard let trend = savingsTrend else { return false }
        return trend > 0
    }
}
