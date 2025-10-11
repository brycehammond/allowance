# iOS App Feature Parity Specification

## Overview

This specification outlines the complete roadmap for bringing the iOS native app to feature parity with the Blazor web application. The iOS app currently has basic authentication, budgeting, and category features implemented. This plan details all missing features, implementation phases, and technical approach.

**Target**: Complete feature parity with web app (Phases 1-7 complete)
**Approach**: Test-Driven Development (TDD) following iOS best practices
**Timeline**: 8 implementation phases
**Testing**: >80% code coverage target

---

## Gap Analysis

### Web App Features (Complete ✅)

1. ✅ **Authentication** - ASP.NET Core Identity + JWT
2. ✅ **Dashboard** - Family dashboard showing all children with real-time balances
3. ✅ **Transaction Management** - Full CRUD for transactions
4. ✅ **Wish List** - Full CRUD with affordability calculation
5. ✅ **Analytics Dashboard** - Balance history, income vs spending, category breakdown, monthly comparison
6. ✅ **Categories** - Transaction categories with predefined set
7. ✅ **Budgets** - Category budgets with spending tracking
8. ✅ **Savings Accounts** - Virtual savings with auto-transfer functionality
9. ✅ **Family Management** - View and manage family
10. ✅ **Children Management** - Add, edit, view children
11. ✅ **Real-time Updates** - SignalR for live balance updates
12. ✅ **Allowance Management** - Weekly allowance configuration and payment

### iOS App Features (Currently Implemented ✅)

1. ✅ **Authentication Foundation**
   - User and Auth models
   - KeychainService for token storage
   - APIService with JWT integration
   - AuthViewModel
   - LoginView and RegisterView
   - Tests for auth flow

2. ✅ **Category & Budget Features**
   - TransactionCategory model
   - CategoryBudget, CategorySpending, BudgetCheckResult models
   - CategoryService and BudgetService
   - BudgetViewModel
   - Budget management views (BudgetManagementView, AddBudgetSheet, BudgetCardView)
   - CategoryPicker, CategorySpendingChart, BudgetWarningView
   - CategorySpendingView
   - Tests for budget features

3. ✅ **Basic Infrastructure**
   - Child and User models
   - API service protocol and implementation
   - URLSession abstraction
   - DesignSystem with colors, typography, button styles
   - Extensions for Color and Decimal
   - AppConstants
   - Unit test structure

### iOS App Features (Missing ❌)

1. ❌ **Dashboard** - No parent dashboard showing all children
2. ❌ **Transaction Management** - No transaction models, services, or views
3. ❌ **Transaction History** - No transaction list or detail views
4. ❌ **Wish List** - Completely missing
5. ❌ **Savings Accounts** - Completely missing
6. ❌ **Comprehensive Analytics** - Only category analytics exist, missing:
   - Balance history charts
   - Income vs spending summary
   - Monthly comparison
   - Consolidated analytics dashboard
7. ❌ **Family Management** - No family views
8. ❌ **Children Management** - No add/edit child screens
9. ❌ **SignalR Real-time Updates** - No real-time functionality
10. ❌ **Navigation Structure** - No TabView or main navigation
11. ❌ **Savings Progress Widget** - For wish list goals
12. ❌ **Quick Transaction Sheet** - Inline transaction creation
13. ❌ **Child Detail View** - Individual child detail screen
14. ❌ **Transaction Form** - Reusable transaction input component
15. ❌ **Balance Display Component** - Reusable balance widget

---

## Feature Mapping: Web → iOS

### 1. Dashboard Feature

**Web Implementation:**
- `Pages/Dashboard.razor` - Shows grid of child cards
- `Components/ChildCard.razor` - Individual child card with balance, quick actions
- SignalR integration for real-time updates
- Responsive grid layout (1-3 columns)

**iOS Implementation Needed:**
```swift
// Models
- Child.swift ✅ (already exists, may need enhancement)
- ChildDto.swift (NEW)

// Services
- FamilyService.swift (NEW)
- DashboardService.swift (NEW)

// ViewModels
- DashboardViewModel.swift (NEW)

// Views
- DashboardView.swift (NEW) - Main parent dashboard
- ChildCardView.swift (NEW) - Reusable child card component
- QuickTransactionSheet.swift (NEW) - Modal for quick transactions
- BalanceDisplayView.swift (NEW) - Reusable balance widget

// Tests
- DashboardViewModelTests.swift (NEW)
- ChildCardViewTests.swift (NEW - snapshot tests)
```

### 2. Transaction Management Feature

**Web Implementation:**
- `Api/V1/TransactionsController.cs` - CRUD endpoints
- `Services/TransactionService.cs` - Business logic
- `Components/TransactionForm.razor` - Input form with validation
- Real-time transaction notifications

**iOS Implementation Needed:**
```swift
// Models
- Transaction.swift (NEW)
- TransactionType.swift (NEW)
- CreateTransactionDto.swift (NEW)
- TransactionDto.swift (NEW)

// Services
- TransactionService.swift (NEW)
- ITransactionService.swift protocol (NEW)

// ViewModels
- TransactionViewModel.swift (NEW)
- TransactionFormViewModel.swift (NEW)

// Views
- TransactionListView.swift (NEW)
- TransactionDetailView.swift (NEW)
- TransactionFormView.swift (NEW)
- TransactionRow.swift (NEW)
- AddTransactionSheet.swift (NEW)

// Tests
- TransactionServiceTests.swift (NEW)
- TransactionViewModelTests.swift (NEW)
- TransactionFormValidationTests.swift (NEW)
```

### 3. Wish List Feature

**Web Implementation:**
- `Api/V1/WishListController.cs` - CRUD + purchase endpoints
- `Services/WishListService.cs` - Business logic
- Affordability calculation based on current balance
- Purchase tracking

**iOS Implementation Needed:**
```swift
// Models
- WishListItem.swift (NEW)
- CreateWishListItemDto.swift (NEW)
- UpdateWishListItemDto.swift (NEW)
- WishListItemDto.swift (NEW)

// Services
- WishListService.swift (NEW)
- IWishListService.swift protocol (NEW)

// ViewModels
- WishListViewModel.swift (NEW)

// Views
- WishListView.swift (NEW)
- WishListItemRow.swift (NEW)
- AddWishListItemSheet.swift (NEW)
- EditWishListItemSheet.swift (NEW)
- WishListProgressView.swift (NEW)

// Components
- SavingsProgressWidget.swift (NEW) - Shows progress toward goals

// Tests
- WishListServiceTests.swift (NEW)
- WishListViewModelTests.swift (NEW)
```

### 4. Analytics Dashboard Feature

**Web Implementation:**
- `Pages/Analytics.razor` - Comprehensive analytics page
- `Components/BalanceHistoryChart.razor` - Line chart
- `Components/IncomeSpendingChart.razor` - Bar chart comparison
- Income vs spending summary with savings rate
- Spending breakdown by category
- Monthly comparison table

**iOS Implementation Needed:**
```swift
// Models (Analytics)
- BalancePoint.swift (NEW)
- IncomeSpendingSummary.swift (NEW)
- MonthlyComparison.swift (NEW)
- SpendingTrend.swift (NEW)
- TransactionHeatmap.swift (NEW)

// Services
- AnalyticsService.swift (NEW)
- IAnalyticsService.swift protocol (NEW)

// ViewModels
- AnalyticsViewModel.swift (NEW - comprehensive, replace partial one)

// Views
- AnalyticsView.swift (NEW - comprehensive dashboard)
- BalanceHistoryChartView.swift (NEW)
- IncomeSpendingChartView.swift (NEW)
- MonthlyComparisonView.swift (NEW)
- SpendingBreakdownView.swift (NEW)

// Tests
- AnalyticsServiceTests.swift (NEW)
- AnalyticsViewModelTests.swift (NEW)
```

