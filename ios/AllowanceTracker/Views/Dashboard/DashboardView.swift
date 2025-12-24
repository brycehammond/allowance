import SwiftUI

/// Main dashboard view displaying all family children
/// Optimized for iPhone (single column) and iPad (multi-column grid)
@MainActor
struct DashboardView: View {

    // MARK: - Properties

    @State private var viewModel: DashboardViewModel
    @Environment(AuthViewModel.self) private var authViewModel
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @State private var showingAddChild = false

    // MARK: - Computed Properties

    private var isChild: Bool {
        authViewModel.effectiveIsChild
    }

    private var isParent: Bool {
        authViewModel.currentUser?.role == .parent
    }

    /// True when on iPad in regular width mode
    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    /// Grid columns for adaptive layout
    private var gridColumns: [GridItem] {
        if isRegularWidth {
            return [
                GridItem(.adaptive(minimum: 320, maximum: 450), spacing: 16)
            ]
        } else {
            return [GridItem(.flexible())]
        }
    }

    // MARK: - Initialization

    init(apiService: APIServiceProtocol = APIService()) {
        _viewModel = State(wrappedValue: DashboardViewModel(apiService: apiService))
    }

    // MARK: - Body

    var body: some View {
        NavigationStack {
            ZStack {
                if viewModel.isLoading && viewModel.children.isEmpty {
                    // Loading state
                    ProgressView("Loading...")
                } else if isChild, let child = viewModel.children.first {
                    // Child user: show their own detail view directly
                    ChildDetailView(child: child)
                } else if viewModel.children.isEmpty {
                    // Empty state (parent with no children)
                    emptyStateView
                } else {
                    // Parent: show children list
                    childrenListView
                }
            }
            .navigationTitle(isChild ? "" : "Dashboard")
            .toolbar {
                if isParent && !viewModel.children.isEmpty {
                    ToolbarItem(placement: .navigationBarLeading) {
                        Button {
                            showingAddChild = true
                        } label: {
                            Label("Add Child", systemImage: "plus")
                        }
                    }
                }

                ToolbarItem(placement: .navigationBarTrailing) {
                    Button {
                        Task {
                            await authViewModel.logout()
                        }
                    } label: {
                        Image(systemName: "rectangle.portrait.and.arrow.right")
                    }
                }
            }
            .sheet(isPresented: $showingAddChild) {
                AddChildView()
            }
            .onChange(of: showingAddChild) { _, isShowing in
                if !isShowing {
                    // Refresh children list when sheet is dismissed
                    Task {
                        await viewModel.refresh()
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
                await viewModel.loadChildren()
            }
        }
    }

    // MARK: - Subviews

    /// List of children cards - uses adaptive grid on iPad
    private var childrenListView: some View {
        ScrollView {
            VStack(spacing: 16) {
                // Welcome message
                if let user = authViewModel.currentUser {
                    VStack(alignment: .leading, spacing: 8) {
                        Text("Welcome, \(user.firstName)!")
                            .font(.title2)
                            .fontWeight(.bold)

                        Text(isRegularWidth
                            ? "Select a child to view details"
                            : "Tap a child to view details")
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                    }
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(.horizontal, isRegularWidth ? 24 : 16)
                    .padding(.top)
                }

                // Children cards - grid on iPad, list on iPhone
                LazyVGrid(columns: gridColumns, spacing: 16) {
                    ForEach(viewModel.children) { child in
                        NavigationLink {
                            ChildDetailView(child: child)
                        } label: {
                            ChildCardView(child: child)
                        }
                        .buttonStyle(.plain)
                    }
                }
                .padding(.horizontal, isRegularWidth ? 24 : 16)
            }
            .padding(.bottom)
        }
    }

    /// Empty state when no children exist - centered and constrained on iPad
    private var emptyStateView: some View {
        VStack(spacing: 24) {
            Image(systemName: "person.2.slash")
                .font(.system(size: 60))
                .foregroundStyle(.secondary)

            VStack(spacing: 8) {
                Text("No Children Yet")
                    .font(.title2)
                    .fontWeight(.bold)

                Text("Add children to your family to get started")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }

            Button {
                showingAddChild = true
            } label: {
                Label("Add Child", systemImage: "plus.circle.fill")
                    .fontWeight(.semibold)
                    .frame(maxWidth: .infinity)
                    .padding()
                    .background(DesignSystem.Colors.primary)
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

// MARK: - Child Detail View

/// Detail view for a specific child
/// Uses TabView on iPhone, optimized layout on iPad
@MainActor
struct ChildDetailView: View {
    let child: Child
    @Environment(AuthViewModel.self) private var authViewModel
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @State private var selectedTab = 0

    private var isParent: Bool {
        authViewModel.effectiveIsParent
    }

    /// Whether the parent can enter child view mode (only when not already in child view mode)
    private var canEnterChildViewMode: Bool {
        authViewModel.currentUser?.isParent == true && !authViewModel.isViewingAsChild
    }

    /// True when on iPad in regular width mode
    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    var body: some View {
        VStack(spacing: 0) {
            // Header with balance breakdown
            childHeaderView

            // Tab view - same structure works well on both platforms
            TabView(selection: $selectedTab) {
                // Transactions tab
                TransactionListView(
                    childId: child.id,
                    savingsBalance: child.savingsBalance,
                    allowDebt: child.allowDebt
                )
                    .tabItem {
                        Label("Transactions", systemImage: "receipt")
                    }
                    .tag(0)

                // Wish List tab
                WishListView(childId: child.id)
                    .tabItem {
                        Label("Wish List", systemImage: "star")
                    }
                    .tag(1)

                // Analytics tab
                AnalyticsView(childId: child.id)
                    .tabItem {
                        Label("Analytics", systemImage: "chart.line.uptrend.xyaxis")
                    }
                    .tag(2)

                // Savings tab (Parent only)
                if isParent {
                    SavingsAccountView(childId: child.id)
                        .tabItem {
                            Label("Savings", systemImage: "banknote")
                        }
                        .tag(3)
                }

                // Settings tab (Parent only)
                if isParent {
                    ChildSettingsView(childId: child.id, apiService: APIService())
                        .tabItem {
                            Label("Settings", systemImage: "gearshape")
                        }
                        .tag(4)
                }
            }
            .tint(DesignSystem.Colors.primary)
        }
        .navigationTitle(authViewModel.isViewingAsChild ? "" : child.fullName)
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            // View as child button (only for parents, not already in child view mode)
            if canEnterChildViewMode {
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button {
                        withAnimation(.easeInOut(duration: 0.2)) {
                            authViewModel.enterChildViewMode(child: child)
                        }
                    } label: {
                        Label("View as Child", systemImage: "eye")
                    }
                }
            }

            // Child view mode indicator with exit button
            if authViewModel.isViewingAsChild {
                ToolbarItem(placement: .principal) {
                    HStack(spacing: 8) {
                        Image(systemName: "eye.fill")
                            .foregroundStyle(.orange)
                        Text("Viewing as \(child.firstName)")
                            .font(.subheadline)
                            .fontWeight(.medium)
                        Button("Exit") {
                            withAnimation(.easeInOut(duration: 0.2)) {
                                authViewModel.exitChildViewMode()
                            }
                        }
                        .font(.subheadline)
                        .fontWeight(.semibold)
                        .foregroundStyle(.orange)
                    }
                }
            }
        }
    }

    // MARK: - Child Header View

    private var childHeaderView: some View {
        VStack(spacing: 12) {
            HStack(alignment: .center, spacing: 16) {
                // Avatar
                ZStack {
                    Circle()
                        .fill(DesignSystem.Colors.primary.opacity(0.15))
                        .frame(width: 56, height: 56)
                    Text(String(child.firstName.prefix(1)))
                        .font(.title2)
                        .fontWeight(.semibold)
                        .foregroundStyle(DesignSystem.Colors.primary)
                }

                // Name and allowance
                VStack(alignment: .leading, spacing: 4) {
                    Text(child.fullName)
                        .font(.title3)
                        .fontWeight(.bold)
                    Text("Weekly: \(child.weeklyAllowance.currencyFormatted)")
                        .font(.subheadline)
                        .foregroundStyle(.secondary)
                }

                Spacer()

                // Total balance
                VStack(alignment: .trailing, spacing: 4) {
                    Text(child.formattedTotalBalance)
                        .font(.title2)
                        .fontWeight(.bold)
                        .fontDesign(.monospaced)
                    Text("Total Balance")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }

            // Balance breakdown
            HStack(spacing: 24) {
                Spacer()

                // Spending balance
                VStack(alignment: .trailing, spacing: 2) {
                    Text(child.formattedBalance)
                        .font(.headline)
                        .fontDesign(.monospaced)
                    Text("Spending")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                // Savings balance
                VStack(alignment: .trailing, spacing: 2) {
                    Text(child.formattedSavingsBalance)
                        .font(.headline)
                        .fontDesign(.monospaced)
                        .foregroundStyle(DesignSystem.Colors.primary)
                    Text("Savings")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .overlay(
            Rectangle()
                .frame(height: 1)
                .foregroundStyle(Color(.separator)),
            alignment: .bottom
        )
    }
}

// MARK: - Preview Provider

#Preview("Dashboard - With Children") {
    NavigationStack {
        DashboardView()
            .environment({
                let vm = AuthViewModel()
                vm.currentUser = User(
                    id: UUID(),
                    email: "parent@test.com",
                    firstName: "John",
                    lastName: "Doe",
                    role: .parent,
                    familyId: UUID()
                )
                vm.isAuthenticated = true
                return vm
            }())
    }
}

#Preview("Dashboard - Loading") {
    NavigationStack {
        DashboardView()
            .environment({
                let vm = AuthViewModel()
                vm.isAuthenticated = true
                return vm
            }())
    }
}

#Preview("Child Detail - Parent") {
    NavigationStack {
        ChildDetailView(
            child: Child(
                id: UUID(),
                firstName: "Alice",
                lastName: "Smith",
                weeklyAllowance: 10.00,
                currentBalance: 125.50,
                savingsBalance: 45.00,
                lastAllowanceDate: Date(),
                allowanceDay: .friday,
                savingsAccountEnabled: true,
                savingsTransferType: .percentage,
                savingsTransferPercentage: 20,
                savingsTransferAmount: nil,
                savingsBalanceVisibleToChild: true,
                allowDebt: false
            )
        )
        .environment({
            let vm = AuthViewModel()
            vm.currentUser = User(
                id: UUID(),
                email: "parent@test.com",
                firstName: "John",
                lastName: "Doe",
                role: .parent,
                familyId: UUID()
            )
            vm.isAuthenticated = true
            return vm
        }())
    }
}

#Preview("Child Detail - Child") {
    NavigationStack {
        ChildDetailView(
            child: Child(
                id: UUID(),
                firstName: "Alice",
                lastName: "Smith",
                weeklyAllowance: 10.00,
                currentBalance: 125.50,
                savingsBalance: 30.00,
                lastAllowanceDate: Date(),
                allowanceDay: nil,
                savingsAccountEnabled: true,
                savingsTransferType: .percentage,
                savingsTransferPercentage: 20,
                savingsTransferAmount: nil,
                savingsBalanceVisibleToChild: true,
                allowDebt: false
            )
        )
        .environment({
            let vm = AuthViewModel()
            vm.currentUser = User(
                id: UUID(),
                email: "alice@test.com",
                firstName: "Alice",
                lastName: "Smith",
                role: .child,
                familyId: UUID()
            )
            vm.isAuthenticated = true
            return vm
        }())
    }
}
