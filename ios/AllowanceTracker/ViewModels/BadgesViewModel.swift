import Foundation
import SwiftUI

/// ViewModel for achievement badges management
@Observable
@MainActor
final class BadgesViewModel {

    // MARK: - Observable Properties

    private(set) var earnedBadges: [ChildBadgeDto] = []
    private(set) var inProgressBadges: [BadgeProgressDto] = []
    private(set) var points: ChildPointsDto?
    private(set) var summary: AchievementSummaryDto?
    private(set) var isLoading = false
    private(set) var isProcessing = false
    var errorMessage: String?

    /// Currently selected category filter (nil = all)
    var selectedCategory: BadgeCategory?

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol
    let childId: UUID

    // MARK: - Initialization

    init(childId: UUID, apiService: APIServiceProtocol = APIService()) {
        self.childId = childId
        self.apiService = apiService
    }

    // MARK: - Public Methods

    /// Load all badge data for the child
    func loadBadges() async {
        // Clear previous errors
        errorMessage = nil

        // Set loading state
        isLoading = true
        defer { isLoading = false }

        do {
            // Load summary which includes recent badges and in-progress badges
            summary = try await apiService.getAchievementSummary(forChild: childId)

            // Load all earned badges
            earnedBadges = try await apiService.getChildBadges(
                forChild: childId,
                category: selectedCategory,
                newOnly: false
            )

            // Load badge progress
            inProgressBadges = try await apiService.getBadgeProgress(forChild: childId)

            // Load points
            points = try await apiService.getChildPoints(forChild: childId)

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load badges. Please try again."
        }
    }

    /// Load badges with a specific category filter
    func loadBadges(category: BadgeCategory?) async {
        selectedCategory = category
        await loadBadges()
    }

    /// Toggle whether a badge is displayed on the child's profile
    /// - Parameters:
    ///   - badgeId: Badge's unique identifier
    ///   - isDisplayed: Whether to display the badge
    func toggleBadgeDisplay(badgeId: UUID, isDisplayed: Bool) async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Set processing state
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = UpdateBadgeDisplayRequest(isDisplayed: isDisplayed)
            let updatedBadge = try await apiService.toggleBadgeDisplay(
                forChild: childId,
                badgeId: badgeId,
                request
            )

            // Update local state
            if let index = earnedBadges.firstIndex(where: { $0.badgeId == badgeId }) {
                earnedBadges[index] = updatedBadge
            }

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to update badge display. Please try again."
            return false
        }
    }

    /// Mark badges as seen (clears "new" indicator)
    /// - Parameter badgeIds: Array of badge IDs to mark as seen
    func markBadgesAsSeen(badgeIds: [UUID]) async -> Bool {
        // Don't show loading for this background operation
        do {
            let request = MarkBadgesSeenRequest(badgeIds: badgeIds)
            try await apiService.markBadgesSeen(forChild: childId, request)

            // Update local state - remove new indicator
            for i in earnedBadges.indices {
                if badgeIds.contains(earnedBadges[i].badgeId) {
                    // Create updated badge with isNew = false
                    let badge = earnedBadges[i]
                    earnedBadges[i] = ChildBadgeDto(
                        id: badge.id,
                        badgeId: badge.badgeId,
                        badgeName: badge.badgeName,
                        badgeDescription: badge.badgeDescription,
                        iconUrl: badge.iconUrl,
                        category: badge.category,
                        categoryName: badge.categoryName,
                        rarity: badge.rarity,
                        rarityName: badge.rarityName,
                        pointsValue: badge.pointsValue,
                        earnedAt: badge.earnedAt,
                        isDisplayed: badge.isDisplayed,
                        isNew: false,
                        earnedContext: badge.earnedContext
                    )
                }
            }

            return true

        } catch {
            // Silently fail - this is a non-critical operation
            return false
        }
    }

    /// Mark all new badges as seen
    func markAllNewBadgesAsSeen() async {
        let newBadgeIds = newBadges.map { $0.badgeId }
        guard !newBadgeIds.isEmpty else { return }
        _ = await markBadgesAsSeen(badgeIds: newBadgeIds)
    }

    /// Refresh all badge data
    func refresh() async {
        await loadBadges()
    }

    /// Clear error message
    func clearError() {
        errorMessage = nil
    }

    // MARK: - Computed Properties

    /// Badges that are new (not yet seen)
    var newBadges: [ChildBadgeDto] {
        earnedBadges.filter { $0.isNew }
    }

    /// Number of new badges
    var newBadgeCount: Int {
        newBadges.count
    }

    /// Badges filtered by selected category
    var filteredBadges: [ChildBadgeDto] {
        guard let category = selectedCategory else {
            return earnedBadges
        }
        return earnedBadges.filter { $0.category == category }
    }

    /// Badges sorted by rarity (legendary first)
    var badgesByRarity: [ChildBadgeDto] {
        filteredBadges.sorted { $0.rarity.sortOrder > $1.rarity.sortOrder }
    }

    /// Badges sorted by earned date (newest first)
    var badgesByDate: [ChildBadgeDto] {
        filteredBadges.sorted { $0.earnedAt > $1.earnedAt }
    }

    /// Total points earned
    var totalPoints: Int {
        points?.totalPoints ?? summary?.totalPoints ?? 0
    }

    /// Available points (not yet spent)
    var availablePoints: Int {
        points?.availablePoints ?? summary?.availablePoints ?? 0
    }

    /// Total badges earned
    var totalBadgesEarned: Int {
        summary?.earnedBadges ?? earnedBadges.count
    }

    /// Total badges available
    var totalBadgesAvailable: Int {
        summary?.totalBadges ?? 0
    }

    /// Progress toward total badges (0.0 to 1.0)
    var overallProgress: Double {
        guard totalBadgesAvailable > 0 else { return 0 }
        return Double(totalBadgesEarned) / Double(totalBadgesAvailable)
    }

    /// Group earned badges by category
    var badgesByCategory: [BadgeCategory: [ChildBadgeDto]] {
        Dictionary(grouping: earnedBadges) { $0.category }
    }

    /// Categories that have at least one earned badge
    var categoriesWithBadges: [BadgeCategory] {
        Array(Set(earnedBadges.map { $0.category })).sorted { $0.rawValue < $1.rawValue }
    }
}