### 5. Savings Accounts Feature

**Web Implementation:**
- `Api/V1/SavingsAccountController.cs` - Savings account endpoints
- `Services/SavingsAccountService.cs` - Business logic
- `Models/SavingsTransaction.cs` - Savings transaction tracking
- Auto-transfer functionality
- Manual deposit/withdrawal
- Transfer type tracking (Manual, AutoTransfer, Goal)

**iOS Implementation Needed:**
```swift
// Models
- SavingsAccount.swift (NEW)
- SavingsTransaction.swift (NEW)
- SavingsTransactionType.swift (NEW)
- SavingsTransferType.swift (NEW)
- CreateSavingsAccountDto.swift (NEW)
- SavingsAccountDto.swift (NEW)

// Services
- SavingsAccountService.swift (NEW)
- ISavingsAccountService.swift protocol (NEW)

// ViewModels
- SavingsAccountViewModel.swift (NEW)

// Views
- SavingsAccountView.swift (NEW)
- SavingsAccountDetailView.swift (NEW)
- AddSavingsAccountSheet.swift (NEW)
- SavingsTransactionListView.swift (NEW)
- SavingsDepositSheet.swift (NEW)
- SavingsWithdrawSheet.swift (NEW)

// Tests
- SavingsAccountServiceTests.swift (NEW)
- SavingsAccountViewModelTests.swift (NEW)
```

### 6. Family & Children Management Feature

**Web Implementation:**
- `Api/V1/FamiliesController.cs` - Family management
- `Api/V1/ChildrenController.cs` - Children CRUD
- `Pages/Children/Create.razor` - Add child form
- Family service for data access

**iOS Implementation Needed:**
```swift
// Models
- Family.swift (NEW)
- FamilyDto.swift (NEW)
- CreateChildDto.swift (NEW)
- UpdateChildDto.swift (NEW)

// Services
- FamilyService.swift (NEW)
- ChildManagementService.swift (NEW)

// ViewModels
- FamilyViewModel.swift (NEW)
- ChildManagementViewModel.swift (NEW)

// Views
- FamilyView.swift (NEW)
- AddChildSheet.swift (NEW)
- EditChildSheet.swift (NEW)
- ChildDetailView.swift (NEW)

// Tests
- FamilyServiceTests.swift (NEW)
- ChildManagementServiceTests.swift (NEW)
```

### 7. SignalR Real-time Updates Feature

**Web Implementation:**
- `Hubs/FamilyHub.cs` - SignalR hub for real-time updates
- TransactionCreated event
- BalanceUpdated event
- Automatic reconnection

**iOS Implementation Needed:**
```swift
// Services
- SignalRService.swift (NEW)
- SignalRConnectionManager.swift (NEW)

// Protocols
- SignalRConnectionDelegate.swift (NEW)

// Events
- TransactionEvent.swift (NEW)
- BalanceUpdateEvent.swift (NEW)

// Integration
- Update DashboardViewModel to listen for events
- Update TransactionViewModel to listen for events
- Update BalanceDisplayView to show real-time updates

// Dependencies
- Add SignalRClient Swift package

// Tests
- SignalRServiceTests.swift (NEW)
- SignalRConnectionTests.swift (NEW)
```

### 8. Navigation Structure

**Web Implementation:**
- `Shared/MainLayout.razor` - App shell
- `Shared/NavMenu.razor` - Sidebar navigation
- Header with user info and logout

**iOS Implementation Needed:**
```swift
// Views
- MainTabView.swift (NEW) - Root TabView navigation
- ProfileView.swift (NEW) - User profile and settings
- NavigationCoordinator.swift (NEW) - Navigation management

// Tabs:
1. Dashboard Tab - DashboardView
2. Transactions Tab - TransactionListView
3. Analytics Tab - AnalyticsView
4. Profile Tab - ProfileView with settings

// Tests
- NavigationTests.swift (NEW)
```

---

## Implementation Phases

### Phase 1: Transaction Foundation (Week 1)

**Goal**: Build complete transaction management system with TDD

**Models & DTOs**:
```swift
// Transaction.swift
struct Transaction: Codable, Identifiable, Equatable {
    let id: UUID
    let childId: UUID
    let amount: Decimal
    let type: TransactionType
    let description: String
    let categoryId: UUID?
    let balanceAfter: Decimal
    let createdAt: Date
    let createdByName: String

    var isCredit: Bool {
        type == .credit
    }

    var formattedAmount: String {
        let prefix = isCredit ? "+" : "-"
        return "\(prefix)\(amount.currencyFormatted)"
    }
}

// TransactionType.swift
enum TransactionType: String, Codable {
    case credit = "Credit"
    case debit = "Debit"
}

// CreateTransactionDto.swift
struct CreateTransactionDto: Codable {
    let childId: UUID
    let amount: Decimal
    let type: TransactionType
    let description: String
    let categoryId: UUID?
}

// TransactionDto.swift
typealias TransactionDto = Transaction
```

**Services**:
```swift
// ITransactionService.swift
protocol ITransactionService {
    func createTransaction(_ dto: CreateTransactionDto) async throws -> Transaction
    func getTransactions(for childId: UUID, limit: Int?) async throws -> [Transaction]
    func getTransaction(id: UUID) async throws -> Transaction
}

// TransactionService.swift
final class TransactionService: ITransactionService {
    private let apiService: APIServiceProtocol

    init(apiService: APIServiceProtocol = APIService.shared) {
        self.apiService = apiService
    }

    func createTransaction(_ dto: CreateTransactionDto) async throws -> Transaction {
        return try await apiService.request(
            endpoint: .createTransaction,
            method: .post,
            body: dto
        )
    }

    func getTransactions(for childId: UUID, limit: Int? = nil) async throws -> [Transaction] {
        var endpoint = Endpoint.childTransactions(childId)
        if let limit = limit {
            // Add query parameter
        }
        return try await apiService.request(
            endpoint: endpoint,
            method: .get,
            body: nil as String?
        )
    }

    func getTransaction(id: UUID) async throws -> Transaction {
        return try await apiService.request(
            endpoint: .transaction(id),
            method: .get,
            body: nil as String?
        )
    }
}
```

**Tests (Write FIRST)**:
```swift
// TransactionServiceTests.swift
final class TransactionServiceTests: XCTestCase {
    var sut: TransactionService!
    var mockAPIService: MockAPIService!

    override func setUp() {
        super.setUp()
        mockAPIService = MockAPIService()
        sut = TransactionService(apiService: mockAPIService)
    }

    func testCreateTransaction_Success() async throws {
        // Given
        let dto = CreateTransactionDto(
            childId: UUID(),
            amount: 10.00,
            type: .credit,
            description: "Allowance",
            categoryId: nil
        )
        let expectedTransaction = Transaction(
            id: UUID(),
            childId: dto.childId,
            amount: dto.amount,
            type: dto.type,
            description: dto.description,
            categoryId: nil,
            balanceAfter: 110.00,
            createdAt: Date(),
            createdByName: "Parent"
        )
        mockAPIService.mockResponse = expectedTransaction

        // When
        let result = try await sut.createTransaction(dto)

        // Then
        XCTAssertEqual(result.amount, 10.00)
        XCTAssertEqual(result.type, .credit)
    }

    func testGetTransactions_ReturnsOrdered() async throws {
        // Test implementation
    }

    func testCreateTransaction_InsufficientFunds_ThrowsError() async throws {
        // Test implementation
    }
}
```

