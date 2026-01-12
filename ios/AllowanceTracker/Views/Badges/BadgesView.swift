import SwiftUI

/// View displaying achievement badges for a child
@MainActor
struct BadgesView: View {

    // MARK: - Properties

    @State private var viewModel: BadgesViewModel
    @Environment(AuthViewModel.self) private var authViewModel
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @State private var selectedCategory: BadgeCategory?

    // MARK: - Computed Properties

    private var isParent: Bool {
        authViewModel.effectiveIsParent
    }

    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    private var gridColumns: [GridItem] {
        let count = isRegularWidth ? 5 : 3
        return Array(repeating: GridItem(.flexible(), spacing: 12), count: count)
    }

    // MARK: - Initialization

    init(childId: UUID, apiService: APIServiceProtocol = APIService()) {
        _viewModel = State(wrappedValue: BadgesViewModel(childId: childId, apiService: apiService))
    }

    // MARK: - Body

    var body: some View {
        ZStack {
            if viewModel.isLoading && viewModel.earnedBadges.isEmpty {
                // Loading state
                ProgressView("Loading badges...")
            } else if viewModel.earnedBadges.isEmpty && viewModel.inProgressBadges.isEmpty {
                // Empty state
                emptyStateView
            } else {
                // Badges content
                badgesContentView
            }
        }
        .navigationTitle("Badges")
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                NavigationLink(destination: RewardShopView(childId: viewModel.childId)) {
                    Label("Reward Shop", systemImage: "gift.fill")
                }
            }
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
            await viewModel.loadBadges()
            // Mark badges as seen when view appears
            await viewModel.markAllNewBadgesAsSeen()
        }
    }

    // MARK: - Subviews

    /// Main content with badges
    private var badgesContentView: some View {
        ScrollView {
            VStack(spacing: 20) {
                // Points summary
                pointsSummaryCard
                    .padding(.horizontal, isRegularWidth ? 24 : 16)
                    .padding(.top)

                // Category filter
                categoryPicker
                    .padding(.horizontal, isRegularWidth ? 24 : 16)

                // Earned badges section
                if !viewModel.filteredBadges.isEmpty {
                    earnedBadgesSection
                }

                // In-progress badges section
                if !viewModel.inProgressBadges.isEmpty && selectedCategory == nil {
                    inProgressSection
                }
            }
            .padding(.bottom)
            .frame(maxWidth: isRegularWidth ? 800 : .infinity)
            .frame(maxWidth: .infinity)
        }
    }

    /// Points summary card
    private var pointsSummaryCard: some View {
        VStack(spacing: 12) {
            HStack {
                Label("Achievement Points", systemImage: "star.circle.fill")
                    .font(.headline)
                    .foregroundStyle(Color.amber500)

                Spacer()

                if viewModel.newBadgeCount > 0 {
                    Text("\(viewModel.newBadgeCount) New!")
                        .font(.caption)
                        .fontWeight(.semibold)
                        .foregroundStyle(.white)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 4)
                        .background(Color.red)
                        .clipShape(Capsule())
                }
            }

            Divider()

            HStack(spacing: 20) {
                // Total points
                VStack(spacing: 4) {
                    HStack(spacing: 4) {
                        Image(systemName: "star.fill")
                            .foregroundStyle(Color.amber500)
                        Text("\(viewModel.totalPoints)")
                            .font(.title2)
                            .fontWeight(.bold)
                    }
                    Text("Total Points")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Divider()
                    .frame(height: 40)

                // Badges earned
                VStack(spacing: 4) {
                    Text("\(viewModel.totalBadgesEarned)")
                        .font(.title2)
                        .fontWeight(.bold)
                        .foregroundStyle(Color.green500)
                    Text("Badges Earned")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Divider()
                    .frame(height: 40)

                // Progress
                VStack(spacing: 4) {
                    Text("\(Int(viewModel.overallProgress * 100))%")
                        .font(.title2)
                        .fontWeight(.bold)
                        .foregroundStyle(Color.blue)
                    Text("Complete")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }

            // Overall progress bar
            if viewModel.totalBadgesAvailable > 0 {
                VStack(spacing: 4) {
                    ProgressView(value: viewModel.overallProgress)
                        .tint(Color.green500)

                    Text("\(viewModel.totalBadgesEarned) of \(viewModel.totalBadgesAvailable) badges unlocked")
                        .font(.caption2)
                        .foregroundStyle(.secondary)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }

    /// Category filter picker
    private var categoryPicker: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                // All category
                categoryButton(nil, label: "All")

                // Individual categories
                ForEach(BadgeCategory.allCases, id: \.self) { category in
                    categoryButton(category, label: category.displayName)
                }
            }
            .padding(.horizontal, 4)
        }
    }

    private func categoryButton(_ category: BadgeCategory?, label: String) -> some View {
        let isSelected = selectedCategory == category

        return Button {
            withAnimation(.easeInOut(duration: 0.2)) {
                selectedCategory = category
                viewModel.selectedCategory = category
            }
        } label: {
            HStack(spacing: 4) {
                if let category = category {
                    Image(systemName: category.systemImage)
                        .font(.caption)
                }
                Text(label)
                    .font(.subheadline)
                    .fontWeight(isSelected ? .semibold : .regular)
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 8)
            .background(isSelected ? Color.green500 : Color(.systemGray5))
            .foregroundStyle(isSelected ? .white : .primary)
            .clipShape(Capsule())
        }
        .buttonStyle(.plain)
    }

    /// Earned badges section
    private var earnedBadgesSection: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Text("Earned Badges")
                    .font(.headline)

                Spacer()

                Text("\(viewModel.filteredBadges.count)")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }
            .padding(.horizontal, isRegularWidth ? 24 : 16)

            LazyVGrid(columns: gridColumns, spacing: 12) {
                ForEach(viewModel.badgesByDate) { badge in
                    BadgeCardView(
                        badge: badge,
                        isParent: isParent,
                        onToggleDisplay: { isDisplayed in
                            Task {
                                await viewModel.toggleBadgeDisplay(
                                    badgeId: badge.badgeId,
                                    isDisplayed: isDisplayed
                                )
                            }
                        }
                    )
                }
            }
            .padding(.horizontal, isRegularWidth ? 24 : 16)
        }
    }

    /// In-progress badges section
    private var inProgressSection: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Text("In Progress")
                    .font(.headline)

                Spacer()

                Text("\(viewModel.inProgressBadges.count)")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }
            .padding(.horizontal, isRegularWidth ? 24 : 16)

            VStack(spacing: 8) {
                ForEach(viewModel.inProgressBadges) { progress in
                    BadgeProgressCard(progress: progress)
                        .padding(.horizontal, isRegularWidth ? 24 : 16)
                }
            }
        }
    }

    /// Empty state
    private var emptyStateView: some View {
        VStack(spacing: 24) {
            Image(systemName: "medal")
                .font(.system(size: 60))
                .foregroundStyle(.secondary)

            VStack(spacing: 8) {
                Text("No Badges Yet")
                    .font(.title2)
                    .fontWeight(.bold)

                Text("Start saving, spending wisely, and completing goals to earn badges!")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }

            // Tips for earning badges
            VStack(alignment: .leading, spacing: 12) {
                tipRow(icon: "banknote", text: "Make regular deposits to savings")
                tipRow(icon: "cart", text: "Track your spending carefully")
                tipRow(icon: "target", text: "Set and achieve savings goals")
                tipRow(icon: "flame", text: "Keep up your saving streaks")
            }
            .padding()
            .background(Color(.systemGray6))
            .clipShape(RoundedRectangle(cornerRadius: 12))
        }
        .frame(maxWidth: isRegularWidth ? 500 : .infinity)
        .padding()
    }

    private func tipRow(icon: String, text: String) -> some View {
        HStack(spacing: 12) {
            Image(systemName: icon)
                .font(.headline)
                .foregroundStyle(Color.green500)
                .frame(width: 24)

            Text(text)
                .font(.subheadline)
                .foregroundStyle(.secondary)

            Spacer()
        }
    }
}

// MARK: - Preview Provider

#Preview("Badges View") {
    NavigationStack {
        BadgesView(childId: UUID())
    }
}
