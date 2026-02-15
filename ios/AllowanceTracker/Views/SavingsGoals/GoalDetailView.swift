import SwiftUI

/// View displaying details and management options for a savings goal
@MainActor
struct GoalDetailView: View {

    // MARK: - Properties

    @Bindable var viewModel: SavingsGoalViewModel
    let goalId: UUID
    @Environment(\.dismiss) private var dismiss
    @Environment(AuthViewModel.self) private var authViewModel

    @State private var showingContributeSheet = false
    @State private var showingMatchingSheet = false
    @State private var showingChallengeSheet = false
    @State private var showingDeleteConfirm = false
    @State private var showingPurchaseConfirm = false

    // MARK: - Computed Properties

    private var isParent: Bool {
        authViewModel.effectiveIsParent
    }

    private var goal: SavingsGoalDto? {
        viewModel.selectedGoal
    }

    // MARK: - Body

    var body: some View {
        NavigationStack {
            Group {
                if viewModel.isLoading && goal == nil {
                    ProgressView("Loading goal...")
                } else if let goal = goal {
                    goalDetailContent(goal)
                } else {
                    Text("Goal not found")
                        .foregroundStyle(.secondary)
                }
            }
            .navigationTitle(goal?.name ?? "Goal")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Done") {
                        dismiss()
                    }
                }

