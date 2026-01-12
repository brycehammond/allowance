import SwiftUI

/// View displaying savings goals for a child
@MainActor
struct SavingsGoalsView: View {

    // MARK: - Properties

    @State private var viewModel: SavingsGoalViewModel
    @Environment(AuthViewModel.self) private var authViewModel
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @State private var showingAddGoal = false
    @State private var selectedGoalForDetail: SavingsGoalDto?
    @State private var includeCompleted = false

    // MARK: - Computed Properties

    private var isParent: Bool {
        authViewModel.effectiveIsParent
    }

    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    // MARK: - Initialization

    init(childId: UUID, isParent: Bool, currentBalance: Decimal, apiService: APIServiceProtocol = APIService()) {
        _viewModel = State(wrappedValue: SavingsGoalViewModel(
            childId: childId,
            isParent: isParent,
            currentBalance: currentBalance,
            apiService: apiService
        ))
    }

    // MARK: - Body

    var body: some View {
        ZStack {
            if viewModel.isLoading && viewModel.goals.isEmpty {
                ProgressView("Loading goals...")
            } else if viewModel.goals.isEmpty {
                emptyStateView
            } else {
                goalsListView
            }
        }
        .navigationTitle("Savings Goals")
        .toolbar {
            ToolbarItem(placement: .navigationBarTrailing) {
                Button {
                    showingAddGoal = true
                } label: {
                    Image(systemName: "plus.circle.fill")
                }
            }
            ToolbarItem(placement: .navigationBarTrailing) {
                Menu {
                    Toggle("Show Completed", isOn: $includeCompleted)
                } label: {
                    Image(systemName: "line.3.horizontal.decrease.circle")
                }
            }
        }
        .onChange(of: includeCompleted) { _, newValue in
            Task {
                await viewModel.loadGoals(includeCompleted: newValue)
            }
        }
        .sheet(isPresented: $showingAddGoal) {
            AddGoalView(viewModel: viewModel)
        }
        .sheet(item: $selectedGoalForDetail) { goal in
            GoalDetailView(viewModel: viewModel, goalId: goal.id)
        }
        .refreshable {
            await viewModel.loadGoals(includeCompleted: includeCompleted)
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
        .alert("Success", isPresented: .constant(viewModel.successMessage != nil)) {
            Button("OK") {
                viewModel.clearSuccess()
            }
        } message: {
            if let successMessage = viewModel.successMessage {
                Text(successMessage)
            }
        }
        .task {
            await viewModel.loadGoals(includeCompleted: includeCompleted)
        }
    }

    // MARK: - Subviews

    private var goalsListView: some View {
        ScrollView {
            VStack(spacing: 16) {
                // Summary section
                if viewModel.hasActiveGoals {
                    summarySection
                        .padding(.horizontal, isRegularWidth ? 24 : 16)
                }

                // Goals list
                VStack(spacing: 8) {
                    ForEach(viewModel.goals) { goal in
                        GoalCard(goal: goal, isParent: isParent) {
                            selectedGoalForDetail = goal
                        }
                        .padding(.horizontal, isRegularWidth ? 24 : 16)
                    }
                }
            }
            .padding(.vertical)
            .frame(maxWidth: isRegularWidth ? 700 : .infinity)
            .frame(maxWidth: .infinity)
        }
    }

    private var summarySection: some View {
        VStack(spacing: 12) {
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Total Saved")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text(viewModel.totalSaved.currencyFormatted)
                        .font(.title2)
                        .fontWeight(.bold)
                }

                Spacer()

                VStack(alignment: .trailing, spacing: 4) {
                    Text("of \(viewModel.totalTarget.currencyFormatted)")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text("\(Int(viewModel.overallProgress))%")
                        .font(.title2)
                        .fontWeight(.bold)
                        .foregroundStyle(Color.green500)
                }
            }

            ProgressView(value: viewModel.overallProgress / 100)
                .tint(Color.green500)
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }

