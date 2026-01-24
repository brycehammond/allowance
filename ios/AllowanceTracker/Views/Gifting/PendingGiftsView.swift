import SwiftUI

/// View displaying pending gifts for a child (parent only)
@MainActor
struct PendingGiftsView: View {

    // MARK: - Properties

    @State private var viewModel: PendingGiftsViewModel
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @State private var giftToApprove: GiftDto?
    @State private var giftToReject: GiftDto?

    private let childName: String

    // MARK: - Initialization

    init(childId: UUID, childName: String, apiService: APIServiceProtocol = ServiceProvider.apiService) {
        self.childName = childName
        _viewModel = State(wrappedValue: PendingGiftsViewModel(childId: childId, apiService: apiService))
    }

    // MARK: - Computed Properties

    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    // MARK: - Body

    var body: some View {
        ZStack {
            if viewModel.isLoading && viewModel.gifts.isEmpty {
                ProgressView("Loading gifts...")
            } else if viewModel.filteredGifts.isEmpty {
                emptyStateView
            } else {
                giftsListView
            }
        }
        .navigationTitle("Pending Gifts")
        .toolbar {
            ToolbarItem(placement: .navigationBarTrailing) {
                Picker("Filter", selection: $viewModel.showPendingOnly) {
                    Text("Pending").tag(true)
                    Text("All").tag(false)
                }
                .pickerStyle(.segmented)
                .fixedSize()
            }
        }
        .sheet(item: $giftToApprove) { gift in
            GiftApprovalSheet(viewModel: viewModel, gift: gift)
        }
        .sheet(item: $giftToReject) { gift in
            GiftRejectSheet(viewModel: viewModel, gift: gift)
        }
        .refreshable {
            await viewModel.refresh()
        }
        .alert("Error", isPresented: .constant(viewModel.errorMessage != nil)) {
            Button("OK") { viewModel.clearError() }
        } message: {
            if let error = viewModel.errorMessage {
                Text(error)
            }
        }
        .task {
            await viewModel.loadGifts()
        }
    }

    // MARK: - Subviews

    private var giftsListView: some View {
        ScrollView {
            VStack(spacing: 16) {
                // Summary for pending
                if viewModel.pendingCount > 0 {
                    summaryCard
                }

                // Gifts
                ForEach(viewModel.filteredGifts) { gift in
                    giftCard(gift)
                }
            }
            .padding(.horizontal, isRegularWidth ? 24 : 16)
            .padding(.vertical)
            .frame(maxWidth: isRegularWidth ? 700 : .infinity)
            .frame(maxWidth: .infinity)
        }
    }

    private var summaryCard: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text("\(viewModel.pendingCount) Pending Gift\(viewModel.pendingCount == 1 ? "" : "s")")
                    .font(.headline)
                Text(viewModel.formattedTotalPending)
                    .font(.title2)
                    .fontWeight(.bold)
                    .foregroundStyle(.purple)
            }
            Spacer()
            Image(systemName: "gift.fill")
                .font(.largeTitle)
                .foregroundStyle(.purple.opacity(0.3))
        }
        .padding()
        .background(Color.purple.opacity(0.1))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    private func giftCard(_ gift: GiftDto) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text(gift.formattedAmount)
                        .font(.title2)
                        .fontWeight(.bold)

                    HStack {
                        Text("From: \(gift.giverName)")
                        if let relationship = gift.giverRelationship {
                            Text("(\(relationship))")
                                .foregroundStyle(.secondary)
                        }
                    }
                    .font(.subheadline)
                }

                Spacer()

                statusBadge(gift.status)
            }

            HStack {
                Label(gift.occasionDisplay, systemImage: gift.occasion.systemImage)
                    .font(.caption)
                    .foregroundStyle(.secondary)

                Spacer()

                Text(gift.formattedDate)
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            if let message = gift.message {
                Text("\"\(message)\"")
                    .font(.subheadline)
                    .italic()
                    .foregroundStyle(.secondary)
            }

            // Approval info
            if gift.status == .Approved {
                if let goalName = gift.allocatedToGoalName {
                    Label("Allocated to: \(goalName)", systemImage: "target")
                        .font(.caption)
                        .foregroundStyle(.green)
                }
                if gift.hasThankYouNote {
                    Label("Thank you note sent", systemImage: "heart.fill")
                        .font(.caption)
                        .foregroundStyle(.pink)
                }
            }

            if gift.status == .Rejected, let reason = gift.rejectionReason {
                Label("Reason: \(reason)", systemImage: "xmark.circle")
                    .font(.caption)
                    .foregroundStyle(.red)
            }

            // Action buttons for pending
            if gift.status == .Pending {
                Divider()
                HStack(spacing: 12) {
                    Button {
                        giftToApprove = gift
                    } label: {
                        Label("Approve", systemImage: "checkmark.circle.fill")
                            .frame(maxWidth: .infinity)
                    }
                    .buttonStyle(.borderedProminent)
                    .tint(.green)

                    Button(role: .destructive) {
                        giftToReject = gift
                    } label: {
                        Label("Reject", systemImage: "xmark.circle.fill")
                            .frame(maxWidth: .infinity)
                    }
                    .buttonStyle(.bordered)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(gift.status == .Pending ? Color.yellow.opacity(0.5) : Color.clear, lineWidth: 2)
        )
    }

    private func statusBadge(_ status: GiftStatus) -> some View {
        HStack(spacing: 4) {
            Image(systemName: status.systemImage)
            Text(status.displayName)
        }
        .font(.caption)
        .padding(.horizontal, 8)
        .padding(.vertical, 4)
        .background(statusColor(status).opacity(0.2))
        .foregroundStyle(statusColor(status))
        .clipShape(Capsule())
    }

    private func statusColor(_ status: GiftStatus) -> Color {
        switch status {
        case .Pending: return .yellow
        case .Approved: return .green
        case .Rejected: return .red
        case .Expired: return .gray
        }
    }

    private var emptyStateView: some View {
        VStack(spacing: 24) {
            Image(systemName: "gift.fill")
                .font(.system(size: 60))
                .foregroundStyle(.secondary)

            VStack(spacing: 8) {
                Text(viewModel.showPendingOnly ? "No Pending Gifts" : "No Gifts Yet")
                    .font(.title2)
                    .fontWeight(.bold)

                Text(viewModel.showPendingOnly
                     ? "All gifts have been reviewed!"
                     : "Share a gift link with family members to receive gifts for \(childName).")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }
        }
        .frame(maxWidth: isRegularWidth ? 500 : .infinity)
        .padding()
    }
}

