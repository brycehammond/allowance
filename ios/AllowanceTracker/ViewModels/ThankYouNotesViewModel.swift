import Foundation
import SwiftUI

/// ViewModel for thank you notes management (child only)
@Observable
@MainActor
final class ThankYouNotesViewModel {

    // MARK: - Observable Properties

    private(set) var pendingThankYous: [PendingThankYouDto] = []
    private(set) var currentNote: ThankYouNoteDto?
    private(set) var isLoading = false
    private(set) var isProcessing = false
    var errorMessage: String?
    var successMessage: String?

    // Selected gift for writing note
    var selectedGiftId: UUID?

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol
    private let childId: UUID

    // MARK: - Initialization

    init(childId: UUID, apiService: APIServiceProtocol = APIService()) {
        self.childId = childId
        self.apiService = apiService
    }

    // MARK: - Public Methods

    /// Load pending thank yous for the child
    func loadPendingThankYous() async {
        errorMessage = nil
        isLoading = true
        defer { isLoading = false }

        do {
            pendingThankYous = try await apiService.getPendingThankYous()
        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load pending thank yous. Please try again."
        }
    }

    /// Load existing note for a gift (if any)
    func loadNote(forGiftId giftId: UUID) async {
        errorMessage = nil
        currentNote = nil

        do {
            currentNote = try await apiService.getThankYouNote(forGiftId: giftId)
        } catch APIError.notFound {
            // No note exists yet, that's fine
            currentNote = nil
        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load thank you note. Please try again."
        }
    }

    /// Create or update a thank you note
    func saveNote(forGiftId giftId: UUID, message: String, imageUrl: String?) async -> Bool {
        errorMessage = nil
        successMessage = nil

        guard !message.isEmpty else {
            errorMessage = "Please write a message."
            return false
        }

        isProcessing = true
        defer { isProcessing = false }

        do {
            if let existingNote = currentNote {
                // Update existing note
                let request = UpdateThankYouNoteRequest(
                    message: message,
                    imageUrl: imageUrl
                )
                currentNote = try await apiService.updateThankYouNote(id: existingNote.id, request)
                successMessage = "Thank you note updated!"
            } else {
                // Create new note
                let request = CreateThankYouNoteRequest(
                    message: message,
                    imageUrl: imageUrl
                )
                currentNote = try await apiService.createThankYouNote(forGiftId: giftId, request)
                successMessage = "Thank you note saved!"

                // Update local state to reflect note exists
                if let index = pendingThankYous.firstIndex(where: { $0.giftId == giftId }) {
                    // The DTO is immutable, so we need to reload
                    await loadPendingThankYous()
                }
            }
            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to save thank you note. Please try again."
            return false
        }
    }

    /// Send a thank you note
    func sendNote(forGiftId giftId: UUID) async -> Bool {
        errorMessage = nil
        successMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            currentNote = try await apiService.sendThankYouNote(forGiftId: giftId)
            successMessage = "Thank you note sent!"

            // Reload pending list
            await loadPendingThankYous()

            // Clear selection
            selectedGiftId = nil
            currentNote = nil

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to send thank you note. Please try again."
            return false
        }
    }

    /// Select a gift to write a thank you note
    func selectGift(_ gift: PendingThankYouDto) async {
        selectedGiftId = gift.giftId
        if gift.hasNote {
            await loadNote(forGiftId: gift.giftId)
        } else {
            currentNote = nil
        }
    }

    /// Clear selection
    func clearSelection() {
        selectedGiftId = nil
        currentNote = nil
    }

    /// Refresh pending thank yous
    func refresh() async {
        await loadPendingThankYous()
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

    /// Get selected pending thank you
    var selectedPendingThankYou: PendingThankYouDto? {
        guard let giftId = selectedGiftId else { return nil }
        return pendingThankYous.first { $0.giftId == giftId }
    }

    /// Check if there are overdue thank yous (more than 7 days)
    var hasOverdueThankYous: Bool {
        pendingThankYous.contains { $0.daysSinceReceived > 7 && !$0.hasNote }
    }

    /// Get count of gifts waiting for notes
    var waitingCount: Int {
        pendingThankYous.filter { !$0.hasNote }.count
    }

    /// Get count of drafts
    var draftCount: Int {
        pendingThankYous.filter { $0.hasNote }.count
    }
}
