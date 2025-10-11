import Foundation
import SwiftUI

/// ViewModel for the dashboard that displays family children
@MainActor
final class DashboardViewModel: ObservableObject {

    // MARK: - Published Properties

    @Published private(set) var children: [Child] = []
    @Published private(set) var isLoading = false
    @Published var errorMessage: String?

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol

    // MARK: - Initialization

    init(apiService: APIServiceProtocol = APIService()) {
        self.apiService = apiService
    }

    // MARK: - Public Methods

    /// Load all children for the current family
    func loadChildren() async {
        // Clear previous errors
        errorMessage = nil

        // Set loading state
        isLoading = true
        defer { isLoading = false }

        do {
            children = try await apiService.getChildren()

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load children. Please try again."
        }
    }

    /// Refresh children list (for pull-to-refresh)
    func refresh() async {
        await loadChildren()
    }

    /// Get a specific child by ID
    /// - Parameter id: Child's unique identifier
    /// - Returns: Child if found, nil otherwise
    func getChild(id: UUID) -> Child? {
        children.first { $0.id == id }
    }

    /// Clear error message
    func clearError() {
        errorMessage = nil
    }
}