                if isParent {
                    ToolbarItem(placement: .primaryAction) {
                        Menu {
                            if goal?.status == .Active {
                                Button {
                                    Task {
                                        await viewModel.pauseGoal(goalId: goalId)
                                    }
                                } label: {
                                    Label("Pause Goal", systemImage: "pause.circle")
                                }
                            }

                            if goal?.status == .Paused {
                                Button {
                                    Task {
                                        await viewModel.resumeGoal(goalId: goalId)
                                    }
                                } label: {
                                    Label("Resume Goal", systemImage: "play.circle")
                                }
                            }

                            if goal?.isCompleted == true && goal?.status != .Purchased {
                                Button {
                                    showingPurchaseConfirm = true
                                } label: {
                                    Label("Mark as Purchased", systemImage: "bag.fill")
                                }
                            }

                            Divider()

                            Button(role: .destructive) {
                                showingDeleteConfirm = true
                            } label: {
                                Label("Delete Goal", systemImage: "trash")
                            }
                        } label: {
                            Image(systemName: "ellipsis.circle")
                        }
                    }
                }
            }
            .sheet(isPresented: $showingContributeSheet) {
                ContributeSheet(viewModel: viewModel, goalId: goalId)
            }
            .sheet(isPresented: $showingMatchingSheet) {
                MatchingRuleSheet(viewModel: viewModel, goalId: goalId)
            }
            .sheet(isPresented: $showingChallengeSheet) {
                ChallengeSheet(viewModel: viewModel, goalId: goalId)
            }
            .confirmationDialog("Delete Goal?", isPresented: $showingDeleteConfirm) {
                Button("Delete", role: .destructive) {
                    Task {
                        if await viewModel.deleteGoal(goalId: goalId) {
                            dismiss()
                        }
                    }
                }
            } message: {
                Text("This will delete the goal and return any saved amount to the spending balance.")
            }
            .confirmationDialog("Mark as Purchased?", isPresented: $showingPurchaseConfirm) {
                Button("Mark as Purchased") {
                    Task {
                        await viewModel.markAsPurchased(goalId: goalId, notes: nil)
                    }
                }
            } message: {
                Text("Congratulations! Mark this goal as purchased to celebrate the achievement.")
            }
            .task {
                await viewModel.loadGoalDetails(goalId: goalId)
            }
        }
    }

    // MARK: - Goal Detail Content

    @ViewBuilder
    private func goalDetailContent(_ goal: SavingsGoalDto) -> some View {
        ScrollView {
            VStack(spacing: 20) {
                // Progress card
                progressCard(goal)

                // Contribute button
                if goal.status == .Active {
                    contributeButton
                }

                // Challenge card
                if let challenge = viewModel.activeChallenge {
                    challengeCard(challenge)
                } else if isParent && goal.status == .Active {
                    addChallengeButton
                }

                // Matching rule card
                if let matching = viewModel.matchingRule {
                    matchingCard(matching)
                } else if isParent && goal.status == .Active {
                    addMatchingButton
                }

                // Milestones
                milestonesCard(goal)

                // Recent contributions
                contributionsCard

                // Auto-transfer info
                if goal.autoTransferType != .None {
                    autoTransferCard(goal)
                }
            }
            .padding()
        }
    }

    // MARK: - Progress Card

    private func progressCard(_ goal: SavingsGoalDto) -> some View {
        VStack(spacing: 16) {
            // Category and status
            HStack {
                Label {
                    Text(goal.categoryName)
                } icon: {
                    Text(goal.category.emoji)
                }
                .font(.subheadline)
                .foregroundStyle(.secondary)

                Spacer()

                statusBadge(for: goal.status, name: goal.statusName)
            }

            // Amount progress
            VStack(spacing: 8) {
                HStack(alignment: .bottom) {
                    Text(goal.formattedCurrentAmount)
                        .font(.system(size: 36, weight: .bold))
                        .foregroundStyle(Color.green600)

                    Text("of \(goal.formattedTargetAmount)")
                        .font(.title3)
                        .foregroundStyle(.secondary)
                        .padding(.bottom, 4)

                    Spacer()
                }

                // Progress bar
                ProgressView(value: goal.progressFraction)
                    .tint(Color.green600)
                    .scaleEffect(y: 2, anchor: .center)

                HStack {
                    Text("\(Int(goal.progressPercentage))% complete")
                        .font(.caption)
                        .foregroundStyle(.secondary)

                    Spacer()

                    Text("\(goal.formattedAmountRemaining) remaining")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }

    // MARK: - Contribute Button

    private var contributeButton: some View {
        Button {
            showingContributeSheet = true
        } label: {
            Label("Contribute", systemImage: "plus.circle.fill")
                .fontWeight(.semibold)
                .frame(maxWidth: .infinity)
                .padding()
                .background(Color.green600)
                .foregroundStyle(.white)
                .clipShape(RoundedRectangle(cornerRadius: 12))
        }
    }

    // MARK: - Challenge Card

    private func challengeCard(_ challenge: GoalChallengeDto) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Label("Active Challenge", systemImage: "trophy.fill")
                    .font(.headline)
                    .foregroundStyle(.orange)

                Spacer()

                if isParent {
                    Button("Cancel", role: .destructive) {
                        Task {
                            await viewModel.cancelChallenge(goalId: goalId)
                        }
                    }
                    .font(.caption)
                }
            }

            VStack(alignment: .leading, spacing: 8) {
                HStack {
                    Text("Save \(challenge.formattedTargetAmount)")
                        .font(.subheadline)
                        .fontWeight(.semibold)

                    Spacer()

                    Text("Bonus: \(challenge.formattedBonusAmount)")
                        .font(.subheadline)
                        .foregroundStyle(Color.green600)
                }

                ProgressView(value: challenge.progressFraction)
                    .tint(.orange)

                HStack {
                    Text("\(Int(challenge.progressPercentage))%")
                        .font(.caption)
                        .foregroundStyle(.secondary)

                    Spacer()

                    Text("\(challenge.daysRemaining) days left")
                        .font(.caption)
                        .foregroundStyle(challenge.daysRemaining <= 3 ? .red : .secondary)
                }
            }
        }
        .padding()
        .background(Color.orange.opacity(0.1))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    private var addChallengeButton: some View {
        Button {
            showingChallengeSheet = true
        } label: {
            HStack {
                Image(systemName: "trophy")
                Text("Create Challenge")
            }
            .frame(maxWidth: .infinity)
            .padding()
            .background(Color.orange.opacity(0.1))
            .foregroundStyle(.orange)
            .clipShape(RoundedRectangle(cornerRadius: 12))
        }
    }

    // MARK: - Matching Card

    private func matchingCard(_ matching: MatchingRuleDto) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Label("Parent Matching", systemImage: "arrow.2.squarepath")
                    .font(.headline)
                    .foregroundStyle(.blue)

                Spacer()

                if isParent {
                    Button("Remove", role: .destructive) {
                        Task {
                            await viewModel.deleteMatchingRule(goalId: goalId)
                        }
                    }
                    .font(.caption)
                }
            }

            VStack(alignment: .leading, spacing: 4) {
                Text(matching.matchDescription)
                    .font(.subheadline)

                Text("Total matched: \(matching.totalMatchedAmount.currencyFormatted)")
                    .font(.caption)
                    .foregroundStyle(.secondary)

                if let max = matching.maxMatchAmount {
                    Text("Max match: \(max.currencyFormatted)")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
        }
        .padding()
        .background(Color.blue.opacity(0.1))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    private var addMatchingButton: some View {
        Button {
            showingMatchingSheet = true
        } label: {
            HStack {
                Image(systemName: "arrow.2.squarepath")
                Text("Add Parent Matching")
            }
            .frame(maxWidth: .infinity)
            .padding()
            .background(Color.blue.opacity(0.1))
            .foregroundStyle(.blue)
            .clipShape(RoundedRectangle(cornerRadius: 12))
        }
    }

    // MARK: - Milestones Card

    private func milestonesCard(_ goal: SavingsGoalDto) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Milestones")
                .font(.headline)

            HStack(spacing: 0) {
                ForEach(goal.milestones) { milestone in
                    VStack(spacing: 4) {
                        ZStack {
                            Circle()
                                .fill(milestone.isAchieved ? Color.green600 : Color.gray.opacity(0.3))
                                .frame(width: 32, height: 32)

                            if milestone.isAchieved {
                                Image(systemName: "checkmark")
                                    .font(.caption)
                                    .fontWeight(.bold)
                                    .foregroundStyle(.white)
                            }
                        }

                        Text("\(milestone.percentComplete)%")
                            .font(.caption2)
                            .foregroundStyle(milestone.isAchieved ? Color.green600 : .secondary)
                    }
                    .frame(maxWidth: .infinity)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }

    // MARK: - Contributions Card

    private var contributionsCard: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Recent Activity")
                .font(.headline)

            if viewModel.contributions.isEmpty {
                Text("No contributions yet")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .padding()
            } else {
                ForEach(viewModel.contributions.prefix(5)) { contribution in
                    HStack {
                        Image(systemName: contribution.type.systemImage)
                            .foregroundStyle(contribution.type.isPositive ? Color.green600 : .red)

                        VStack(alignment: .leading, spacing: 2) {
                            Text(contribution.typeName)
                                .font(.subheadline)

                            Text(contribution.formattedDate)
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }

                        Spacer()

                        Text(contribution.formattedAmount)
                            .font(.subheadline)
                            .fontWeight(.semibold)
                            .foregroundStyle(contribution.type.isPositive ? Color.green600 : .red)
                    }
                    .padding(.vertical, 4)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }

    // MARK: - Auto Transfer Card

    private func autoTransferCard(_ goal: SavingsGoalDto) -> some View {
        VStack(alignment: .leading, spacing: 8) {
            Label("Auto Transfer", systemImage: "arrow.right.circle")
                .font(.headline)
                .foregroundStyle(.purple)

            switch goal.autoTransferType {
            case .FixedAmount:
                if let amount = goal.autoTransferAmount {
                    Text("\(amount.currencyFormatted) per allowance")
                        .font(.subheadline)
                }
            case .Percentage:
                if let percentage = goal.autoTransferPercentage {
                    Text("\(NSNumber(value: Double(truncating: percentage as NSNumber)), formatter: percentFormatter)% of each allowance")
                        .font(.subheadline)
                }
            case .None:
                EmptyView()
            }
        }
        .padding()
        .background(Color.purple.opacity(0.1))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    // MARK: - Helper Views

    @ViewBuilder
    private func statusBadge(for status: GoalStatus, name: String) -> some View {
        let color: Color = switch status {
        case .Active: .green
        case .Paused: .orange
        case .Completed: .blue
        case .Purchased: .purple
        case .Cancelled: .gray
        }

        Label(name, systemImage: status.systemImage)
            .font(.caption)
            .padding(.horizontal, 8)
            .padding(.vertical, 4)
            .background(color.opacity(0.2))
            .foregroundStyle(color)
            .clipShape(Capsule())
    }

    private var percentFormatter: NumberFormatter {
        let formatter = NumberFormatter()
        formatter.numberStyle = .decimal
        formatter.maximumFractionDigits = 0
        return formatter
    }
}

// MARK: - Contribute Sheet

struct ContributeSheet: View {
    @Bindable var viewModel: SavingsGoalViewModel
    let goalId: UUID
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Amount", text: $viewModel.contributionAmount)
                        .keyboardType(.decimalPad)

                    TextField("Description (optional)", text: $viewModel.contributionDescription)
                }

                Section {
                    Button {
                        Task {
                            if await viewModel.contribute(goalId: goalId) {
                                dismiss()
                            }
                        }
                    } label: {
                        if viewModel.isProcessing {
                            ProgressView()
                                .frame(maxWidth: .infinity)
                        } else {
                            Text("Contribute")
                                .frame(maxWidth: .infinity)
                        }
                    }
                    .disabled(viewModel.contributionAmount.isEmpty || viewModel.isProcessing)
                }
            }
            .navigationTitle("Contribute to Goal")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        viewModel.clearFormState()
                        dismiss()
                    }
                }
            }
        }
    }
}

