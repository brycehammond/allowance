import SwiftUI

/// View for browsing and purchasing rewards with points
@MainActor
struct RewardShopView: View {

    // MARK: - Properties

    @State private var viewModel: RewardShopViewModel
    @Environment(AuthViewModel.self) private var authViewModel
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @State private var selectedReward: RewardDto?
    @State private var showUnlockConfirmation = false

    // MARK: - Computed Properties

    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    private var gridColumns: [GridItem] {
        let count = isRegularWidth ? 4 : 2
        return Array(repeating: GridItem(.flexible(), spacing: 16), count: count)
    }

    // MARK: - Initialization

    init(childId: UUID, apiService: APIServiceProtocol = APIService()) {
        _viewModel = State(wrappedValue: RewardShopViewModel(childId: childId, apiService: apiService))
    }

    // MARK: - Body

    var body: some View {
        ZStack {
            if viewModel.isLoading && viewModel.availableRewards.isEmpty {
                ProgressView("Loading rewards...")
            } else if viewModel.availableRewards.isEmpty {
                emptyStateView
            } else {
                rewardsContentView
            }
        }
        .navigationTitle("Reward Shop")
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
        .alert("Success", isPresented: .constant(viewModel.successMessage != nil)) {
            Button("OK") {
                viewModel.clearSuccess()
            }
        } message: {
            if let successMessage = viewModel.successMessage {
                Text(successMessage)
            }
        }
        .confirmationDialog(
            "Unlock Reward",
            isPresented: $showUnlockConfirmation,
            presenting: selectedReward
        ) { reward in
            Button("Unlock for \(reward.pointsCost) points") {
                Task {
                    await viewModel.unlockReward(rewardId: reward.id)
                }
            }
            Button("Cancel", role: .cancel) {}
        } message: { reward in
            Text("Spend \(reward.pointsCost) points to unlock \(reward.name)?")
        }
        .task {
            await viewModel.loadRewards()
        }
    }

    // MARK: - Subviews

    /// Main content with rewards
    private var rewardsContentView: some View {
        ScrollView {
            VStack(spacing: 24) {
                // Points balance card
                pointsBalanceCard
                    .padding(.horizontal, isRegularWidth ? 24 : 16)
                    .padding(.top)

                // Type filter
                typeFilterPicker
                    .padding(.horizontal, isRegularWidth ? 24 : 16)

                // My Rewards section (if any unlocked)
                if !viewModel.unlockedRewards.isEmpty {
                    myRewardsSection
                }

                // Available rewards
                availableRewardsSection
            }
            .padding(.bottom)
            .frame(maxWidth: isRegularWidth ? 900 : .infinity)
            .frame(maxWidth: .infinity)
        }
    }

    /// Points balance card
    private var pointsBalanceCard: some View {
        VStack(spacing: 12) {
            HStack {
                Label("Your Points", systemImage: "star.circle.fill")
                    .font(.headline)
                    .foregroundStyle(Color.amber500)

                Spacer()
            }

            Divider()

            HStack(spacing: 24) {
                // Available points
                VStack(spacing: 4) {
                    HStack(spacing: 4) {
                        Image(systemName: "star.fill")
                            .foregroundStyle(Color.amber500)
                        Text("\(viewModel.availablePoints)")
                            .font(.title)
                            .fontWeight(.bold)
                    }
                    Text("Available")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Divider()
                    .frame(height: 40)

                // Total points
                VStack(spacing: 4) {
                    Text("\(viewModel.totalPoints)")
                        .font(.title2)
                        .fontWeight(.semibold)
                        .foregroundStyle(.secondary)
                    Text("Total Earned")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Spacer()
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }

    /// Type filter picker
    private var typeFilterPicker: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                // All types
                typeButton(nil, label: "All")

                // Individual types
                ForEach(RewardType.allCases, id: \.self) { type in
                    typeButton(type, label: type.displayName)
                }
            }
            .padding(.horizontal, 4)
        }
    }

    private func typeButton(_ type: RewardType?, label: String) -> some View {
        let isSelected = viewModel.selectedType == type

        return Button {
            withAnimation(.easeInOut(duration: 0.2)) {
                Task {
                    await viewModel.loadRewards(type: type)
                }
            }
        } label: {
            HStack(spacing: 4) {
                if let type = type {
                    Image(systemName: type.systemImage)
                        .font(.caption)
                }
                Text(label)
                    .font(.subheadline)
                    .fontWeight(isSelected ? .semibold : .regular)
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 8)
            .background(isSelected ? Color.purple : Color(.systemGray5))
            .foregroundStyle(isSelected ? .white : .primary)
            .clipShape(Capsule())
        }
        .buttonStyle(.plain)
    }

    /// My Rewards section
    private var myRewardsSection: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Text("My Rewards")
                    .font(.headline)

                Spacer()

                Text("\(viewModel.unlockedRewards.count)")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }
            .padding(.horizontal, isRegularWidth ? 24 : 16)