**Tasks**:
- [ ] Write Transaction model tests (5 tests)
- [ ] Implement Transaction model
- [ ] Write TransactionService tests (10 tests)
- [ ] Implement TransactionService
- [ ] Update Endpoints enum with transaction endpoints
- [ ] Register TransactionService in DI
- [ ] **Total: 15 tests passing**

---

### Phase 2: Dashboard & Child Cards (Week 1-2)

**Goal**: Create parent dashboard with child cards and real-time balance display

**ViewModels**:
```swift
// DashboardViewModel.swift
@MainActor
final class DashboardViewModel: ObservableObject {
    @Published var children: [ChildDto] = []
    @Published var isLoading = false
    @Published var errorMessage: String?
    @Published var showAddChild = false

    private let familyService: IFamilyService
    private let authViewModel: AuthViewModel

    init(
        familyService: IFamilyService = FamilyService.shared,
        authViewModel: AuthViewModel
    ) {
        self.familyService = familyService
        self.authViewModel = authViewModel
    }

    func loadChildren() async {
        isLoading = true
        errorMessage = nil

        do {
            children = try await familyService.getChildren()
        } catch {
            errorMessage = "Failed to load children: \(error.localizedDescription)"
        }

        isLoading = false
    }

    func refresh() async {
        await loadChildren()
    }

    func deleteChild(_ child: ChildDto) async throws {
        try await familyService.deleteChild(id: child.id)
        await loadChildren()
    }
}
```

**Views**:
```swift
// DashboardView.swift
struct DashboardView: View {
    @StateObject private var viewModel: DashboardViewModel
    @EnvironmentObject var authViewModel: AuthViewModel

    init(authViewModel: AuthViewModel) {
        _viewModel = StateObject(wrappedValue: DashboardViewModel(authViewModel: authViewModel))
    }

    var body: some View {
        NavigationStack {
            ScrollView {
                if viewModel.isLoading {
                    ProgressView()
                        .scaleEffect(1.5)
                } else if viewModel.children.isEmpty {
                    emptyStateView
                } else {
                    LazyVGrid(columns: [
                        GridItem(.flexible()),
                        GridItem(.flexible())
                    ], spacing: 16) {
                        ForEach(viewModel.children) { child in
                            ChildCardView(child: child) {
                                await viewModel.refresh()
                            }
                        }
                    }
                    .padding()
                }
            }
            .navigationTitle("Family Dashboard")
            .toolbar {
                ToolbarItem(placement: .primaryAction) {
                    Button {
                        viewModel.showAddChild = true
                    } label: {
                        Image(systemName: "person.badge.plus")
                    }
                }
            }
            .sheet(isPresented: $viewModel.showAddChild) {
                AddChildSheet()
            }
            .refreshable {
                await viewModel.refresh()
            }
        }
        .task {
            await viewModel.loadChildren()
        }
    }

    private var emptyStateView: some View {
        ContentUnavailableView(
            "No Children Yet",
            systemImage: "person.2.slash",
            description: Text("Add a child to get started with allowance tracking")
        )
    }
}

// ChildCardView.swift
struct ChildCardView: View {
    let child: ChildDto
    var onTransactionAdded: (() async -> Void)?

    @State private var showTransactionSheet = false
    @State private var showDetailView = false

    var body: some View {
        VStack(alignment: .leading, spacing: DesignSystem.Spacing.md) {
            // Header
            HStack {
                Image(systemName: "person.circle.fill")
                    .font(.title)
                    .foregroundStyle(DesignSystem.Colors.primary)

                VStack(alignment: .leading) {
                    Text(child.fullName)
                        .font(.headline)

                    Text("Weekly: \(child.weeklyAllowance.currencyFormatted)")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Spacer()
            }

            Divider()

            // Balance
            HStack {
                Text("Balance")
                    .font(.caption)
                    .foregroundStyle(.secondary)

                Spacer()

                Text(child.currentBalance.currencyFormatted)
                    .font(.title2)
                    .fontWeight(.bold)
                    .foregroundStyle(DesignSystem.Colors.primary)
                    .fontDesign(.monospaced)
            }

            // Actions
            HStack(spacing: DesignSystem.Spacing.sm) {
                Button {
                    showTransactionSheet = true
                } label: {
                    Label("Add Money", systemImage: "plus.circle")
                        .font(.caption)
                }
                .buttonStyle(DesignSystem.ButtonStyles.Primary())
                .controlSize(.small)

                Button {
                    showDetailView = true
                } label: {
                    Label("History", systemImage: "list.bullet")
                        .font(.caption)
                }
                .buttonStyle(DesignSystem.ButtonStyles.Secondary())
                .controlSize(.small)
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: DesignSystem.CornerRadius.medium))
        .shadow(color: .black.opacity(0.1), radius: 4, x: 0, y: 2)
        .sheet(isPresented: $showTransactionSheet) {
            AddTransactionSheet(childId: child.id) {
                await onTransactionAdded?()
            }
        }
        .sheet(isPresented: $showDetailView) {
            TransactionListView(childId: child.id)
        }
    }
}
```

**Tests**:
```swift
// DashboardViewModelTests.swift
final class DashboardViewModelTests: XCTestCase {
    @MainActor
    func testLoadChildren_Success() async {
        // Given
        let mockService = MockFamilyService()
        let authVM = AuthViewModel()
        let sut = DashboardViewModel(familyService: mockService, authViewModel: authVM)

        mockService.mockChildren = [
            ChildDto(id: UUID(), firstName: "Alice", lastName: "Smith",
                    weeklyAllowance: 10, currentBalance: 50, lastAllowanceDate: nil)
        ]

        // When
        await sut.loadChildren()

        // Then
        XCTAssertEqual(sut.children.count, 1)
        XCTAssertEqual(sut.children.first?.firstName, "Alice")
        XCTAssertFalse(sut.isLoading)
        XCTAssertNil(sut.errorMessage)
    }

    @MainActor
    func testLoadChildren_Failure_SetsErrorMessage() async {
        // Test implementation
    }
}
```

**Tasks**:
- [ ] Write ChildDto model tests (3 tests)
- [ ] Implement ChildDto (enhancement to existing Child)
- [ ] Write FamilyService tests (8 tests)
- [ ] Implement FamilyService
- [ ] Write DashboardViewModel tests (10 tests)
- [ ] Implement DashboardViewModel
- [ ] Create DashboardView with SwiftUI
- [ ] Create ChildCardView component
- [ ] Write ChildCard snapshot tests (3 tests)
- [ ] **Total: 24 tests passing**

---

### Phase 3: Transaction UI & Forms (Week 2)

**Goal**: Build transaction list, detail views, and transaction form with validation

