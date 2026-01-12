import Foundation
import SwiftUI

/// ViewModel for savings goals management
@Observable
@MainActor
final class SavingsGoalViewModel {

    // MARK: - Observable Properties

    private(set) var goals: [SavingsGoalDto] = []
    private(set) var selectedGoal: SavingsGoalDto?
    private(set) var contributions: [GoalContributionDto] = []
    private(set) var matchingRule: MatchingRuleDto?
    private(set) var activeChallenge: GoalChallengeDto?
    private(set) var isLoading = false
    private(set) var isProcessing = false
    var errorMessage: String?
    var successMessage: String?

    // Form state for contribution
    var contributionAmount: String = ""
    var contributionDescription: String = ""

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol
    private let childId: UUID
    private let isParent: Bool
    private let currentBalance: Decimal

    // MARK: - Initialization

    init(childId: UUID, isParent: Bool, currentBalance: Decimal, apiService: APIServiceProtocol = APIService()) {
        self.childId = childId
        self.isParent = isParent
        self.currentBalance = currentBalance
        self.apiService = apiService
    }

    // MARK: - Public Methods

    /// Load all savings goals for the child
    func loadGoals(includeCompleted: Bool = false) async {
        errorMessage = nil
        isLoading = true
        defer { isLoading = false }

        do {
            goals = try await apiService.getSavingsGoals(forChild: childId, status: nil, includeCompleted: includeCompleted)
        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load savings goals. Please try again."
        }
    }

    /// Load details for a specific goal
    func loadGoalDetails(goalId: UUID) async {
        errorMessage = nil
        isLoading = true
        defer { isLoading = false }

        do {
            async let goalFetch = apiService.getSavingsGoal(id: goalId)
            async let contributionsFetch = apiService.getGoalContributions(goalId: goalId, type: nil)
            async let matchingFetch = apiService.getMatchingRule(goalId: goalId)
            async let challengeFetch = apiService.getGoalChallenge(goalId: goalId)

            let (goal, contribs, matching, challenge) = try await (goalFetch, contributionsFetch, matchingFetch, challengeFetch)

            selectedGoal = goal
            contributions = contribs
            matchingRule = matching
            activeChallenge = challenge
        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load goal details. Please try again."
        }
    }

