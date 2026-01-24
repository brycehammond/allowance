import SwiftUI

/// View displaying gift links for a child (parent only)
@MainActor
struct GiftLinksView: View {

    // MARK: - Properties

    @State private var viewModel: GiftLinkViewModel
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @State private var showingCreateLink = false
    @State private var showingStats: GiftLinkDto?
    @State private var linkToDeactivate: GiftLinkDto?
    @State private var linkToRegenerate: GiftLinkDto?
    @State private var copiedLinkId: UUID?

    private let childName: String

    // MARK: - Initialization

    init(childId: UUID, childName: String, apiService: APIServiceProtocol = ServiceProvider.apiService) {
        self.childName = childName
        _viewModel = State(wrappedValue: GiftLinkViewModel(childId: childId, apiService: apiService))
    }

    // MARK: - Computed Properties

    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    // MARK: - Body

    var body: some View {
        ZStack {
            if viewModel.isLoading && viewModel.links.isEmpty {
                ProgressView("Loading gift links...")
            } else if viewModel.links.isEmpty {
                emptyStateView
            } else {
                linksListView
            }
        }
        .navigationTitle("Gift Links")
        .toolbar {
            ToolbarItem(placement: .navigationBarTrailing) {
                Button {
                    showingCreateLink = true
                } label: {
                    Image(systemName: "plus.circle.fill")
                }
            }
        }
        .sheet(isPresented: $showingCreateLink) {
            CreateGiftLinkView(viewModel: viewModel, childName: childName)
        }
        .sheet(item: $showingStats) { link in
            GiftLinkStatsSheet(viewModel: viewModel, link: link)
        }
        .confirmationDialog(
            "Deactivate Link",
            isPresented: .constant(linkToDeactivate != nil),
            presenting: linkToDeactivate
        ) { link in
            Button("Deactivate", role: .destructive) {
                Task {
                    await viewModel.deactivateLink(id: link.id)
                    linkToDeactivate = nil
                }
            }
        } message: { link in
            Text("Are you sure you want to deactivate \"\(link.name)\"? Family members will no longer be able to use this link.")
        }
        .confirmationDialog(
            "Regenerate Token",
            isPresented: .constant(linkToRegenerate != nil),
            presenting: linkToRegenerate
        ) { link in
            Button("Regenerate") {
                Task {
                    await viewModel.regenerateToken(id: link.id)
                    linkToRegenerate = nil
                }
            }
        } message: { _ in
            Text("This will create a new link URL. The old link will stop working and you'll need to share the new one.")
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
            await viewModel.loadGiftLinks()
        }
        .onChange(of: copiedLinkId) { _, newValue in
            if newValue != nil {
                DispatchQueue.main.asyncAfter(deadline: .now() + 2) {
                    copiedLinkId = nil
                }
            }
        }
    }

    // MARK: - Subviews

    private var linksListView: some View {
        ScrollView {
            VStack(spacing: 16) {
                // Active links section
                if !viewModel.activeLinks.isEmpty {
                    sectionHeader("Active Links", count: viewModel.activeLinks.count)
                    ForEach(viewModel.activeLinks) { link in
                        linkCard(link)
                    }
                }

                // Inactive links section
                if !viewModel.inactiveLinks.isEmpty {
                    sectionHeader("Inactive Links", count: viewModel.inactiveLinks.count)
                        .padding(.top, 8)
                    ForEach(viewModel.inactiveLinks) { link in
                        linkCard(link)
                    }
                }
            }
            .padding(.horizontal, isRegularWidth ? 24 : 16)
            .padding(.vertical)
            .frame(maxWidth: isRegularWidth ? 700 : .infinity)
            .frame(maxWidth: .infinity)
        }
    }

    private func sectionHeader(_ title: String, count: Int) -> some View {
        HStack {
            Text(title)
                .font(.headline)
            Text("(\(count))")
                .font(.subheadline)
                .foregroundStyle(.secondary)
            Spacer()
        }
    }

