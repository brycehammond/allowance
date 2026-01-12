import Foundation
import SwiftUI

/// ViewModel for the reward shop
@Observable
@MainActor
final class RewardShopViewModel {

    // MARK: - Observable Properties

    private(set) var availableRewards: [RewardDto] = []
    private(set) var unlockedRewards: [RewardDto] = []
    private(set) var points: ChildPointsDto?
    private(set) var isLoading = false
    private(set) var isProcessing = false
    var errorMessage: String?
    var successMessage: String?

    /// Currently selected reward type filter (nil = all)
    var selectedType: RewardType?

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol
    private let childId: UUID

    // MARK: - Initialization

    init(childId: UUID, apiService: APIServiceProtocol = APIService()) {
        self.childId = childId
        self.apiService = apiService
    }

    // MARK: - Public Methods

    /// Load all reward data
    func loadRewards() async {
        errorMessage = nil

        isLoading = true
        defer { isLoading = false }

        do {
            // Load available rewards with child context for affordability
            availableRewards = try await apiService.getAvailableRewards(
                type: selectedType,
                forChild: childId
            )

            // Load unlocked rewards
            unlockedRewards = try await apiService.getChildRewards(forChild: childId)

            // Load points
            points = try await apiService.getChildPoints(forChild: childId)

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load rewards. Please try again."
        }
    }

    /// Load rewards with type filter
    func loadRewards(type: RewardType?) async {
        selectedType = type
        await loadRewards()
    }

    /// Unlock a reward using points
    /// - Parameter rewardId: Reward's unique identifier
    /// - Returns: True if successful
    func unlockReward(rewardId: UUID) async -> Bool {
        errorMessage = nil
        successMessage = nil

        isProcessing = true
        defer { isProcessing = false }

        do {
            let unlockedReward = try await apiService.unlockReward(
                forChild: childId,
                rewardId: rewardId
            )

            // Update local state
            if let index = availableRewards.firstIndex(where: { $0.id == rewardId }) {
                availableRewards[index] = unlockedReward
            }
            unlockedRewards.append(unlockedReward)

            // Refresh points
            points = try await apiService.getChildPoints(forChild: childId)

            successMessage = "Unlocked \(unlockedReward.name)!"
            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to unlock reward. Please try again."
            return false
        }
    }

    /// Equip a reward
    /// - Parameter rewardId: Reward's unique identifier
    /// - Returns: True if successful
    func equipReward(rewardId: UUID) async -> Bool {
        errorMessage = nil

        isProcessing = true
        defer { isProcessing = false }

        do {
            let equippedReward = try await apiService.equipReward(
                forChild: childId,
                rewardId: rewardId
            )

            // Update local state
            updateRewardInList(equippedReward)

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to equip reward. Please try again."
            return false
        }
    }

    /// Unequip a reward
    /// - Parameter rewardId: Reward's unique identifier
    /// - Returns: True if successful
    func unequipReward(rewardId: UUID) async -> Bool {
        errorMessage = nil

        isProcessing = true
        defer { isProcessing = false }

        do {
            try await apiService.unequipReward(forChild: childId, rewardId: rewardId)

            // Update local state - mark as not equipped
            if let index = unlockedRewards.firstIndex(where: { $0.id == rewardId }) {
                let reward = unlockedRewards[index]
                unlockedRewards[index] = RewardDto(
                    id: reward.id,
                    name: reward.name,
                    description: reward.description,
                    type: reward.type,
                    typeName: reward.typeName,
                    value: reward.value,
                    previewUrl: reward.previewUrl,
                    pointsCost: reward.pointsCost,
                    isUnlocked: true,
                    isEquipped: false,
                    canAfford: reward.canAfford
                )
            }

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to unequip reward. Please try again."
            return false
        }
    }

    /// Refresh all data
    func refresh() async {
        await loadRewards()
    }

    /// Clear error message
    func clearError() {
        errorMessage = nil
    }

    /// Clear success message
    func clearSuccess() {
        successMessage = nil
    }

    // MARK: - Private Methods

    private func updateRewardInList(_ reward: RewardDto) {
        // Unequip other rewards of the same type
        for i in unlockedRewards.indices {
            if unlockedRewards[i].type == reward.type && unlockedRewards[i].id != reward.id {
                let r = unlockedRewards[i]
                unlockedRewards[i] = RewardDto(
                    id: r.id,
                    name: r.name,
                    description: r.description,
                    type: r.type,
                    typeName: r.typeName,
                    value: r.value,
                    previewUrl: r.previewUrl,
                    pointsCost: r.pointsCost,
                    isUnlocked: r.isUnlocked,
                    isEquipped: false,
                    canAfford: r.canAfford
                )
            }
        }

        // Update the equipped reward
        if let index = unlockedRewards.firstIndex(where: { $0.id == reward.id }) {
            unlockedRewards[index] = reward
        }
    }

    // MARK: - Computed Properties

    /// Available points to spend
    var availablePoints: Int {
        points?.availablePoints ?? 0
    }

    /// Total points earned
    var totalPoints: Int {
        points?.totalPoints ?? 0
    }

    /// Rewards filtered by type
    var filteredRewards: [RewardDto] {
        guard let type = selectedType else {
            return availableRewards
        }
        return availableRewards.filter { $0.type == type }
    }

    /// Rewards that can be afforded
    var affordableRewards: [RewardDto] {
        availableRewards.filter { $0.canAfford && !$0.isUnlocked }
    }

    /// Currently equipped rewards
    var equippedRewards: [RewardDto] {
        unlockedRewards.filter { $0.isEquipped }
    }

    /// Group rewards by type
    var rewardsByType: [RewardType: [RewardDto]] {
        Dictionary(grouping: availableRewards) { $0.type }
    }

    /// Group unlocked rewards by type
    var unlockedByType: [RewardType: [RewardDto]] {
        Dictionary(grouping: unlockedRewards) { $0.type }
    }
}
