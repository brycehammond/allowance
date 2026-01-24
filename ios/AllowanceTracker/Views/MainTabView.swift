import SwiftUI

/// Main tab-based navigation structure for the app
/// Uses sidebar navigation on iPad, tab bar on iPhone
@MainActor
struct MainTabView: View {

    // MARK: - Navigation Section

    enum Section: String, CaseIterable, Identifiable {
        case dashboard = "Dashboard"
        case budgets = "Budgets"
        case analytics = "Analytics"
        case profile = "Profile"

        var id: String { rawValue }

        var icon: String {
            switch self {
            case .dashboard: return "house.fill"
            case .budgets: return "chart.pie.fill"
            case .analytics: return "chart.line.uptrend.xyaxis"
            case .profile: return "person.fill"
            }
        }
    }

    // MARK: - Properties

    @Environment(AuthViewModel.self) private var authViewModel
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @State private var selectedTab = 0
    @State private var selectedSection: Section? = .dashboard

    // MARK: - Computed Properties

    /// True when on iPad in regular width mode
    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    /// Sections available based on user role
    private var availableSections: [Section] {
        if authViewModel.effectiveIsParent {
            return Section.allCases
        } else {
            // Children don't see budgets
            return [.dashboard, .analytics, .profile]
        }
    }

    // MARK: - Body

    var body: some View {
        if isRegularWidth {
            // iPad: Use sidebar navigation
            NavigationSplitView {
                sidebarContent
            } detail: {
                detailContent
            }
            .tint(DesignSystem.Colors.primary)
        } else {
            // iPhone: Use tab bar
            tabViewContent
        }
    }

    // MARK: - iPad Sidebar

    private var sidebarContent: some View {
        List(availableSections, selection: $selectedSection) { section in
            Label(section.rawValue, systemImage: section.icon)
                .tag(section)
        }
        .navigationTitle("Allowance")
        .listStyle(.sidebar)
    }

    // MARK: - iPad Detail Content

    @ViewBuilder
    private var detailContent: some View {
        switch selectedSection {
        case .dashboard:
            DashboardView()
        case .budgets:
            NavigationStack {
                BudgetTabView()
            }
        case .analytics:
            NavigationStack {
                AnalyticsTabView()
            }
        case .profile:
            ProfileView()
        case nil:
            ContentUnavailableView(
                "Select a Section",
                systemImage: "sidebar.left",
                description: Text("Choose a section from the sidebar")
            )
        }
    }

    // MARK: - iPhone Tab View

    private var tabViewContent: some View {
        TabView(selection: $selectedTab) {
            // Tab 1: Dashboard
            DashboardView()
                .tabItem {
                    Label("Dashboard", systemImage: "house.fill")
                }
                .tag(0)

            // Tab 2: Budget (if parent and not viewing as child)
            if authViewModel.effectiveIsParent {
                NavigationStack {
                    BudgetTabView()
                }
                .tabItem {
                    Label("Budgets", systemImage: "chart.pie.fill")
                }
                .tag(1)
            }

            // Tab 3: Analytics
            NavigationStack {
                AnalyticsTabView()
            }
            .tabItem {
                Label("Analytics", systemImage: "chart.line.uptrend.xyaxis")
            }
            .tag(2)

            // Tab 4: Profile
            ProfileView()
                .tabItem {
                    Label("Profile", systemImage: "person.fill")
                }
                .tag(3)
        }
        .tint(DesignSystem.Colors.primary)
    }
}

// MARK: - Analytics Tab View

/// Wrapper view for analytics tab to handle child selection
@MainActor
struct AnalyticsTabView: View {

    @State private var dashboardViewModel = DashboardViewModel()
    @State private var selectedChildId: UUID?

    var body: some View {
        VStack {
            if dashboardViewModel.children.isEmpty {
                ContentUnavailableView(
                    "No Children",
                    systemImage: "chart.bar.xaxis",
                    description: Text("Add children to view analytics")
                )
            } else {
                // Child selector
                if dashboardViewModel.children.count > 1 {
                    Picker("Child", selection: $selectedChildId) {
                        ForEach(dashboardViewModel.children) { child in
                            Text(child.fullName).tag(child.id as UUID?)
                        }
                    }
                    .pickerStyle(.segmented)
                    .padding()
                }

                // Analytics view for selected child
                if let childId = selectedChildId ?? dashboardViewModel.children.first?.id {
                    AnalyticsView(childId: childId)
                } else {
                    ContentUnavailableView(
                        "Select a Child",
                        systemImage: "person.crop.circle.badge.questionmark",
                        description: Text("Choose a child to view analytics")
                    )
                }
            }
        }
        .navigationTitle("Analytics")
        .task {
            await dashboardViewModel.loadChildren()
            if selectedChildId == nil {
                selectedChildId = dashboardViewModel.children.first?.id
            }
        }
    }
}

// MARK: - Budget Tab View

/// Wrapper view for budget tab to handle child selection
@MainActor
struct BudgetTabView: View {

    @State private var dashboardViewModel = DashboardViewModel()
    @State private var selectedChildId: UUID?

    var body: some View {
        VStack {
            if dashboardViewModel.children.isEmpty {
                ContentUnavailableView(
                    "No Children",
                    systemImage: "chart.pie.fill",
                    description: Text("Add children to manage budgets")
                )
            } else {
                // Child selector
                if dashboardViewModel.children.count > 1 {
                    Picker("Child", selection: $selectedChildId) {
                        ForEach(dashboardViewModel.children) { child in
                            Text(child.fullName).tag(child.id as UUID?)
                        }
                    }
                    .pickerStyle(.segmented)
                    .padding()
                }

                // Budget view for selected child
                if let selectedChild = dashboardViewModel.children.first(where: { $0.id == selectedChildId }) ?? dashboardViewModel.children.first {
                    BudgetManagementView(child: selectedChild)
                } else {
                    ContentUnavailableView(
                        "Select a Child",
                        systemImage: "person.crop.circle.badge.questionmark",
                        description: Text("Choose a child to manage budgets")
                    )
                }
            }
        }
        .navigationTitle("Budgets")
        .task {
            await dashboardViewModel.loadChildren()
            if selectedChildId == nil {
                selectedChildId = dashboardViewModel.children.first?.id
            }
        }
    }
}

// MARK: - Preview Provider

#Preview("Main Tab View - Parent") {
    MainTabView()
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

#Preview("Main Tab View - Child") {
    MainTabView()
        .environment({
            let vm = AuthViewModel()
            vm.currentUser = User(
                id: UUID(),
                email: "child@test.com",
                firstName: "Alice",
                lastName: "Smith",
                role: .child,
                familyId: UUID()
            )
            vm.isAuthenticated = true
            return vm
        }())
}