    private func linkCard(_ link: GiftLinkDto) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text(link.name)
                        .font(.headline)
                    Text(link.statusDescription)
                        .font(.caption)
                        .foregroundStyle(link.isActive ? .green : .secondary)
                }

                Spacer()

                // Copy button
                Button {
                    UIPasteboard.general.string = link.portalUrl
                    copiedLinkId = link.id
                } label: {
                    Image(systemName: copiedLinkId == link.id ? "checkmark" : "doc.on.doc")
                        .foregroundStyle(copiedLinkId == link.id ? .green : .blue)
                }
                .buttonStyle(.bordered)
            }

            if let description = link.description {
                Text(description)
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }

            HStack(spacing: 16) {
                Label("\(link.useCount) uses", systemImage: "person.2")
                    .font(.caption)
                    .foregroundStyle(.secondary)

                Label(link.visibility.displayName, systemImage: link.visibility.systemImage)
                    .font(.caption)
                    .foregroundStyle(.secondary)

                if let min = link.formattedMinAmount {
                    Label("Min: \(min)", systemImage: "dollarsign.circle")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }

            Divider()

            HStack(spacing: 12) {
                Button {
                    showingStats = link
                } label: {
                    Label("Stats", systemImage: "chart.bar")
                        .font(.subheadline)
                }

                Spacer()

                if link.isActive {
                    Button {
                        linkToRegenerate = link
                    } label: {
                        Label("Regenerate", systemImage: "arrow.clockwise")
                            .font(.subheadline)
                    }

                    Button(role: .destructive) {
                        linkToDeactivate = link
                    } label: {
                        Label("Deactivate", systemImage: "xmark.circle")
                            .font(.subheadline)
                    }
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }

    private var emptyStateView: some View {
        VStack(spacing: 24) {
            Image(systemName: "link.badge.plus")
                .font(.system(size: 60))
                .foregroundStyle(.purple)

            VStack(spacing: 8) {
                Text("No Gift Links")
                    .font(.title2)
                    .fontWeight(.bold)

                Text("Create a gift link to share with family members so they can send gifts to \(childName).")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }

            Button {
                showingCreateLink = true
            } label: {
                Label("Create Gift Link", systemImage: "plus.circle.fill")
                    .fontWeight(.semibold)
                    .frame(maxWidth: .infinity)
                    .padding()
                    .background(Color.purple)
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

// MARK: - Gift Link Stats Sheet

private struct GiftLinkStatsSheet: View {
    let viewModel: GiftLinkViewModel
    let link: GiftLinkDto
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        NavigationStack {
            VStack(spacing: 24) {
                if let stats = viewModel.selectedLinkStats {
                    statsContent(stats)
                } else {
                    ProgressView()
                }
            }
            .padding()
            .navigationTitle("Link Statistics")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .confirmationAction) {
                    Button("Done") { dismiss() }
                }
            }
            .task {
                await viewModel.loadStats(for: link.id)
            }
        }
    }

    private func statsContent(_ stats: GiftLinkStatsDto) -> some View {
        VStack(spacing: 20) {
            Text(link.name)
                .font(.headline)

            LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], spacing: 16) {
                statCard("Total Gifts", value: "\(stats.totalGifts)", icon: "gift.fill", color: .purple)
                statCard("Total Received", value: stats.formattedTotalAmount, icon: "dollarsign.circle.fill", color: .green)
                statCard("Pending", value: "\(stats.pendingGifts)", icon: "clock.fill", color: .yellow)
                statCard("Approved", value: "\(stats.approvedGifts)", icon: "checkmark.circle.fill", color: .green)
            }

            if let avgAmount = stats.formattedAverageAmount {
                HStack {
                    Text("Average Gift")
                        .foregroundStyle(.secondary)
                    Spacer()
                    Text(avgAmount)
                        .fontWeight(.semibold)
                }
                .padding()
                .background(Color(.secondarySystemBackground))
                .clipShape(RoundedRectangle(cornerRadius: 8))
            }

            Spacer()
        }
    }

    private func statCard(_ title: String, value: String, icon: String, color: Color) -> some View {
        VStack(spacing: 8) {
            Image(systemName: icon)
                .font(.title2)
                .foregroundStyle(color)
            Text(value)
                .font(.title3)
                .fontWeight(.bold)
            Text(title)
                .font(.caption)
                .foregroundStyle(.secondary)
        }
        .frame(maxWidth: .infinity)
        .padding()
        .background(Color(.secondarySystemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }
}

// MARK: - Preview

#Preview {
    NavigationStack {
        GiftLinksView(childId: UUID(), childName: "Emma")
    }
}