// MARK: - Matching Rule Sheet

struct MatchingRuleSheet: View {
    @Bindable var viewModel: SavingsGoalViewModel
    let goalId: UUID
    @Environment(\.dismiss) private var dismiss

    @State private var matchType: MatchingType = .RatioMatch
    @State private var matchRatio = ""
    @State private var maxMatchAmount = ""

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    Picker("Match Type", selection: $matchType) {
                        ForEach(MatchingType.allCases, id: \.self) { type in
                            Text(type.displayName).tag(type)
                        }
                    }

                    TextField("Match Ratio", text: $matchRatio)
                        .keyboardType(.decimalPad)

                    TextField("Max Match Amount (optional)", text: $maxMatchAmount)
                        .keyboardType(.decimalPad)
                } footer: {
                    switch matchType {
                    case .RatioMatch:
                        Text("Parent adds $X for every $1 the child saves")
                    case .PercentageMatch:
                        Text("Parent matches X% of each contribution")
                    case .MilestoneBonus:
                        Text("Parent adds bonus at milestones")
                    }
                }

                Section {
                    Button {
                        Task {
                            await createMatchingRule()
                        }
                    } label: {
                        if viewModel.isProcessing {
                            ProgressView()
                                .frame(maxWidth: .infinity)
                        } else {
                            Text("Create Matching Rule")
                                .frame(maxWidth: .infinity)
                        }
                    }
                    .disabled(matchRatio.isEmpty || viewModel.isProcessing)
                }
            }
            .navigationTitle("Parent Matching")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        dismiss()
                    }
                }
            }
        }
    }

    private func createMatchingRule() async {
        guard let ratio = Decimal(string: matchRatio) else { return }
        let maxAmount = Decimal(string: maxMatchAmount)

        if await viewModel.createMatchingRule(
            goalId: goalId,
            matchType: matchType,
            matchRatio: ratio,
            maxMatchAmount: maxAmount
        ) {
            dismiss()
        }
    }
}