    /// Create a new savings goal
    func createGoal(
        name: String,
        description: String?,
        targetAmount: Decimal,
        category: GoalCategory,
        autoTransferType: AutoTransferType = .None,
        autoTransferAmount: Decimal? = nil,
        autoTransferPercentage: Decimal? = nil
    ) async -> Bool {
        errorMessage = nil

        // Validate inputs
        guard !name.isEmpty else {
            errorMessage = "Please enter a goal name."
            return false
        }

        guard targetAmount > 0 else {
            errorMessage = "Target amount must be greater than zero."
            return false
        }

        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = CreateSavingsGoalRequest(
                childId: childId,
                name: name,
                targetAmount: targetAmount,
                category: category,
                description: description,
                autoTransferType: autoTransferType,
                autoTransferAmount: autoTransferAmount,
                autoTransferPercentage: autoTransferPercentage
            )

            let newGoal = try await apiService.createSavingsGoal(request)
            goals.insert(newGoal, at: 0)
            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to create goal. Please try again."
            return false
        }
    }

    /// Update an existing savings goal
    func updateGoal(
        goalId: UUID,
        name: String?,
        description: String?,
        targetAmount: Decimal?,
        category: GoalCategory?,
        autoTransferType: AutoTransferType?,
        autoTransferAmount: Decimal?,
        autoTransferPercentage: Decimal?
    ) async -> Bool {
        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = UpdateSavingsGoalRequest(
                name: name,
                description: description,
                targetAmount: targetAmount,
                category: category,
                imageUrl: nil,
                autoTransferType: autoTransferType,
                autoTransferAmount: autoTransferAmount,
                autoTransferPercentage: autoTransferPercentage,
                priority: nil
            )

            let updatedGoal = try await apiService.updateSavingsGoal(id: goalId, request)
            updateGoalInList(updatedGoal)
            selectedGoal = updatedGoal
            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to update goal. Please try again."
            return false
        }
    }

    /// Delete a savings goal
    func deleteGoal(goalId: UUID) async -> Bool {
        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            try await apiService.deleteSavingsGoal(id: goalId)
            goals.removeAll { $0.id == goalId }
            if selectedGoal?.id == goalId {
                selectedGoal = nil
            }
            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to delete goal. Please try again."
            return false
        }
    }

    /// Pause a savings goal (parents only)
    func pauseGoal(goalId: UUID) async -> Bool {
        guard isParent else {
            errorMessage = "Only parents can pause goals."
            return false
        }

        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            let updatedGoal = try await apiService.pauseSavingsGoal(id: goalId)
            updateGoalInList(updatedGoal)
            selectedGoal = updatedGoal
            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to pause goal. Please try again."
            return false
        }
    }

    /// Resume a paused savings goal (parents only)
    func resumeGoal(goalId: UUID) async -> Bool {
        guard isParent else {
            errorMessage = "Only parents can resume goals."
            return false
        }

        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            let updatedGoal = try await apiService.resumeSavingsGoal(id: goalId)
            updateGoalInList(updatedGoal)
            selectedGoal = updatedGoal
            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to resume goal. Please try again."
            return false
        }
    }

    /// Contribute to a savings goal
    func contribute(goalId: UUID) async -> Bool {
        errorMessage = nil
        successMessage = nil

        guard let amount = Decimal(string: contributionAmount), amount > 0 else {
            errorMessage = "Please enter a valid amount."
            return false
        }

        guard amount <= currentBalance else {
            errorMessage = "Insufficient balance. You have \(currentBalance.currencyFormatted) available."
            return false
        }

        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = ContributeToGoalRequest(
                amount: amount,
                description: contributionDescription.isEmpty ? nil : contributionDescription
            )

            let progressEvent = try await apiService.contributeToGoal(goalId: goalId, request)

            // Update local state
            updateGoalInList(progressEvent.goal)
            selectedGoal = progressEvent.goal
            contributions.insert(progressEvent.contribution, at: 0)

            // Check for celebrations
            if !progressEvent.newMilestonesAchieved.isEmpty {
                let milestone = progressEvent.newMilestonesAchieved.first!
                successMessage = "Milestone reached: \(milestone.percentComplete)%!"
            } else if progressEvent.challengeCompleted {
                successMessage = "Challenge completed! Bonus awarded!"
            }

            // Clear form
            contributionAmount = ""
            contributionDescription = ""

            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to contribute. Please try again."
            return false
        }
    }

    /// Withdraw from a savings goal (parents only)
    func withdraw(goalId: UUID, amount: Decimal, reason: String?) async -> Bool {
        guard isParent else {
            errorMessage = "Only parents can withdraw from goals."
            return false
        }

        guard let goal = selectedGoal, amount <= goal.currentAmount else {
            errorMessage = "Withdrawal amount exceeds goal balance."
            return false
        }

        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = WithdrawFromGoalRequest(amount: amount, reason: reason)
            let contribution = try await apiService.withdrawFromGoal(goalId: goalId, request)

            contributions.insert(contribution, at: 0)

            // Reload goal to get updated balance
            await loadGoalDetails(goalId: goalId)

            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to withdraw. Please try again."
            return false
        }
    }

    /// Mark a goal as purchased (parents only)
    func markAsPurchased(goalId: UUID, notes: String?) async -> Bool {
        guard isParent else {
            errorMessage = "Only parents can mark goals as purchased."
            return false
        }

        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = notes != nil ? MarkGoalPurchasedRequest(purchaseNotes: notes) : nil
            let updatedGoal = try await apiService.markGoalAsPurchased(goalId: goalId, request)

            updateGoalInList(updatedGoal)
            selectedGoal = updatedGoal
            successMessage = "Congratulations! Goal completed!"
            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to mark as purchased. Please try again."
            return false
        }
    }

    // MARK: - Matching Rules (Parents Only)

    /// Create a matching rule for a goal
    func createMatchingRule(goalId: UUID, matchType: MatchingType, matchRatio: Decimal, maxMatchAmount: Decimal?) async -> Bool {
        guard isParent else {
            errorMessage = "Only parents can create matching rules."
            return false
        }

        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = CreateMatchingRuleRequest(
                matchType: matchType,
                matchRatio: matchRatio,
                maxMatchAmount: maxMatchAmount
            )

            matchingRule = try await apiService.createMatchingRule(goalId: goalId, request)

            // Update goal to reflect matching rule
            if var goal = goals.first(where: { $0.id == goalId }) {
                // Reload goal to get updated hasMatchingRule flag
                await loadGoalDetails(goalId: goalId)
            }

            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to create matching rule. Please try again."
            return false
        }
    }

    /// Delete matching rule for a goal
    func deleteMatchingRule(goalId: UUID) async -> Bool {
        guard isParent else {
            errorMessage = "Only parents can delete matching rules."
            return false
        }

        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            try await apiService.deleteMatchingRule(goalId: goalId)
            matchingRule = nil

            // Reload goal details
            await loadGoalDetails(goalId: goalId)

            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to delete matching rule. Please try again."
            return false
        }
    }

    // MARK: - Challenges (Parents Only)

    /// Create a challenge for a goal
    func createChallenge(goalId: UUID, targetAmount: Decimal, endDate: Date, bonusAmount: Decimal) async -> Bool {
        guard isParent else {
            errorMessage = "Only parents can create challenges."
            return false
        }

        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = CreateGoalChallengeRequest(
                targetAmount: targetAmount,
                endDate: endDate,
                bonusAmount: bonusAmount
            )

            activeChallenge = try await apiService.createGoalChallenge(goalId: goalId, request)

            // Reload goal to get updated hasActiveChallenge flag
            await loadGoalDetails(goalId: goalId)

            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to create challenge. Please try again."
            return false
        }
    }

    /// Cancel a challenge for a goal
    func cancelChallenge(goalId: UUID) async -> Bool {
        guard isParent else {
            errorMessage = "Only parents can cancel challenges."
            return false
        }

        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            try await apiService.cancelGoalChallenge(goalId: goalId)
            activeChallenge = nil

            // Reload goal details
            await loadGoalDetails(goalId: goalId)

            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to cancel challenge. Please try again."
            return false
        }
    }

    // MARK: - Helper Methods

    /// Update a goal in the local list
    private func updateGoalInList(_ updatedGoal: SavingsGoalDto) {
        if let index = goals.firstIndex(where: { $0.id == updatedGoal.id }) {
            goals[index] = updatedGoal
        }
    }

    /// Clear error message
    func clearError() {
        errorMessage = nil
    }

    /// Clear success message
    func clearSuccess() {
        successMessage = nil
    }

    /// Clear form state
    func clearFormState() {
        contributionAmount = ""
        contributionDescription = ""
    }

    /// Refresh goals
    func refresh() async {
        await loadGoals()
    }

    // MARK: - Computed Properties

    /// Get active goals
    var activeGoals: [SavingsGoalDto] {
        goals.filter { $0.status == .Active }
    }

    /// Get paused goals
    var pausedGoals: [SavingsGoalDto] {
        goals.filter { $0.status == .Paused }
    }

    /// Get completed goals (includes Completed and Purchased)
    var completedGoals: [SavingsGoalDto] {
        goals.filter { $0.status == .Completed || $0.status == .Purchased }
    }

    /// Check if there are any active goals
    var hasActiveGoals: Bool {
        !activeGoals.isEmpty
    }

    /// Total saved across all active goals
    var totalSaved: Decimal {
        activeGoals.reduce(0) { $0 + $1.currentAmount }
    }

    /// Total target across all active goals
    var totalTarget: Decimal {
        activeGoals.reduce(0) { $0 + $1.targetAmount }
    }

    /// Overall progress percentage
    var overallProgress: Double {
        guard totalTarget > 0 else { return 0 }
        return Double(truncating: (totalSaved / totalTarget) as NSNumber) * 100
    }

    /// Check if the selected goal has an active challenge
    var hasActiveChallenge: Bool {
        activeChallenge != nil
    }

    /// Check if the selected goal has a matching rule
    var hasMatchingRule: Bool {
        matchingRule != nil
    }
}
