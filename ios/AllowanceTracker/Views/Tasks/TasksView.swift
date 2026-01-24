import SwiftUI
import PhotosUI

/// View displaying tasks/chores for a child
@MainActor
struct TasksView: View {

    // MARK: - Properties

    @State private var viewModel: TaskViewModel
    @Environment(AuthViewModel.self) private var authViewModel
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @State private var showingAddTask = false
    @State private var completingTask: ChoreTask?
    @State private var archivingTask: ChoreTask?

    // MARK: - Computed Properties

    private var isParent: Bool {
        authViewModel.effectiveIsParent
    }

    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    // MARK: - Initialization

    init(childId: UUID, isParent: Bool, apiService: APIServiceProtocol = ServiceProvider.apiService) {
        _viewModel = State(wrappedValue: TaskViewModel(childId: childId, isParent: isParent, apiService: apiService))
    }

    // MARK: - Body

    var body: some View {
        ZStack {
            if viewModel.isLoading && viewModel.tasks.isEmpty {
                ProgressView("Loading tasks...")
            } else if viewModel.tasks.isEmpty {
                emptyStateView
            } else {
                tasksListView
            }
        }
        .navigationTitle("Chores")
        .toolbar {
            if isParent {
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button {
                        showingAddTask = true
                    } label: {
                        Image(systemName: "plus.circle.fill")
                    }
                }
            }
        }
        .sheet(isPresented: $showingAddTask) {
            AddTaskView(viewModel: viewModel)
        }
        .sheet(item: $completingTask) { task in
            CompleteTaskView(viewModel: viewModel, task: task)
        }
        .confirmationDialog(
            "Archive Task",
            isPresented: .constant(archivingTask != nil),
            presenting: archivingTask
        ) { task in
            Button("Archive", role: .destructive) {
                Task {
                    await archiveTask(task)
                }
            }
        } message: { task in
            Text("Are you sure you want to archive \"\(task.title)\"? This will remove it from the active tasks list.")
        }
        .refreshable {
            await viewModel.refresh()
        }
        .alert("Error", isPresented: .constant(viewModel.errorMessage != nil)) {
            Button("OK") {
                viewModel.clearError()
            }
        } message: {
            if let errorMessage = viewModel.errorMessage {
                Text(errorMessage)
            }
        }
        .task {
            await viewModel.loadTasks()
        }
    }

    // MARK: - Subviews

    private var tasksListView: some View {
        ScrollView {
            VStack(spacing: 16) {
                // Pending approvals section (Parents only)
                if isParent && viewModel.hasPendingApprovals {
                    pendingApprovalsSection
                        .adaptivePadding(.horizontal)
                }

                // Tasks - use grid on iPad for multi-column layout
                if isRegularWidth {
                    AdaptiveGrid(minItemWidth: 350, spacing: 12) {
                        ForEach(viewModel.tasks) { task in
                            TaskCard(
                                task: task,
                                isParent: isParent,
                                onComplete: {
                                    completingTask = task
                                },
                                onArchive: {
                                    archivingTask = task
                                }
                            )
                        }
                    }
                    .adaptivePadding(.horizontal)
                } else {
                    VStack(spacing: 8) {
                        ForEach(viewModel.tasks) { task in
                            TaskCard(
                                task: task,
                                isParent: isParent,
                                onComplete: {
                                    completingTask = task
                                },
                                onArchive: {
                                    archivingTask = task
                                }
                            )
                            .padding(.horizontal, 16)
                        }
                    }
                }
            }
            .padding(.vertical)
        }
    }

    private var pendingApprovalsSection: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Label("Pending Approvals", systemImage: "clock.badge.exclamationmark")
                    .font(.headline)
                    .foregroundStyle(.orange)

                Spacer()

                Text("\(viewModel.pendingApprovalsCount)")
                    .font(.subheadline)
                    .fontWeight(.semibold)
                    .padding(.horizontal, 12)
                    .padding(.vertical, 4)
                    .background(Color.orange.opacity(0.2))
                    .clipShape(Capsule())
            }

            ForEach(viewModel.pendingApprovals) { completion in
                PendingApprovalCard(
                    completion: completion,
                    onApprove: {
                        Task {
                            await viewModel.approveCompletion(completionId: completion.id)
                        }
                    },
                    onReject: { reason in
                        Task {
                            await viewModel.rejectCompletion(completionId: completion.id, reason: reason)
                        }
                    }
                )
            }
        }
        .padding()
        .background(Color.orange.opacity(0.1))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    private var emptyStateView: some View {
        VStack(spacing: 24) {
            Image(systemName: "checkmark.circle.badge.questionmark")
                .font(.system(size: 60))
                .foregroundStyle(.secondary)

            VStack(spacing: 8) {
                Text("No Tasks Yet")
                    .font(.title2)
                    .fontWeight(.bold)

                Text(isParent ? "Create tasks for your child to earn rewards!" : "No tasks assigned yet.")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }

            if isParent {
                Button {
                    showingAddTask = true
                } label: {
                    Label("Add Task", systemImage: "plus.circle.fill")
                        .fontWeight(.semibold)
                        .frame(maxWidth: .infinity)
                        .padding()
                        .background(Color.green500)
                        .foregroundStyle(.white)
                        .clipShape(RoundedRectangle(cornerRadius: 12))
                }
                .padding(.horizontal, 40)
                .padding(.top)
            }
        }
        .frame(maxWidth: 500)
        .adaptivePadding()
    }

    // MARK: - Methods

    private func archiveTask(_ task: ChoreTask) async {
        _ = await viewModel.archiveTask(id: task.id)
        archivingTask = nil
    }
}

