import SwiftUI

/// Main dashboard view displaying all family children
struct DashboardView: View {

    // MARK: - Properties

    @StateObject private var viewModel: DashboardViewModel
    @EnvironmentObject private var authViewModel: AuthViewModel

    // MARK: - Initialization

    init(apiService: APIServiceProtocol = APIService()) {
        _viewModel = StateObject(wrappedValue: DashboardViewModel(apiService: apiService))
    }

    // MARK: - Body

    var body: some View {
        NavigationStack {
            ZStack {
                if viewModel.isLoading && viewModel.children.isEmpty {
                    // Loading state
                    ProgressView("Loading children...")
                } else if viewModel.children.isEmpty {
                    // Empty state
                    emptyStateView
                } else {
                    // Children list
                    childrenListView
                }
            }
            .navigationTitle("Dashboard")
            .toolbar {
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

    /// List of children cards
    private var childrenListView: some View {
        ScrollView {
            VStack(spacing: 16) {
                // Welcome message
                if let user = authViewModel.currentUser {
                    VStack(alignment: .leading, spacing: 8) {
                        Text("Welcome, \(user.firstName)!")
                            .font(.title2)
                            .fontWeight(.bold)

                        Text("Tap a child to view details")
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                    }
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(.horizontal)
                    .padding(.top)
                }

                // Children cards
                ForEach(viewModel.children) { child in
                    NavigationLink {
                        ChildDetailView(child: child)
                    } label: {
                        ChildCardView(child: child, onTap: {})
                    }
                    .buttonStyle(.plain)
                    .padding(.horizontal)
                }
            }
            .padding(.bottom)
        }
    }

    /// Empty state when no children exist
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
                // TODO: Navigate to add child screen
            } label: {
                Label("Add Child", systemImage: "plus.circle.fill")
                    .fontWeight(.semibold)
                    .frame(maxWidth: .infinity)
                    .padding()
                    .background(Color.blue)
                    .foregroundStyle(.white)
                    .clipShape(RoundedRectangle(cornerRadius: 12))
            }
            .padding(.horizontal, 40)
            .padding(.top)
        }
        .padding()
    }
}

// MARK: - Child Detail View

/// Detail view for a specific child
struct ChildDetailView: View {
    let child: Child

    var body: some View {
        TabView {
            // Transactions tab
            TransactionListView(childId: child.id)
                .tabItem {
                    Label("Transactions", systemImage: "list.bullet")
                }

            // Savings tab
            SavingsAccountView(childId: child.id)
                .tabItem {
                    Label("Savings", systemImage: "banknote")
                }

            // Wish List tab
            WishListView(childId: child.id)
                .tabItem {
                    Label("Wish List", systemImage: "star")
                }

            // Analytics tab
            AnalyticsView(childId: child.id)
                .tabItem {
                    Label("Analytics", systemImage: "chart.bar")
                }
        }
        .navigationTitle(child.fullName)
        .navigationBarTitleDisplayMode(.inline)
    }
}

// MARK: - Preview Provider

#Preview("Dashboard - With Children") {
    NavigationStack {
        DashboardView()
            .environmentObject({
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
            .environmentObject({
                let vm = AuthViewModel()
                vm.isAuthenticated = true
                return vm
            }())
    }
}

#Preview("Child Detail") {
    NavigationStack {
        ChildDetailView(
            child: Child(
                id: UUID(),
                firstName: "Alice",
                lastName: "Smith",
                weeklyAllowance: 10.00,
                currentBalance: 125.50,
                lastAllowanceDate: Date()
            )
        )
    }
}
