import SwiftUI

/// View displaying wish list items for a child
@MainActor
struct WishListView: View {

    // MARK: - Properties

    @State private var viewModel: WishListViewModel
    @Environment(AuthViewModel.self) private var authViewModel
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @State private var showingAddItem = false
    @State private var editingItem: WishListItem?
    @State private var deletingItem: WishListItem?
    @State private var selectedFilter: WishListFilter = .active

    // MARK: - Computed Properties

    private var isParent: Bool {
        authViewModel.effectiveIsParent
    }

    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    // MARK: - Filter enum

    private enum WishListFilter: String, CaseIterable {
        case active = "Active"
        case purchased = "Purchased"
        case all = "All"
    }

    // MARK: - Initialization

    init(childId: UUID, apiService: APIServiceProtocol = APIService()) {
        _viewModel = State(wrappedValue: WishListViewModel(childId: childId, apiService: apiService))
    }

    // MARK: - Body

    var body: some View {
        ZStack {
            if viewModel.isLoading && viewModel.items.isEmpty {
                // Loading state
                ProgressView("Loading wish list...")
            } else if filteredItems.isEmpty {
                // Empty state
                emptyStateView
            } else {
                // Wish list items
                wishListView
            }
        }
        .navigationTitle("Wish List")
        .toolbar {
            ToolbarItem(placement: .navigationBarTrailing) {
                Button {
                    showingAddItem = true
                } label: {
                    Image(systemName: "plus.circle.fill")
                }
            }
        }
        .sheet(isPresented: $showingAddItem) {
            AddWishListItemView(viewModel: viewModel)
        }
        .sheet(item: $editingItem) { item in
            EditWishListItemView(viewModel: viewModel, item: item)
        }
        .confirmationDialog(
            "Delete Item",
            isPresented: .constant(deletingItem != nil),
            presenting: deletingItem
        ) { item in
            Button("Delete", role: .destructive) {
                Task {
                    await deleteItem(item)
                }
            }
        } message: { item in
            Text("Are you sure you want to delete \"\(item.name)\"?")
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
            await viewModel.loadWishList()
            await viewModel.loadBalance()
        }
    }

    // MARK: - Subviews

    /// Wish list with items
    private var wishListView: some View {
        ScrollView {
            VStack(spacing: 16) {
                // Filter picker
                Picker("Filter", selection: $selectedFilter) {
                    ForEach(WishListFilter.allCases, id: \.self) { filter in
                        Text(filter.rawValue).tag(filter)
                    }
                }
                .pickerStyle(.segmented)
                .padding(.horizontal, isRegularWidth ? 24 : 16)
                .padding(.top)

                // Summary card (for active items)
                if selectedFilter == .active {
                    summaryCard
                        .padding(.horizontal, isRegularWidth ? 24 : 16)
                }

                // Items
                VStack(spacing: 8) {
                    ForEach(filteredItems) { item in
                        WishListItemCard(
                            item: item,
                            currentBalance: viewModel.currentBalance,
                            isParent: isParent,
                            onEdit: {
                                editingItem = item
                            },
                            onDelete: {
                                deletingItem = item
                            },
                            onMarkPurchased: {
                                Task {
                                    await markAsPurchased(item)
                                }
                            }
                        )
                        .padding(.horizontal, isRegularWidth ? 24 : 16)
                    }
                }
            }
            .padding(.bottom)
            .frame(maxWidth: isRegularWidth ? 700 : .infinity)
            .frame(maxWidth: .infinity)
        }
    }

    /// Summary card with statistics
    private var summaryCard: some View {
        VStack(spacing: 12) {
            HStack {
                Label("Wish List Summary", systemImage: "star.fill")
                    .font(.headline)
                    .foregroundStyle(Color.amber500)

                Spacer()
            }

            Divider()

            HStack(spacing: 24) {
                VStack(spacing: 4) {
                    Text("\(viewModel.activeItems.count)")
                        .font(.title2)
                        .fontWeight(.bold)
                    Text("Active Items")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Divider()
                    .frame(height: 40)

                VStack(spacing: 4) {
                    Text("\(viewModel.affordableItems.count)")
                        .font(.title2)
                        .fontWeight(.bold)
                        .foregroundStyle(Color.green500)
                    Text("Can Afford")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Divider()
                    .frame(height: 40)

                VStack(spacing: 4) {
                    Text(viewModel.currentBalance.currencyFormatted)
                        .font(.title3)
                        .fontWeight(.bold)
                        .fontDesign(.monospaced)
                    Text("Balance")
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

    /// Empty state
    private var emptyStateView: some View {
        VStack(spacing: 24) {
            Image(systemName: "star.slash")
                .font(.system(size: 60))
                .foregroundStyle(.secondary)

            VStack(spacing: 8) {
                Text(emptyStateTitle)
                    .font(.title2)
                    .fontWeight(.bold)

                Text(emptyStateMessage)
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }

            if selectedFilter == .active {
                Button {
                    showingAddItem = true
                } label: {
                    Label("Add Item", systemImage: "plus.circle.fill")
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
        .frame(maxWidth: isRegularWidth ? 500 : .infinity)
        .padding()
    }

    // MARK: - Computed Properties

    /// Filtered items based on selected filter
    private var filteredItems: [WishListItem] {
        switch selectedFilter {
        case .active:
            return viewModel.activeItems
        case .purchased:
            return viewModel.purchasedItems
        case .all:
            return viewModel.items
        }
    }

    /// Empty state title
    private var emptyStateTitle: String {
        switch selectedFilter {
        case .active:
            return "No Active Items"
        case .purchased:
            return "No Purchased Items"
        case .all:
            return "No Wish List Items"
        }
    }

    /// Empty state message
    private var emptyStateMessage: String {
        switch selectedFilter {
        case .active:
            return "Add items you want to save for"
        case .purchased:
            return "No items have been purchased yet"
        case .all:
            return "Your wish list is empty"
        }
    }

    // MARK: - Methods

    /// Delete an item
    private func deleteItem(_ item: WishListItem) async {
        _ = await viewModel.deleteItem(id: item.id)
        deletingItem = nil
    }

    /// Mark item as purchased
    private func markAsPurchased(_ item: WishListItem) async {
        _ = await viewModel.markAsPurchased(id: item.id)
    }
}

// MARK: - Preview Provider

#Preview("Wish List - With Items") {
    NavigationStack {
        WishListView(childId: UUID())
    }
}

#Preview("Wish List - Empty") {
    NavigationStack {
        WishListView(childId: UUID())
    }
}