// MARK: - Gift Approval Sheet

private struct GiftApprovalSheet: View {
    let viewModel: PendingGiftsViewModel
    let gift: GiftDto
    @Environment(\.dismiss) private var dismiss

    @State private var allocateToGoal = false
    @State private var selectedGoalId: UUID?
    @State private var useSavingsPercentage = false
    @State private var savingsPercentage = 20

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    LabeledContent("Amount", value: gift.formattedAmount)
                    LabeledContent("From", value: gift.giverName)
                    LabeledContent("Occasion", value: gift.occasionDisplay)
                }

                if !viewModel.savingsGoals.isEmpty {
                    Section {
                        Toggle("Allocate to Savings Goal", isOn: $allocateToGoal)
                        if allocateToGoal {
                            Picker("Goal", selection: $selectedGoalId) {
                                Text("Select a goal").tag(nil as UUID?)
                                ForEach(viewModel.savingsGoals) { goal in
                                    Text("\(goal.name) (\(Int(goal.progressPercentage))%)")
                                        .tag(goal.id as UUID?)
                                }
                            }
                        }
                    } footer: {
                        Text("The entire gift amount will be added to the selected savings goal.")
                    }
                }

                if !allocateToGoal {
                    Section {
                        Toggle("Split to Savings", isOn: $useSavingsPercentage)
                        if useSavingsPercentage {
                            Stepper("\(savingsPercentage)% to savings", value: $savingsPercentage, in: 0...100, step: 5)
                        }
                    } footer: {
                        if useSavingsPercentage {
                            let savingsAmount = gift.amount * Decimal(savingsPercentage) / 100
                            let spendingAmount = gift.amount - savingsAmount
                            Text("\(spendingAmount.currencyFormatted) to spending, \(savingsAmount.currencyFormatted) to savings")
                        }
                    }
                }
            }
            .navigationTitle("Approve Gift")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Approve") {
                        Task { await approve() }
                    }
                    .disabled(viewModel.isProcessing)
                }
            }
        }
    }

    private func approve() async {
        let success = await viewModel.approveGift(
            id: gift.id,
            allocateToGoalId: allocateToGoal ? selectedGoalId : nil,
            savingsPercentage: (!allocateToGoal && useSavingsPercentage) ? savingsPercentage : nil
        )
        if success {
            dismiss()
        }
    }
}

// MARK: - Gift Reject Sheet

private struct GiftRejectSheet: View {
    let viewModel: PendingGiftsViewModel
    let gift: GiftDto
    @Environment(\.dismiss) private var dismiss

    @State private var reason = ""

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    LabeledContent("Amount", value: gift.formattedAmount)
                    LabeledContent("From", value: gift.giverName)
                }

                Section("Reason (Optional)") {
                    TextField("Why is this gift being rejected?", text: $reason, axis: .vertical)
                        .lineLimit(3...5)
                }
            }
            .navigationTitle("Reject Gift")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Reject", role: .destructive) {
                        Task { await reject() }
                    }
                    .disabled(viewModel.isProcessing)
                }
            }
        }
    }

    private func reject() async {
        let success = await viewModel.rejectGift(id: gift.id, reason: reason.isEmpty ? nil : reason)
        if success {
            dismiss()
        }
    }
}

// MARK: - Preview

#Preview {
    NavigationStack {
        PendingGiftsView(childId: UUID(), childName: "Emma")
    }
}