**Views**:
```swift
// TransactionListView.swift
struct TransactionListView: View {
    let childId: UUID
    @StateObject private var viewModel: TransactionViewModel
    @Environment(\.dismiss) private var dismiss

    init(childId: UUID) {
        self.childId = childId
        _viewModel = StateObject(wrappedValue: TransactionViewModel(childId: childId))
    }

    var body: some View {
        NavigationStack {
            List {
                ForEach(viewModel.transactions) { transaction in
                    NavigationLink {
                        TransactionDetailView(transaction: transaction)
                    } label: {
                        TransactionRow(transaction: transaction)
                    }
                }
            }
            .navigationTitle("Transactions")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Done") {
                        dismiss()
                    }
                }
            }
            .overlay {
                if viewModel.isLoading {
                    ProgressView()
                } else if viewModel.transactions.isEmpty {
                    ContentUnavailableView(
                        "No Transactions",
                        systemImage: "list.bullet.rectangle",
                        description: Text("Transactions will appear here")
                    )
                }
            }
            .refreshable {
                await viewModel.refresh()
            }
        }
        .task {
            await viewModel.loadTransactions()
        }
    }
}

// TransactionRow.swift
struct TransactionRow: View {
    let transaction: Transaction

    var body: some View {
        HStack(spacing: DesignSystem.Spacing.md) {
            // Icon
            Image(systemName: transaction.isCredit ? "arrow.down.circle.fill" : "arrow.up.circle.fill")
                .font(.title2)
                .foregroundStyle(transaction.isCredit ? DesignSystem.Colors.success : DesignSystem.Colors.error)

            // Content
            VStack(alignment: .leading, spacing: 4) {
                Text(transaction.description)
                    .font(.headline)

                HStack {
                    Text(transaction.createdAt, style: .date)
                    Text("•")
                    Text(transaction.createdAt, style: .time)
                }
                .font(.caption)
                .foregroundStyle(.secondary)

                if let categoryId = transaction.categoryId {
                    // Show category badge
                }
            }

            Spacer()

            // Amount
            VStack(alignment: .trailing, spacing: 4) {
                Text(transaction.formattedAmount)
                    .font(.headline)
                    .fontDesign(.monospaced)
                    .foregroundStyle(transaction.isCredit ? DesignSystem.Colors.success : DesignSystem.Colors.error)

                Text("Balance: \(transaction.balanceAfter.currencyFormatted)")
                    .font(.caption)
                    .fontDesign(.monospaced)
                    .foregroundStyle(.secondary)
            }
        }
        .padding(.vertical, 4)
    }
}

// AddTransactionSheet.swift
struct AddTransactionSheet: View {
    let childId: UUID
    var onTransactionAdded: (() async -> Void)?

    @StateObject private var viewModel: TransactionFormViewModel
    @Environment(\.dismiss) private var dismiss

    init(childId: UUID, onTransactionAdded: (() async -> Void)? = nil) {
        self.childId = childId
        self.onTransactionAdded = onTransactionAdded
        _viewModel = StateObject(wrappedValue: TransactionFormViewModel(childId: childId))
    }

    var body: some View {
        NavigationStack {
            Form {
                Section("Transaction Details") {
                    HStack {
                        Text("$")
                            .foregroundStyle(.secondary)
                        TextField("Amount", value: $viewModel.amount, format: .currency(code: "USD"))
                            .keyboardType(.decimalPad)
                            .fontDesign(.monospaced)
                    }

                    Picker("Type", selection: $viewModel.type) {
                        Text("Add Money").tag(TransactionType.credit)
                        Text("Spend Money").tag(TransactionType.debit)
                    }
                    .pickerStyle(.segmented)

                    TextField("Description", text: $viewModel.description)
                        .autocorrectionDisabled()

                    CategoryPicker(selectedCategoryId: $viewModel.categoryId)
                }

                if let errorMessage = viewModel.errorMessage {
                    Section {
                        Text(errorMessage)
                            .foregroundStyle(.red)
                            .font(.caption)
                    }
                }
            }
            .navigationTitle("Add Transaction")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        dismiss()
                    }
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button("Save") {
                        Task {
                            await saveTransaction()
                        }
                    }
                    .disabled(!viewModel.isValid || viewModel.isProcessing)
                }
            }
            .overlay {
                if viewModel.isProcessing {
                    ProgressView()
                }
            }
        }
    }

    private func saveTransaction() async {
        await viewModel.save()
        if viewModel.errorMessage == nil {
            await onTransactionAdded?()
            dismiss()
        }
    }
}

// TransactionFormViewModel.swift
@MainActor
final class TransactionFormViewModel: ObservableObject {
    @Published var amount: Decimal = 0
    @Published var type: TransactionType = .credit
    @Published var description: String = ""
    @Published var categoryId: UUID?
    @Published var errorMessage: String?
    @Published var isProcessing = false

    let childId: UUID
    private let transactionService: ITransactionService

    init(
        childId: UUID,
        transactionService: ITransactionService = TransactionService.shared
    ) {
        self.childId = childId
        self.transactionService = transactionService
    }

    var isValid: Bool {
        amount > 0 && !description.isEmpty
    }

    func save() async {
        guard isValid else { return }

        isProcessing = true
        errorMessage = nil

        do {
            let dto = CreateTransactionDto(
                childId: childId,
                amount: amount,
                type: type,
                description: description,
                categoryId: categoryId
            )

            _ = try await transactionService.createTransaction(dto)
        } catch {
            errorMessage = error.localizedDescription
        }

        isProcessing = false
    }
}
```

**Tasks**:
- [ ] Write TransactionViewModel tests (12 tests)
- [ ] Implement TransactionViewModel
- [ ] Write TransactionFormViewModel tests (10 tests)
- [ ] Implement TransactionFormViewModel
- [ ] Create TransactionListView
- [ ] Create TransactionRow component
- [ ] Create AddTransactionSheet
- [ ] Create TransactionDetailView
- [ ] Write snapshot tests for transaction views (5 tests)
- [ ] **Total: 27 tests passing**

---

### Phase 4: Wish List Feature (Week 3)

**Goal**: Complete wish list functionality with affordability tracking

**Models**:
```swift
// WishListItem.swift
struct WishListItem: Codable, Identifiable, Equatable {
    let id: UUID
    let childId: UUID
    let name: String
    let price: Decimal
    let url: String?
    let notes: String?
    let isPurchased: Bool
    let purchasedAt: Date?
    let createdAt: Date

    // Computed on server, included in response
    let canAfford: Bool

    func progressPercentage(currentBalance: Decimal) -> Double {
        guard price > 0 else { return 0 }
        let progress = Double(truncating: currentBalance as NSNumber) / Double(truncating: price as NSNumber)
        return min(progress, 1.0) * 100
    }

    func weeksToGoal(currentBalance: Decimal, weeklyAllowance: Decimal) -> Int? {
        let remaining = price - currentBalance
        guard remaining > 0, weeklyAllowance > 0 else { return nil }
        return Int(ceil(Double(truncating: remaining as NSNumber) / Double(truncating: weeklyAllowance as NSNumber)))
    }
}

// DTOs
struct CreateWishListItemDto: Codable {
    let childId: UUID
    let name: String
    let price: Decimal
    let url: String?
    let notes: String?
}

struct UpdateWishListItemDto: Codable {
    let name: String?
    let price: Decimal?
    let url: String?
    let notes: String?
}
```