// MARK: - Task Card

struct TaskCard: View {
    let task: ChoreTask
    let isParent: Bool
    let onComplete: () -> Void
    let onArchive: () -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack(alignment: .top) {
                VStack(alignment: .leading, spacing: 4) {
                    Text(task.title)
                        .font(.headline)

                    if let description = task.description {
                        Text(description)
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                            .lineLimit(2)
                    }
                }

                Spacer()

                if task.isRecurring {
                    Label(task.recurrenceDisplay, systemImage: "repeat")
                        .font(.caption)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 4)
                        .background(Color.blue.opacity(0.2))
                        .clipShape(Capsule())
                }
            }

            HStack {
                Text(task.formattedReward)
                    .font(.title2)
                    .fontWeight(.bold)
                    .foregroundStyle(Color.green500)

                Spacer()

                if task.pendingApprovals > 0 {
                    Label("\(task.pendingApprovals) pending", systemImage: "clock")
                        .font(.caption)
                        .foregroundStyle(.orange)
                }

                Text("\(task.totalCompletions) completed")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            HStack(spacing: 8) {
                Button {
                    onComplete()
                } label: {
                    Label("Complete", systemImage: "checkmark.circle")
                        .frame(maxWidth: .infinity)
                }
                .buttonStyle(.borderedProminent)
                .tint(Color.green500)

                if isParent {
                    Button(role: .destructive) {
                        onArchive()
                    } label: {
                        Image(systemName: "archivebox")
                    }
                    .buttonStyle(.bordered)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }
}

// MARK: - Pending Approval Card

struct PendingApprovalCard: View {
    let completion: TaskCompletion
    let onApprove: () -> Void
    let onReject: (String?) -> Void

    @State private var showingRejectDialog = false
    @State private var rejectionReason = ""

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Text(completion.taskTitle)
                    .font(.subheadline)
                    .fontWeight(.semibold)

                Spacer()

                Text(completion.formattedReward)
                    .font(.subheadline)
                    .fontWeight(.bold)
                    .foregroundStyle(Color.green500)
            }

            Text("Completed: \(completion.formattedCompletedDate)")
                .font(.caption)
                .foregroundStyle(.secondary)

            if let notes = completion.notes {
                Text("Notes: \(notes)")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            if let photoUrl = completion.photoUrl {
                AsyncImage(url: URL(string: photoUrl)) { image in
                    image
                        .resizable()
                        .scaledToFill()
                        .frame(height: 100)
                        .clipShape(RoundedRectangle(cornerRadius: 8))
                } placeholder: {
                    Rectangle()
                        .fill(Color.gray.opacity(0.3))
                        .frame(height: 100)
                        .clipShape(RoundedRectangle(cornerRadius: 8))
                }
            }

            HStack(spacing: 8) {
                Button {
                    onApprove()
                } label: {
                    Text("Approve")
                        .frame(maxWidth: .infinity)
                }
                .buttonStyle(.borderedProminent)
                .tint(Color.green500)

                Button(role: .destructive) {
                    showingRejectDialog = true
                } label: {
                    Text("Reject")
                        .frame(maxWidth: .infinity)
                }
                .buttonStyle(.bordered)
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 8))
        .alert("Reject Completion", isPresented: $showingRejectDialog) {
            TextField("Reason (optional)", text: $rejectionReason)
            Button("Cancel", role: .cancel) {}
            Button("Reject", role: .destructive) {
                onReject(rejectionReason.isEmpty ? nil : rejectionReason)
                rejectionReason = ""
            }
        } message: {
            Text("Are you sure you want to reject this completion?")
        }
    }
}

// MARK: - Preview

#Preview("Tasks View") {
    NavigationStack {
        TasksView(childId: UUID(), isParent: true)
    }
}
