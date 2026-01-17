import Foundation
import SwiftUI

/// ViewModel for gift link management (parent only)
@Observable
@MainActor
final class GiftLinkViewModel {

    // MARK: - Observable Properties

    private(set) var links: [GiftLinkDto] = []
    private(set) var selectedLinkStats: GiftLinkStatsDto?
    private(set) var isLoading = false
    private(set) var isProcessing = false
    var errorMessage: String?
    var successMessage: String?

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol
    private let childId: UUID

    // MARK: - Initialization

    init(childId: UUID, apiService: APIServiceProtocol = APIService()) {
        self.childId = childId
        self.apiService = apiService
    }

    // MARK: - Public Methods

    /// Load gift links for the child
    func loadGiftLinks() async {
        errorMessage = nil
        isLoading = true
        defer { isLoading = false }

        do {
            let allLinks = try await apiService.getGiftLinks()
            // Filter to only links for this child
            links = allLinks.filter { $0.childId == childId }
        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load gift links. Please try again."
        }
    }

    /// Create a new gift link
    func createGiftLink(
        name: String,
        description: String?,
        visibility: GiftLinkVisibility,
        expiresAt: Date?,
        maxUses: Int?,
        minAmount: Decimal?,
        maxAmount: Decimal?,
        allowedOccasions: [GiftOccasion]?,
        defaultOccasion: GiftOccasion?
    ) async -> Bool {
        errorMessage = nil
        successMessage = nil

        guard !name.isEmpty else {
            errorMessage = "Please enter a link name."
            return false
        }

        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = CreateGiftLinkRequest(
                childId: childId,
                name: name,
                description: description,
                visibility: visibility,
                expiresAt: expiresAt,
                maxUses: maxUses,
                minAmount: minAmount,
                maxAmount: maxAmount,
                allowedOccasions: allowedOccasions,
                defaultOccasion: defaultOccasion
            )

            let newLink = try await apiService.createGiftLink(request)
            links.append(newLink)
            successMessage = "Gift link created successfully!"
            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to create gift link. Please try again."
            return false
        }
    }

    /// Deactivate a gift link
    func deactivateLink(id: UUID) async -> Bool {
        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            let updatedLink = try await apiService.deactivateGiftLink(id: id)

            if let index = links.firstIndex(where: { $0.id == id }) {
                links[index] = updatedLink
            }

            successMessage = "Gift link deactivated."
            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to deactivate gift link. Please try again."
            return false
        }
    }

    /// Regenerate token for a gift link
    func regenerateToken(id: UUID) async -> Bool {
        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            let updatedLink = try await apiService.regenerateGiftLinkToken(id: id)

            if let index = links.firstIndex(where: { $0.id == id }) {
                links[index] = updatedLink
            }

            successMessage = "Gift link token regenerated. Share the new link with family members."
            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to regenerate token. Please try again."
            return false
        }
    }

    /// Load statistics for a gift link
    func loadStats(for linkId: UUID) async {
        errorMessage = nil

        do {
            selectedLinkStats = try await apiService.getGiftLinkStats(id: linkId)
        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load statistics. Please try again."
        }
    }

    /// Refresh gift links
    func refresh() async {
        await loadGiftLinks()
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

    /// Get active gift links
    var activeLinks: [GiftLinkDto] {
        links.filter { $0.isActive && !$0.isExpired && !$0.hasReachedMaxUses }
    }

    /// Get inactive gift links
    var inactiveLinks: [GiftLinkDto] {
        links.filter { !$0.isActive || $0.isExpired || $0.hasReachedMaxUses }
    }
}