**Service**:
```swift
// IWishListService.swift
protocol IWishListService {
    func getWishListItems(for childId: UUID) async throws -> [WishListItem]
    func createWishListItem(_ dto: CreateWishListItemDto) async throws -> WishListItem
    func updateWishListItem(id: UUID, dto: UpdateWishListItemDto) async throws -> WishListItem
    func deleteWishListItem(id: UUID) async throws
    func markAsPurchased(id: UUID) async throws -> WishListItem
    func markAsUnpurchased(id: UUID) async throws -> WishListItem
}

// WishListService.swift
final class WishListService: IWishListService {
    private let apiService: APIServiceProtocol

    init(apiService: APIServiceProtocol = APIService.shared) {
        self.apiService = apiService
    }

    func getWishListItems(for childId: UUID) async throws -> [WishListItem] {
        return try await apiService.request(
            endpoint: .wishList(childId: childId),
            method: .get,
            body: nil as String?
        )
    }

    func createWishListItem(_ dto: CreateWishListItemDto) async throws -> WishListItem {
        return try await apiService.request(
            endpoint: .createWishListItem,
            method: .post,
            body: dto
        )
    }

    func markAsPurchased(id: UUID) async throws -> WishListItem {
        return try await apiService.request(
            endpoint: .purchaseWishListItem(id),
            method: .post,
            body: nil as String?
        )
    }

    // Other methods...
}
```

**ViewModel & Views**:
```swift
// WishListViewModel.swift
@MainActor
final class WishListViewModel: ObservableObject {
    @Published var items: [WishListItem] = []
    @Published var isLoading = false
    @Published var errorMessage: String?
    @Published var showAddItem = false

    let childId: UUID
    private let wishListService: IWishListService

    init(
        childId: UUID,
        wishListService: IWishListService = WishListService.shared
    ) {
        self.childId = childId
        self.wishListService = wishListService
    }

    func loadItems() async {
        isLoading = true
        errorMessage = nil

        do {
            items = try await wishListService.getWishListItems(for: childId)
        } catch {
            errorMessage = error.localizedDescription
        }

        isLoading = false
    }

    func togglePurchase(_ item: WishListItem) async {
        do {
            let updated = item.isPurchased
                ? try await wishListService.markAsUnpurchased(id: item.id)
                : try await wishListService.markAsPurchased(id: item.id)

            if let index = items.firstIndex(where: { $0.id == item.id }) {
                items[index] = updated
            }
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func deleteItems(at offsets: IndexSet) async {
        for index in offsets {
            let item = items[index]
            do {
                try await wishListService.deleteWishListItem(id: item.id)
                items.remove(at: index)
            } catch {
                errorMessage = error.localizedDescription
            }
        }
    }
}

// WishListView.swift
struct WishListView: View {
    @StateObject private var viewModel: WishListViewModel
    let childId: UUID

    init(childId: UUID) {
        self.childId = childId
        _viewModel = StateObject(wrappedValue: WishListViewModel(childId: childId))
    }

    var body: some View {
        List {
            ForEach(viewModel.items) { item in
                WishListItemRow(item: item) {
                    await viewModel.togglePurchase(item)
                }
            }
            .onDelete { indexSet in
                Task {
                    await viewModel.deleteItems(at: indexSet)
                }
            }
        }
        .navigationTitle("Wish List")
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                Button {
                    viewModel.showAddItem = true
                } label: {
                    Image(systemName: "plus")
                }
            }
        }
        .sheet(isPresented: $viewModel.showAddItem) {
            AddWishListItemSheet(childId: childId)
        }
        .task {
            await viewModel.loadItems()
        }
        .refreshable {
            await viewModel.loadItems()
        }
    }
}

// WishListItemRow.swift
struct WishListItemRow: View {
    let item: WishListItem
    let onTogglePurchase: () async -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: DesignSystem.Spacing.sm) {
            HStack {
                Text(item.name)
                    .font(.headline)
                    .strikethrough(item.isPurchased)

                Spacer()

                Text(item.price.currencyFormatted)
                    .font(.headline)
                    .fontDesign(.monospaced)
                    .foregroundStyle(DesignSystem.Colors.primary)
            }

            if item.canAfford {
                Label("Can afford!", systemImage: "checkmark.circle.fill")
                    .font(.caption)
                    .foregroundStyle(DesignSystem.Colors.success)
            } else {
                // Progress bar
                ProgressView(value: item.progressPercentage(currentBalance: 0) / 100) {
                    Text("\(Int(item.progressPercentage(currentBalance: 0)))% saved")
                        .font(.caption)
                }
            }

            if let notes = item.notes, !notes.isEmpty {
                Text(notes)
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .swipeActions(edge: .trailing, allowsFullSwipe: false) {
            Button(item.isPurchased ? "Mark Unpurchased" : "Mark Purchased") {
                Task {
                    await onTogglePurchase()
                }
            }
            .tint(item.isPurchased ? .orange : .green)
        }
    }
}
```

**Tasks**:
- [ ] Write WishListItem model tests (6 tests)
- [ ] Implement WishListItem model
- [ ] Write WishListService tests (12 tests)
- [ ] Implement WishListService
- [ ] Write WishListViewModel tests (10 tests)
- [ ] Implement WishListViewModel
- [ ] Create WishListView
- [ ] Create WishListItemRow
- [ ] Create AddWishListItemSheet
- [ ] Create EditWishListItemSheet
- [ ] Write snapshot tests (4 tests)
- [ ] **Total: 32 tests passing**

---

### Phase 5: Savings Accounts Feature (Week 3-4)

**Goal**: Implement virtual savings accounts with auto-transfer

**Models**:
```swift
// SavingsAccount.swift
struct SavingsAccount: Codable, Identifiable, Equatable {
    let id: UUID
    let childId: UUID
    let name: String
    let targetAmount: Decimal?
    let currentBalance: Decimal
    let autoTransferEnabled: Bool
    let autoTransferPercentage: Decimal?
    let createdAt: Date
}

// SavingsTransaction.swift
struct SavingsTransaction: Codable, Identifiable, Equatable {
    let id: UUID
    let savingsAccountId: UUID
    let amount: Decimal
    let type: SavingsTransactionType
    let transferType: SavingsTransferType
    let balanceAfter: Decimal
    let notes: String?
    let createdAt: Date
}

enum SavingsTransactionType: String, Codable {
    case deposit = "Deposit"
    case withdrawal = "Withdrawal"
}

enum SavingsTransferType: String, Codable {
    case manual = "Manual"
    case autoTransfer = "AutoTransfer"
    case goal = "Goal"
}

// DTOs
struct CreateSavingsAccountDto: Codable {
    let childId: UUID
    let name: String
    let targetAmount: Decimal?
    let autoTransferEnabled: Bool
    let autoTransferPercentage: Decimal?
}
```

**Service & ViewModel**:
```swift
// ISavingsAccountService.swift
protocol ISavingsAccountService {
    func getSavingsAccounts(for childId: UUID) async throws -> [SavingsAccount]
    func createSavingsAccount(_ dto: CreateSavingsAccountDto) async throws -> SavingsAccount
    func deposit(accountId: UUID, amount: Decimal, notes: String?) async throws -> SavingsTransaction
    func withdraw(accountId: UUID, amount: Decimal, notes: String?) async throws -> SavingsTransaction
    func getTransactions(for accountId: UUID) async throws -> [SavingsTransaction]
}

// SavingsAccountViewModel.swift
@MainActor
final class SavingsAccountViewModel: ObservableObject {
    @Published var accounts: [SavingsAccount] = []
    @Published var isLoading = false
    @Published var errorMessage: String?

    let childId: UUID
    private let savingsService: ISavingsAccountService

    // Implementation similar to other ViewModels
}
```

**Tasks**:
- [ ] Write SavingsAccount model tests (5 tests)
- [ ] Implement SavingsAccount models
- [ ] Write SavingsAccountService tests (12 tests)
- [ ] Implement SavingsAccountService
- [ ] Write SavingsAccountViewModel tests (10 tests)
- [ ] Implement SavingsAccountViewModel
- [ ] Create SavingsAccountView
- [ ] Create SavingsAccountDetailView
- [ ] Create AddSavingsAccountSheet
- [ ] Create deposit/withdraw sheets
- [ ] **Total: 27 tests passing**

