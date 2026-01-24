import Foundation
import SwiftUI

/// ViewModel for pending gifts management (parent only)
@Observable
@MainActor
final class PendingGiftsViewModel {

    // MARK: - Observable Properties

    private(set) var gifts: [GiftDto] = []
    private(set) var savingsGoals: [SavingsGoalDto] = []
    private(set) var isLoading = false
    private(set) var isProcessing = false
    var errorMessage: String?
    var successMessage: String?

    // Filter
    var showPendingOnly = true

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol
    private let childId: UUID

    // MARK: - Initialization

    init(childId: UUID, apiService: APIServiceProtocol = ServiceProvider.apiService) {
        self.childId = childId
        self.apiService = apiService
    }

    // MARK: - Public Methods

    /// Load gifts for the child
    func loadGifts() async {
        errorMessage = nil
        isLoading = true
        defer { isLoading = false }

        do {
            async let giftsTask = apiService.getGifts(forChild: childId)
            async let goalsTask = apiService.getSavingsGoals(forChild: childId, status: .Active, includeCompleted: false)

            gifts = try await giftsTask
            savingsGoals = try await goalsTask

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load gifts. Please try again."
        }
    }

    /// Approve a pending gift
    func approveGift(
        id: UUID,
        allocateToGoalId: UUID?,
        savingsPercentage: Int?
    ) async -> Bool {
        errorMessage = nil
        successMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = ApproveGiftRequest(
                allocateToGoalId: allocateToGoalId,
                savingsPercentage: savingsPercentage
            )

            let updatedGift = try await apiService.approveGift(id: id, request)

            if let index = gifts.firstIndex(where: { $0.id == id }) {
                gifts[index] = updatedGift
            }

            successMessage = "Gift approved! \(updatedGift.formattedAmount) added to balance."
            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to approve gift. Please try again."
            return false
        }
    }

    /// Reject a pending gift
    func rejectGift(id: UUID, reason: String?) async -> Bool {
        errorMessage = nil
        successMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = RejectGiftRequest(reason: reason)
            let updatedGift = try await apiService.rejectGift(id: id, request)

            if let index = gifts.firstIndex(where: { $0.id == id }) {
                gifts[index] = updatedGift
            }

            successMessage = "Gift rejected."
            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to reject gift. Please try again."
            return false
        }
    }

    /// Refresh gifts
    func refresh() async {
        await loadGifts()
    }

    /// Clear error message
    func clearError() {
        errorMessage = nil
    }

    /// Clear success message
    func clearSuccess() {
        successMessage = nil
    }

    // MARK: - Computed Properties

    /// Get filtered gifts based on current filter
    var filteredGifts: [GiftDto] {
        if showPendingOnly {
            return pendingGifts
        }
        return gifts
    }

    /// Get pending gifts
    var pendingGifts: [GiftDto] {
        gifts.filter { $0.status == .Pending }
    }

    /// Get approved gifts
    var approvedGifts: [GiftDto] {
        gifts.filter { $0.status == .Approved }
    }

    /// Get rejected gifts
    var rejectedGifts: [GiftDto] {
        gifts.filter { $0.status == .Rejected }
    }

    /// Number of pending gifts
    var pendingCount: Int {
        pendingGifts.count
    }

    /// Total amount pending
    var totalPendingAmount: Decimal {
        pendingGifts.reduce(0) { $0 + $1.amount }
    }

    /// Formatted total pending amount
    var formattedTotalPending: String {
        totalPendingAmount.currencyFormatted
    }
}
