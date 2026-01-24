import Foundation
import SwiftUI
import PhotosUI

/// ViewModel for chores/tasks management
@Observable
@MainActor
final class TaskViewModel {

    // MARK: - Observable Properties

    private(set) var tasks: [ChoreTask] = []
    private(set) var pendingApprovals: [TaskCompletion] = []
    private(set) var isLoading = false
    private(set) var isProcessing = false
    var errorMessage: String?

    // Form state for task completion
    var completionNotes: String = ""
    var selectedPhoto: PhotosPickerItem?
    var selectedPhotoData: Data?

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol
    private let childId: UUID
    private let isParent: Bool

    // MARK: - Initialization

    init(childId: UUID, isParent: Bool, apiService: APIServiceProtocol = ServiceProvider.apiService) {
        self.childId = childId
        self.isParent = isParent
        self.apiService = apiService
    }

    // MARK: - Public Methods

    /// Load tasks for the child
    func loadTasks() async {
        errorMessage = nil
        isLoading = true
        defer { isLoading = false }

        do {
            tasks = try await apiService.getTasks(childId: childId, status: .Active, isRecurring: nil)

            // Load pending approvals for parents
            if isParent {
                let allPending = try await apiService.getPendingApprovals()
                // Filter to only this child's pending approvals
                pendingApprovals = allPending.filter { $0.childId == childId }
            }
        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to load tasks. Please try again."
        }
    }

    /// Create a new task
    /// - Parameters:
    ///   - title: Task title
    ///   - description: Optional description
    ///   - rewardAmount: Reward amount for completing the task
    ///   - isRecurring: Whether task repeats
    ///   - recurrenceType: Type of recurrence (Daily/Weekly/Monthly)
    ///   - recurrenceDay: Day of week for weekly tasks
    ///   - recurrenceDayOfMonth: Day of month for monthly tasks
    func createTask(
        title: String,
        description: String?,
        rewardAmount: Decimal,
        isRecurring: Bool,
        recurrenceType: RecurrenceType?,
        recurrenceDay: Weekday?,
        recurrenceDayOfMonth: Int?
    ) async -> Bool {
        errorMessage = nil

        // Validate inputs
        guard !title.isEmpty else {
            errorMessage = "Please enter a task title."
            return false
        }

        guard rewardAmount > 0 else {
            errorMessage = "Reward must be greater than zero."
            return false
        }

        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = CreateTaskRequest(
                childId: childId,
                title: title,
                description: description,
                rewardAmount: rewardAmount,
                isRecurring: isRecurring,
                recurrenceType: recurrenceType,
                recurrenceDay: recurrenceDay,
                recurrenceDayOfMonth: recurrenceDayOfMonth
            )

            let newTask = try await apiService.createTask(request)
            tasks.append(newTask)
            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to create task. Please try again."
            return false
        }
    }

    /// Archive a task
    /// - Parameter id: Task identifier
    func archiveTask(id: UUID) async -> Bool {
        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            try await apiService.archiveTask(id: id)
            tasks.removeAll { $0.id == id }
            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to archive task. Please try again."
            return false
        }
    }

    /// Complete a task with optional photo
    /// - Parameters:
    ///   - taskId: Task identifier
    ///   - notes: Optional completion notes
    func completeTask(taskId: UUID) async -> Bool {
        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            let completion = try await apiService.completeTask(
                id: taskId,
                notes: completionNotes.isEmpty ? nil : completionNotes,
                photoData: selectedPhotoData,
                photoFileName: selectedPhotoData != nil ? "photo.jpg" : nil
            )

            // Clear form state
            completionNotes = ""
            selectedPhoto = nil
            selectedPhotoData = nil

            // Reload to update pending approvals count
            await loadTasks()

            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to complete task. Please try again."
            return false
        }
    }

    /// Approve a task completion
    /// - Parameter completionId: Completion identifier
    func approveCompletion(completionId: UUID) async -> Bool {
        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = ReviewCompletionRequest(isApproved: true, rejectionReason: nil)
            _ = try await apiService.reviewCompletion(id: completionId, request)

            // Remove from pending list
            pendingApprovals.removeAll { $0.id == completionId }

            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to approve completion. Please try again."
            return false
        }
    }

    /// Reject a task completion
    /// - Parameters:
    ///   - completionId: Completion identifier
    ///   - reason: Optional rejection reason
    func rejectCompletion(completionId: UUID, reason: String?) async -> Bool {
        errorMessage = nil
        isProcessing = true
        defer { isProcessing = false }

        do {
            let request = ReviewCompletionRequest(isApproved: false, rejectionReason: reason)
            _ = try await apiService.reviewCompletion(id: completionId, request)

            // Remove from pending list
            pendingApprovals.removeAll { $0.id == completionId }

            return true
        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to reject completion. Please try again."
            return false
        }
    }

    /// Load photo data from PhotosPickerItem
    func loadPhotoData() async {
        guard let item = selectedPhoto else {
            selectedPhotoData = nil
            return
        }

        do {
            if let data = try await item.loadTransferable(type: Data.self) {
                selectedPhotoData = data
            }
        } catch {
            errorMessage = "Failed to load photo."
        }
    }

    /// Clear form state
    func clearFormState() {
        completionNotes = ""
        selectedPhoto = nil
        selectedPhotoData = nil
    }

    /// Clear error message
    func clearError() {
        errorMessage = nil
    }

    /// Refresh tasks and pending approvals
    func refresh() async {
        await loadTasks()
    }

    // MARK: - Computed Properties

    /// Get recurring tasks
    var recurringTasks: [ChoreTask] {
        tasks.filter { $0.isRecurring }
    }

    /// Get one-time tasks
    var oneTimeTasks: [ChoreTask] {
        tasks.filter { !$0.isRecurring }
    }

    /// Get tasks with pending approvals
    var tasksWithPending: [ChoreTask] {
        tasks.filter { $0.pendingApprovals > 0 }
    }

    /// Check if there are any pending approvals
    var hasPendingApprovals: Bool {
        !pendingApprovals.isEmpty
    }

    /// Total pending approvals count
    var pendingApprovalsCount: Int {
        pendingApprovals.count
    }
}