---

### Phase 6: Comprehensive Analytics Dashboard (Week 4)

**Goal**: Build complete analytics dashboard matching web app functionality

**Models**:
```swift
// BalancePoint.swift
struct BalancePoint: Codable, Identifiable {
    let id: UUID
    let date: Date
    let balance: Decimal
    let transactionDescription: String?
}

// IncomeSpendingSummary.swift
struct IncomeSpendingSummary: Codable {
    let totalIncome: Decimal
    let totalSpending: Decimal
    let netSavings: Decimal
    let incomeTransactionCount: Int
    let spendingTransactionCount: Int
    let savingsRate: Decimal
}

// MonthlyComparison.swift
struct MonthlyComparison: Codable, Identifiable {
    let id: UUID
    let year: Int
    let month: Int
    let monthName: String
    let income: Decimal
    let spending: Decimal
    let netSavings: Decimal
    let endingBalance: Decimal
}
```

**Service**:
```swift
// IAnalyticsService.swift
protocol IAnalyticsService {
    func getBalanceHistory(childId: UUID, days: Int) async throws -> [BalancePoint]
    func getIncomeVsSpending(childId: UUID, startDate: Date?, endDate: Date?) async throws -> IncomeSpendingSummary
    func getMonthlyComparison(childId: UUID, months: Int) async throws -> [MonthlyComparison]
}
```

**Views**:
```swift
// AnalyticsView.swift - Comprehensive dashboard
struct AnalyticsView: View {
    @StateObject private var viewModel: AnalyticsViewModel
    let childId: UUID?

    var body: some View {
        ScrollView {
            VStack(spacing: DesignSystem.Spacing.lg) {
                // Child selector if needed

                if let childId = viewModel.selectedChildId {
                    // Income vs Spending Summary Card
                    if let summary = viewModel.incomeSpending {
                        IncomeSpendingSummaryCard(summary: summary)
                    }

                    // Balance History Chart
                    if !viewModel.balanceHistory.isEmpty {
                        BalanceHistoryChartView(points: viewModel.balanceHistory)
                    }

                    // Spending Breakdown (use existing CategorySpendingView)
                    CategorySpendingView(childId: childId)

                    // Monthly Comparison
                    if !viewModel.monthlyComparison.isEmpty {
                        MonthlyComparisonView(months: viewModel.monthlyComparison)
                    }
                }
            }
            .padding()
        }
        .navigationTitle("Analytics")
        .task {
            if let childId = childId {
                await viewModel.loadAnalytics(for: childId)
            }
        }
        .refreshable {
            if let childId = childId {
                await viewModel.refresh(for: childId)
            }
        }
    }
}
```

**Tasks**:
- [ ] Write Analytics model tests (8 tests)
- [ ] Implement Analytics models
- [ ] Write AnalyticsService tests (10 tests)
- [ ] Implement AnalyticsService
- [ ] Write AnalyticsViewModel tests (12 tests)
- [ ] Implement AnalyticsViewModel (comprehensive)
- [ ] Create AnalyticsView
- [ ] Create BalanceHistoryChartView
- [ ] Create IncomeSpendingSummaryCard
- [ ] Create MonthlyComparisonView
- [ ] **Total: 30 tests passing**

---

### Phase 7: SignalR Real-time Updates (Week 4-5)

**Goal**: Add real-time balance updates and transaction notifications

**Dependencies**:
```swift
// Add to Package.swift
.package(url: "https://github.com/signalr/SignalR-Swift", from: "0.3.0")
```

**Service**:
```swift
// SignalRService.swift
import SignalRClient

final class SignalRService: ObservableObject {
    @Published var isConnected = false

    private var hubConnection: HubConnection?
    private let keychainService: KeychainServiceProtocol

    static let shared = SignalRService()

    init(keychainService: KeychainServiceProtocol = KeychainService.shared) {
        self.keychainService = keychainService
    }

    func connect(familyId: UUID) async throws {
        guard let token = try? keychainService.getToken() else {
            throw SignalRError.noToken
        }

        hubConnection = HubConnectionBuilder(url: AppConstants.API.signalRURL)
            .withLogging(logLevel: .debug)
            .withHttpConnectionOptions { options in
                options.accessTokenProvider = { token }
            }
            .withAutoReconnect()
            .build()

        // Register event handlers
        hubConnection?.on(method: "TransactionCreated") { [weak self] (transactionId: String) in
            await self?.handleTransactionCreated(transactionId: transactionId)
        }

        hubConnection?.on(method: "BalanceUpdated") { [weak self] (childId: String, newBalance: Decimal) in
            await self?.handleBalanceUpdated(childId: childId, balance: newBalance)
        }

        try await hubConnection?.start()
        isConnected = true
    }

    func disconnect() async {
        await hubConnection?.stop()
        isConnected = false
    }

    private func handleTransactionCreated(transactionId: String) async {
        NotificationCenter.default.post(
            name: .transactionCreated,
            object: nil,
            userInfo: ["transactionId": transactionId]
        )
    }

    private func handleBalanceUpdated(childId: String, balance: Decimal) async {
        NotificationCenter.default.post(
            name: .balanceUpdated,
            object: nil,
            userInfo: ["childId": childId, "balance": balance]
        )
    }
}

// Notification names
extension Notification.Name {
    static let transactionCreated = Notification.Name("transactionCreated")
    static let balanceUpdated = Notification.Name("balanceUpdated")
}

enum SignalRError: Error {
    case noToken
    case connectionFailed
}
```

**Integration**:
```swift
// Update DashboardViewModel to listen for real-time updates
@MainActor
final class DashboardViewModel: ObservableObject {
    // ... existing code ...

    private var cancellables = Set<AnyCancellable>()

    init(...) {
        // ... existing init ...

        // Subscribe to balance updates
        NotificationCenter.default.publisher(for: .balanceUpdated)
            .sink { [weak self] notification in
                if let childId = notification.userInfo?["childId"] as? String,
                   let balance = notification.userInfo?["balance"] as? Decimal {
                    self?.updateChildBalance(childId: UUID(uuidString: childId)!, balance: balance)
                }
            }
            .store(in: &cancellables)
    }

    private func updateChildBalance(childId: UUID, balance: Decimal) {
        if let index = children.firstIndex(where: { $0.id == childId }) {
            children[index].currentBalance = balance
        }
    }
}
```

**Tasks**:
- [ ] Add SignalRClient package dependency
- [ ] Write SignalRService tests (8 tests - mock connection)
- [ ] Implement SignalRService
- [ ] Update DashboardViewModel for real-time updates
- [ ] Update BalanceDisplayView for real-time updates
- [ ] Add connection status indicator
- [ ] Handle reconnection scenarios
- [ ] **Total: 8 tests passing**

---

### Phase 8: Navigation & Polish (Week 5)

**Goal**: Complete app navigation structure and polish UI