    private var emptyStateView: some View {
        VStack(spacing: 24) {
            Image(systemName: "target")
                .font(.system(size: 60))
                .foregroundStyle(.secondary)

            VStack(spacing: 8) {
                Text("No Savings Goals Yet")
                    .font(.title2)
                    .fontWeight(.bold)

                Text("Create your first savings goal and start saving towards something special!")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }

            Button {
                showingAddGoal = true
            } label: {
                Label("Create Goal", systemImage: "plus.circle.fill")
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
        .frame(maxWidth: isRegularWidth ? 500 : .infinity)
        .padding()
    }
}

// MARK: - Goal Card

struct GoalCard: View {
    let goal: SavingsGoalDto
    let isParent: Bool
    let onTap: () -> Void

    var body: some View {
        Button(action: onTap) {
            VStack(alignment: .leading, spacing: 12) {
                // Header
                HStack(alignment: .top) {
                    VStack(alignment: .leading, spacing: 4) {
                        HStack {
                            Text(goal.category.emoji)
                            Text(goal.name)
                                .font(.headline)
                        }

                        if let description = goal.description {
                            Text(description)
                                .font(.caption)
                                .foregroundStyle(.secondary)
                                .lineLimit(2)
                        }
                    }

                    Spacer()

                    statusBadge
                }

                // Progress section
                VStack(alignment: .leading, spacing: 8) {
                    HStack {
                        Text(goal.formattedCurrentAmount)
                            .font(.title3)
                            .fontWeight(.bold)
                            .foregroundStyle(Color.green500)

                        Text("of \(goal.formattedTargetAmount)")
                            .font(.subheadline)
                            .foregroundStyle(.secondary)

                        Spacer()

                        Text("\(Int(goal.progressPercentage))%")
                            .font(.subheadline)
                            .fontWeight(.semibold)
                    }

                    // Progress bar with milestones
                    GoalProgressBar(progress: goal.progressFraction, milestones: goal.milestones)
                }

                // Features indicators
                HStack(spacing: 12) {
                    if goal.hasActiveChallenge {
                        Label("Challenge", systemImage: "trophy.fill")
                            .font(.caption)
                            .foregroundStyle(.orange)
                    }

                    if goal.hasMatchingRule {
                        Label("Matching", systemImage: "arrow.2.squarepath")
                            .font(.caption)
                            .foregroundStyle(.blue)
                    }

                    Spacer()

                    Image(systemName: "chevron.right")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
            .padding()
            .background(Color(.systemBackground))
            .clipShape(RoundedRectangle(cornerRadius: 12))
            .shadow(radius: 2)
        }
        .buttonStyle(.plain)
    }

    @ViewBuilder
    private var statusBadge: some View {
        let (color, icon) = statusConfig(for: goal.status)
        Label(goal.statusName, systemImage: icon)
            .font(.caption)
            .padding(.horizontal, 8)
            .padding(.vertical, 4)
            .background(color.opacity(0.2))
            .foregroundStyle(color)
            .clipShape(Capsule())
    }

    private func statusConfig(for status: GoalStatus) -> (Color, String) {
        switch status {
        case .Active:
            return (Color.green, "target")
        case .Paused:
            return (Color.orange, "pause.circle")
        case .Completed:
            return (Color.blue, "checkmark.circle.fill")
        case .Purchased:
            return (Color.purple, "bag.fill")
        case .Cancelled:
            return (Color.gray, "xmark.circle")
        }
    }
}

// MARK: - Goal Progress Bar

struct GoalProgressBar: View {
    let progress: Double
    let milestones: [GoalMilestoneDto]

    var body: some View {
        GeometryReader { geometry in
            ZStack(alignment: .leading) {
                // Background
                RoundedRectangle(cornerRadius: 4)
                    .fill(Color.gray.opacity(0.2))
                    .frame(height: 8)

                // Progress
                RoundedRectangle(cornerRadius: 4)
                    .fill(Color.green500)
                    .frame(width: geometry.size.width * CGFloat(min(progress, 1.0)), height: 8)

                // Milestone markers
                ForEach(milestones) { milestone in
                    let position = CGFloat(milestone.percentComplete) / 100.0 * geometry.size.width
                    Circle()
                        .fill(milestone.isAchieved ? Color.green500 : Color.gray.opacity(0.5))
                        .frame(width: 12, height: 12)
                        .offset(x: position - 6)
                }
            }
        }
        .frame(height: 12)
    }
}

// MARK: - Preview

#Preview("Savings Goals View") {
    NavigationStack {
        SavingsGoalsView(childId: UUID(), isParent: true, currentBalance: 100)
    }
}