            ScrollView(.horizontal, showsIndicators: false) {
                HStack(spacing: 12) {
                    ForEach(viewModel.unlockedRewards) { reward in
                        UnlockedRewardCard(
                            reward: reward,
                            onEquip: {
                                Task {
                                    await viewModel.equipReward(rewardId: reward.id)
                                }
                            },
                            onUnequip: {
                                Task {
                                    await viewModel.unequipReward(rewardId: reward.id)
                                }
                            }
                        )
                    }
                }
                .padding(.horizontal, isRegularWidth ? 24 : 16)
            }
        }
    }

    /// Available rewards section
    private var availableRewardsSection: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Text("Available Rewards")
                    .font(.headline)

                Spacer()

                Text("\(viewModel.filteredRewards.count)")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }
            .padding(.horizontal, isRegularWidth ? 24 : 16)

            LazyVGrid(columns: gridColumns, spacing: 16) {
                ForEach(viewModel.filteredRewards) { reward in
                    RewardCard(
                        reward: reward,
                        onUnlock: {
                            selectedReward = reward
                            showUnlockConfirmation = true
                        }
                    )
                }
            }
            .padding(.horizontal, isRegularWidth ? 24 : 16)
        }
    }

    /// Empty state view
    private var emptyStateView: some View {
        VStack(spacing: 24) {
            Image(systemName: "gift")
                .font(.system(size: 60))
                .foregroundStyle(.secondary)

            VStack(spacing: 8) {
                Text("No Rewards Available")
                    .font(.title2)
                    .fontWeight(.bold)

                Text("Check back later for new rewards to unlock!")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }
        }
        .frame(maxWidth: isRegularWidth ? 500 : .infinity)
        .padding()
    }
}

// MARK: - Reward Card

struct RewardCard: View {
    let reward: RewardDto
    let onUnlock: () -> Void

    var body: some View {
        VStack(spacing: 8) {
            // Preview image or icon
            ZStack {
                if let previewUrl = reward.previewUrl, !previewUrl.isEmpty {
                    AsyncImage(url: URL(string: previewUrl)) { image in
                        image
                            .resizable()
                            .aspectRatio(contentMode: .fit)
                    } placeholder: {
                        Image(systemName: reward.type.systemImage)
                            .font(.title)
                            .foregroundStyle(.secondary)
                    }
                } else {
                    Image(systemName: reward.type.systemImage)
                        .font(.title)
                        .foregroundStyle(typeColor)
                }
            }
            .frame(height: 60)

            // Name
            Text(reward.name)
                .font(.subheadline)
                .fontWeight(.medium)
                .lineLimit(1)

            // Description
            Text(reward.description)
                .font(.caption)
                .foregroundStyle(.secondary)
                .lineLimit(2)
                .multilineTextAlignment(.center)

            Spacer()

            // Price or status
            if reward.isUnlocked {
                Label("Unlocked", systemImage: "checkmark.circle.fill")
                    .font(.caption)
                    .foregroundStyle(.green)
            } else {
                Button(action: onUnlock) {
                    HStack(spacing: 4) {
                        Image(systemName: "star.fill")
                            .font(.caption2)
                        Text("\(reward.pointsCost)")
                            .font(.caption)
                            .fontWeight(.semibold)
                    }
                    .padding(.horizontal, 12)
                    .padding(.vertical, 6)
                    .background(reward.canAfford ? Color.purple : Color(.systemGray4))
                    .foregroundStyle(.white)
                    .clipShape(Capsule())
                }
                .disabled(!reward.canAfford)
            }
        }
        .padding()
        .frame(maxWidth: .infinity)
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }

    private var typeColor: Color {
        switch reward.type {
        case .Avatar: return .blue
        case .Theme: return .purple
        case .Title: return .orange
        case .Special: return .pink
        }
    }
}

// MARK: - Unlocked Reward Card

struct UnlockedRewardCard: View {
    let reward: RewardDto
    let onEquip: () -> Void
    let onUnequip: () -> Void

    var body: some View {
        VStack(spacing: 8) {
            // Preview
            ZStack {
                if let previewUrl = reward.previewUrl, !previewUrl.isEmpty {
                    AsyncImage(url: URL(string: previewUrl)) { image in
                        image
                            .resizable()
                            .aspectRatio(contentMode: .fit)
                    } placeholder: {
                        Image(systemName: reward.type.systemImage)
                            .font(.title2)
                    }
                } else {
                    Image(systemName: reward.type.systemImage)
                        .font(.title2)
                        .foregroundStyle(typeColor)
                }
            }
            .frame(width: 50, height: 50)

            Text(reward.name)
                .font(.caption)
                .fontWeight(.medium)
                .lineLimit(1)

            // Equip/Unequip button
            Button(action: reward.isEquipped ? onUnequip : onEquip) {
                Text(reward.isEquipped ? "Equipped" : "Equip")
                    .font(.caption2)
                    .fontWeight(.semibold)
                    .padding(.horizontal, 8)
                    .padding(.vertical, 4)
                    .background(reward.isEquipped ? Color.green : Color(.systemGray5))
                    .foregroundStyle(reward.isEquipped ? .white : .primary)
                    .clipShape(Capsule())
            }
        }
        .padding()
        .frame(width: 100)
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
        .overlay {
            if reward.isEquipped {
                RoundedRectangle(cornerRadius: 12)
                    .stroke(Color.green, lineWidth: 2)
            }
        }
    }

    private var typeColor: Color {
        switch reward.type {
        case .Avatar: return .blue
        case .Theme: return .purple
        case .Title: return .orange
        case .Special: return .pink
        }
    }
}

// MARK: - Preview

#Preview("Reward Shop") {
    NavigationStack {
        RewardShopView(childId: UUID())
    }
}