// MARK: - Challenge Sheet

struct ChallengeSheet: View {
    @Bindable var viewModel: SavingsGoalViewModel
    let goalId: UUID
    @Environment(\.dismiss) private var dismiss

    @State private var targetAmount = ""
    @State private var bonusAmount = ""
    @State private var endDate = Date().addingTimeInterval(7 * 24 * 60 * 60) // 1 week from now

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Target Amount", text: $targetAmount)
                        .keyboardType(.decimalPad)

                    DatePicker("End Date", selection: $endDate, in: Date()..., displayedComponents: .date)

                    TextField("Bonus Amount", text: $bonusAmount)
                        .keyboardType(.decimalPad)
                } footer: {
                    Text("Challenge the child to save a specific amount by the deadline to earn a bonus!")
                }

                Section {
                    Button {
                        Task {
                            await createChallenge()
                        }
                    } label: {
                        if viewModel.isProcessing {
                            ProgressView()
                                .frame(maxWidth: .infinity)
                        } else {
                            Text("Create Challenge")
                                .frame(maxWidth: .infinity)
                        }
                    }
                    .disabled(targetAmount.isEmpty || bonusAmount.isEmpty || viewModel.isProcessing)
                }
            }
            .navigationTitle("Create Challenge")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        dismiss()
                    }
                }
            }
        }
    }

    private func createChallenge() async {
        guard let target = Decimal(string: targetAmount),
              let bonus = Decimal(string: bonusAmount) else { return }

        if await viewModel.createChallenge(
            goalId: goalId,
            targetAmount: target,
            endDate: endDate,
            bonusAmount: bonus
        ) {
            dismiss()
        }
    }
}

// MARK: - Preview

#Preview("Goal Detail View") {
    GoalDetailView(
        viewModel: SavingsGoalViewModel(childId: UUID(), isParent: true, currentBalance: 100),
        goalId: UUID()
    )
}