**Navigation Structure**:
```swift
// MainTabView.swift
struct MainTabView: View {
    @EnvironmentObject var authViewModel: AuthViewModel
    @State private var selectedTab = 0

    var body: some View {
        TabView(selection: $selectedTab) {
            // Tab 1: Dashboard
            DashboardView(authViewModel: authViewModel)
                .tabItem {
                    Label("Dashboard", systemImage: "house.fill")
                }
                .tag(0)

            // Tab 2: Transactions (if child role)
            if authViewModel.currentUser?.role == .child {
                TransactionListView(childId: authViewModel.currentUser!.childProfile!.id)
                    .tabItem {
                        Label("Transactions", systemImage: "list.bullet.rectangle")
                    }
                    .tag(1)
            }

            // Tab 3: Analytics
            AnalyticsView(childId: nil) // Will select child
                .tabItem {
                    Label("Analytics", systemImage: "chart.bar.fill")
                }
                .tag(2)

            // Tab 4: Profile
            ProfileView()
                .tabItem {
                    Label("Profile", systemImage: "person.fill")
                }
                .tag(3)
        }
    }
}

// ProfileView.swift
struct ProfileView: View {
    @EnvironmentObject var authViewModel: AuthViewModel

    var body: some View {
        NavigationStack {
            List {
                Section("Account") {
                    if let user = authViewModel.currentUser {
                        HStack {
                            Text("Name")
                            Spacer()
                            Text(user.fullName)
                                .foregroundStyle(.secondary)
                        }

                        HStack {
                            Text("Email")
                            Spacer()
                            Text(user.email)
                                .foregroundStyle(.secondary)
                        }

                        HStack {
                            Text("Role")
                            Spacer()
                            Text(user.role.rawValue)
                                .foregroundStyle(.secondary)
                        }
                    }
                }

                Section("Settings") {
                    NavigationLink {
                        // Settings view
                    } label: {
                        Label("Preferences", systemImage: "gear")
                    }

                    NavigationLink {
                        // About view
                    } label: {
                        Label("About", systemImage: "info.circle")
                    }
                }

                Section {
                    Button(role: .destructive) {
                        authViewModel.logout()
                    } label: {
                        Label("Logout", systemImage: "arrow.right.square")
                    }
                }
            }
            .navigationTitle("Profile")
        }
    }
}

// Update AllowanceTrackerApp.swift
@main
struct AllowanceTrackerApp: App {
    @StateObject private var authViewModel = AuthViewModel()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(authViewModel)
        }
    }
}

// Update ContentView.swift
struct ContentView: View {
    @EnvironmentObject var authViewModel: AuthViewModel

    var body: some View {
        if authViewModel.isAuthenticated {
            MainTabView()
        } else {
            LoginView()
        }
    }
}
```

**Polish Tasks**:
- [ ] Create MainTabView with navigation
- [ ] Create ProfileView
- [ ] Add loading states to all views
- [ ] Add error handling with alerts
- [ ] Add pull-to-refresh to all lists
- [ ] Add empty state views
- [ ] Implement proper accessibility labels
- [ ] Add VoiceOver support
- [ ] Test on different screen sizes
- [ ] Add haptic feedback
- [ ] Polish animations and transitions
- [ ] Add app icon and launch screen
- [ ] **Total: ~15 polish items**

---

## Testing Strategy

### Test Coverage Goals

**Target**: >80% overall coverage

**Critical Areas** (must have >90% coverage):
- Authentication (JWT, Keychain)
- Transaction creation and balance updates
- Savings account operations
- Wish list affordability calculations
- Budget tracking and warnings
- API service requests and error handling

### Test Types

#### 1. Unit Tests
```swift
// Model Tests
- Child model calculations
- Transaction amount formatting
- WishListItem progress calculations
- Budget threshold checks

// Service Tests
- API service request/response handling
- Transaction service business logic
- WishList service CRUD operations
- Savings account service operations
- SignalR service connection and events

// ViewModel Tests
- State management
- Async data loading
- Error handling
- Validation logic
```

#### 2. Integration Tests
```swift
// API Integration
- End-to-end transaction creation
- Balance update propagation
- Real API error scenarios
- Authentication flow
```

#### 3. Snapshot Tests
```swift
// UI Component Tests
- ChildCardView variations
- TransactionRow states
- WishListItemRow progress states
- Balance display with different amounts
- Error states
- Loading states
```

#### 4. UI Tests (Basic)
```swift
// Critical User Flows
- Login and logout
- Create transaction
- Add wish list item
- View analytics
- Navigation between tabs
```

### Test Organization

```
Tests/
├── Unit/
│   ├── ModelTests/
│   │   ├── TransactionTests.swift
│   │   ├── WishListItemTests.swift
│   │   └── SavingsAccountTests.swift
│   ├── ServiceTests/
│   │   ├── TransactionServiceTests.swift
│   │   ├── WishListServiceTests.swift
│   │   ├── SavingsAccountServiceTests.swift
│   │   └── SignalRServiceTests.swift
│   └── ViewModelTests/
│       ├── DashboardViewModelTests.swift
│       ├── TransactionViewModelTests.swift
│       ├── WishListViewModelTests.swift
│       └── AnalyticsViewModelTests.swift
├── Integration/
│   └── APIIntegrationTests.swift
├── Snapshot/
│   ├── ChildCardSnapshotTests.swift
│   ├── TransactionRowSnapshotTests.swift
│   └── WishListItemSnapshotTests.swift
└── UI/
    └── MainFlowUITests.swift
```

### Test Utilities

```swift
// MockAPIService.swift
final class MockAPIService: APIServiceProtocol {
    var mockResponse: Any?
    var shouldFail = false
    var requestHistory: [(endpoint: String, method: String)] = []

    func request<T: Decodable>(
        endpoint: Endpoint,
        method: HTTPMethod,
        body: Encodable?
    ) async throws -> T {
        requestHistory.append((endpoint.path, method.rawValue))

        if shouldFail {
            throw APIError.httpError(500)
        }

        guard let response = mockResponse as? T else {
            throw APIError.decodingError
        }

        return response
    }
}

// TestBuilders.swift
struct ChildBuilder {
    var id = UUID()
    var firstName = "Test"
    var lastName = "Child"
    var weeklyAllowance: Decimal = 10.00
    var currentBalance: Decimal = 50.00

    func build() -> ChildDto {
        ChildDto(
            id: id,
            firstName: firstName,
            lastName: lastName,
            weeklyAllowance: weeklyAllowance,
            currentBalance: currentBalance,
            lastAllowanceDate: nil
        )
    }
}

// Usage in tests
let child = ChildBuilder()
    .with(\.currentBalance, 100.00)
    .with(\.weeklyAllowance, 15.00)
    .build()
```

---

## Design Considerations

### iOS-Specific Patterns

1. **SwiftUI Over UIKit**: Use SwiftUI exclusively for modern, declarative UI
2. **Async/Await**: Use Swift concurrency for all async operations
3. **MVVM Architecture**: Strict separation of concerns
4. **Protocol-Oriented**: Use protocols for services for testability
5. **Combine or Observation**: Use for reactive updates (prefer Observation for iOS 17+)

### Platform Differences from Web App

1. **Navigation**: TabView instead of sidebar navigation
2. **Forms**: Use native Form style instead of web forms
3. **Real-time**: SignalR client instead of built-in SignalR
4. **Modals**: Use sheets instead of modal overlays
5. **Pull-to-Refresh**: Use refreshable modifier
6. **Haptics**: Add haptic feedback for actions
7. **Gestures**: Swipe actions for delete/edit
8. **Safe Areas**: Respect iOS safe areas
9. **Dynamic Type**: Support all text sizes
10. **Dark Mode**: Full dark mode support

### Design System Integration

**Already Implemented** (in DesignSystem.swift):
- Color palette
- Typography scale
- Button styles
- Spacing constants

**Needs Enhancement**:
```swift
// Add to DesignSystem.swift

// Card Styles
extension View {
    func cardStyle() -> some View {
        self
            .padding()
            .background(Color(.systemBackground))
            .clipShape(RoundedRectangle(cornerRadius: DesignSystem.CornerRadius.medium))
            .shadow(color: .black.opacity(0.1), radius: 4, x: 0, y: 2)
    }
}

// Section Header Style
struct SectionHeaderStyle: ViewModifier {
    func body(content: Content) -> some View {
        content
            .font(.headline)
            .foregroundStyle(DesignSystem.Colors.textSecondary)
            .textCase(.uppercase)
            .padding(.horizontal)
            .padding(.vertical, DesignSystem.Spacing.sm)
    }
}

// Loading Overlay
struct LoadingOverlay: View {
    var body: some View {
        ZStack {
            Color.black.opacity(0.3)
                .ignoresSafeArea()

            ProgressView()
                .scaleEffect(1.5)
                .padding()
                .background(.regularMaterial)
                .clipShape(RoundedRectangle(cornerRadius: 12))
        }
    }
}
```

### Accessibility

1. **VoiceOver Labels**: All interactive elements
2. **Dynamic Type**: Support all text sizes
3. **Color Contrast**: WCAG AA compliance
4. **Reduce Motion**: Respect accessibility settings
5. **Alternative Indicators**: Don't rely solely on color

### Performance

1. **Lazy Loading**: Use LazyVStack/LazyVGrid for lists
2. **Image Caching**: Cache profile images and icons
3. **Debouncing**: Debounce search and filter inputs
4. **Pagination**: Implement for transaction history
5. **Background Refresh**: Keep data fresh when app returns to foreground

---

## Dependencies

### Swift Packages

```swift
// Package.swift dependencies

dependencies: [
    // SignalR for real-time updates
    .package(
        url: "https://github.com/signalr/SignalR-Swift",
        from: "0.3.0"
    ),

    // Snapshot testing (dev only)
    .package(
        url: "https://github.com/pointfreeco/swift-snapshot-testing",
        from: "1.15.0"
    )
]
```

### API Endpoints

**Ensure these endpoints are implemented** (reference web app API controllers):

```swift
// Update Endpoints.swift

enum Endpoint {
    // Existing auth endpoints...

    // Transactions
    case createTransaction
    case transaction(UUID)
    case childTransactions(UUID, limit: Int?)

    // WishList
    case wishList(childId: UUID)
    case createWishListItem
    case wishListItem(UUID)
    case updateWishListItem(UUID)
    case deleteWishListItem(UUID)
    case purchaseWishListItem(UUID)
    case unpurchaseWishListItem(UUID)

    // Savings
    case savingsAccounts(childId: UUID)
    case createSavingsAccount
    case savingsAccount(UUID)
    case savingsDeposit(accountId: UUID)
    case savingsWithdraw(accountId: UUID)
    case savingsTransactions(accountId: UUID)

    // Analytics
    case balanceHistory(childId: UUID, days: Int)
    case incomeVsSpending(childId: UUID, startDate: Date?, endDate: Date?)
    case monthlyComparison(childId: UUID, months: Int)

    // Family & Children
    case family
    case familyMembers
    case children
    case createChild
    case child(UUID)
    case updateChild(UUID)
    case deleteChild(UUID)

    var path: String {
        switch self {
        // ... implementation
        }
    }
}
```

---

## Success Metrics

### Functionality
- ✅ All 9 web app features implemented
- ✅ Real-time updates working
- ✅ Offline caching for core data
- ✅ <2 second app launch time
- ✅ <200ms API response handling

### Quality
- ✅ >80% test coverage overall
- ✅ >90% coverage for critical paths
- ✅ Zero crashes in testing
- ✅ All accessibility checks pass
- ✅ Performance: 60 FPS scrolling

### User Experience
- ✅ Intuitive navigation
- ✅ Consistent with iOS design guidelines
- ✅ Works on all iOS devices (iPhone, iPad)
- ✅ Supports iOS 17.0+
- ✅ Full dark mode support

---

## Risk Mitigation

### Technical Risks

1. **SignalR Connectivity**
   - Risk: Connection drops on poor network
   - Mitigation: Implement automatic reconnection, show connection status, fallback to polling

2. **Real-time State Synchronization**
   - Risk: Local state gets out of sync with server
   - Mitigation: Implement refresh on app foreground, periodic sync checks

3. **Test Environment Setup**
   - Risk: Complex mock setup for API and SignalR
   - Mitigation: Create comprehensive mock services, use dependency injection everywhere

4. **Performance with Large Data Sets**
   - Risk: Slow rendering with many transactions
   - Mitigation: Implement pagination, lazy loading, virtualized lists

### Timeline Risks

1. **SignalR Integration Complexity**
   - Buffer: Allocate extra time for Phase 7
   - Alternative: Ship without real-time first, add in v1.1

2. **Analytics Chart Complexity**
   - Buffer: Use simple bars/lines first, enhance later
   - Alternative: Ship basic analytics, add advanced charts in v1.1

---

## Implementation Timeline

### Week 1: Foundation
- **Days 1-2**: Phase 1 - Transaction Foundation
- **Days 3-5**: Phase 2 - Dashboard & Child Cards

### Week 2: Core Features
- **Days 1-3**: Phase 3 - Transaction UI & Forms
- **Days 4-5**: Phase 4 - Wish List (start)

### Week 3: Advanced Features
- **Days 1-2**: Phase 4 - Wish List (complete)
- **Days 3-5**: Phase 5 - Savings Accounts

### Week 4: Analytics & Real-time
- **Days 1-3**: Phase 6 - Comprehensive Analytics
- **Days 4-5**: Phase 7 - SignalR Real-time

### Week 5: Polish & Ship
- **Days 1-3**: Phase 8 - Navigation & Polish
- **Days 4-5**: Final testing, bug fixes, TestFlight

**Total: 5 Weeks to Parity**

---

## Next Steps

### Immediate Actions

1. **Set up project structure**
   - Create Models folder structure
   - Create Services folder structure
   - Create ViewModels folder structure
   - Create Views folder structure
   - Create Tests folder structure

2. **Start Phase 1: Transaction Foundation**
   - Write Transaction model tests
   - Implement Transaction models
   - Write TransactionService tests
   - Implement TransactionService
   - Update API Endpoints

3. **Continuous Integration**
   - Set up CI for running tests
   - Add code coverage reporting
   - Set up snapshot test baselines

### Long-term Considerations

**Post-Parity Features** (v1.1+):
- Push notifications for transactions
- Widgets for balance at-a-glance
- Face ID / Touch ID for app lock
- Export transaction history
- Custom categories
- Recurring transactions
- Photo attachments for transactions
- Multi-family support

---

## Conclusion

This specification provides a complete roadmap for bringing the iOS app to feature parity with the web application. By following the phased approach with strict TDD methodology, the iOS app will achieve:

1. **Complete feature parity** with all 9 web app features
2. **High test coverage** (>80%) ensuring reliability
3. **Modern iOS architecture** following best practices
4. **Excellent user experience** with native iOS patterns
5. **Real-time updates** via SignalR integration
6. **Production-ready quality** with comprehensive testing

The 8-phase implementation plan provides clear milestones and can be executed over approximately 5 weeks by following TDD practices and maintaining focus on one feature at a time.

**Status**: Ready for implementation
**Estimated Effort**: 5 weeks full-time development
**Test Target**: >200 tests total (150+ new tests)
**Lines of Code**: ~8,000-10,000 new Swift code
