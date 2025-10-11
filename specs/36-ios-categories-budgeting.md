# iOS Transaction Categories & Budgeting

## Overview

Native iOS implementation of Transaction Categories & Budgeting feature for Allowance Tracker. Provides parents with mobile tools to manage category-based budgets and view spending analytics by category.

**Related Specs**:
- Backend: `12-transaction-categories.md`
- iOS Base: `08-ios-app-specification.md`
- API: `03-api-specification.md`

## Models

### TransactionCategory Enum
```swift
enum TransactionCategory: String, Codable, CaseIterable, Identifiable {
    // Income Categories
    case allowance = "Allowance"
    case chores = "Chores"
    case gift = "Gift"
    case bonusReward = "BonusReward"
    case otherIncome = "OtherIncome"

    // Spending Categories
    case toys = "Toys"
    case games = "Games"
    case books = "Books"
    case clothes = "Clothes"
    case snacks = "Snacks"
    case candy = "Candy"
    case electronics = "Electronics"
    case entertainment = "Entertainment"
    case sports = "Sports"
    case crafts = "Crafts"
    case savings = "Savings"
    case charity = "Charity"
    case otherSpending = "OtherSpending"

    var id: String { rawValue }

    var displayName: String {
        // Convert camelCase to Title Case
        rawValue.replacingOccurrences(
            of: "([a-z])([A-Z])",
            with: "$1 $2",
            options: .regularExpression
        )
    }

    var icon: String {
        switch self {
        case .allowance: return "dollarsign.circle.fill"
        case .chores: return "list.bullet.clipboard.fill"
        case .gift: return "gift.fill"
        case .bonusReward: return "star.fill"
        case .otherIncome: return "plus.circle.fill"
        case .toys: return "teddybear.fill"
        case .games: return "gamecontroller.fill"
        case .books: return "book.fill"
        case .clothes: return "tshirt.fill"
        case .snacks: return "fork.knife"
        case .candy: return "birthday.cake.fill"
        case .electronics: return "iphone"
        case .entertainment: return "tv.fill"
        case .sports: return "sportscourt.fill"
        case .crafts: return "paintbrush.fill"
        case .savings: return "banknote.fill"
        case .charity: return "heart.fill"
        case .otherSpending: return "minus.circle.fill"
        }
    }

    var isIncome: Bool {
        [.allowance, .chores, .gift, .bonusReward, .otherIncome].contains(self)
    }

    static var incomeCategories: [TransactionCategory] {
        allCases.filter { $0.isIncome }
    }

    static var spendingCategories: [TransactionCategory] {
        allCases.filter { !$0.isIncome }
    }
}
```

### CategoryBudget Model
```swift
struct CategoryBudget: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let category: TransactionCategory
    var limit: Decimal
    var period: BudgetPeriod
    var alertThresholdPercent: Int
    var enforceLimit: Bool
    let createdAt: Date
    var updatedAt: Date

    var formattedLimit: String {
        limit.currencyFormatted
    }
}

enum BudgetPeriod: String, Codable, CaseIterable {
    case weekly = "Weekly"
    case monthly = "Monthly"

    var icon: String {
        switch self {
        case .weekly: return "calendar.badge.clock"
        case .monthly: return "calendar"
        }
    }
}
```

### CategorySpending Model
```swift
struct CategorySpending: Codable, Identifiable {
    let category: TransactionCategory
    let categoryName: String
    let totalAmount: Decimal
    let transactionCount: Int
    let percentage: Decimal

    var id: String { category.rawValue }

    var formattedAmount: String {
        totalAmount.currencyFormatted
    }
}
```

### CategoryBudgetStatus Model
```swift
struct CategoryBudgetStatus: Codable, Identifiable {
    let category: TransactionCategory
    let categoryName: String
    let budgetLimit: Decimal
    let currentSpending: Decimal
    let remaining: Decimal
    let percentUsed: Int
    let status: BudgetStatus
    let period: BudgetPeriod

    var id: String { category.rawValue }

    var progressColor: Color {
        switch status {
        case .safe: return .green
        case .warning: return .orange
        case .atLimit: return .red
        case .overBudget: return .red
        }
    }
}

enum BudgetStatus: String, Codable {
    case safe = "Safe"
    case warning = "Warning"
    case atLimit = "AtLimit"
    case overBudget = "OverBudget"
}
```

### BudgetCheckResult Model
```swift
struct BudgetCheckResult: Codable {
    let allowed: Bool
    let message: String
    let currentSpending: Decimal
    let budgetLimit: Decimal
    let remainingAfter: Decimal
}
```

## API Integration

### Category Endpoints
```swift
extension Endpoint {
    // Category endpoints
    case getCategories(type: TransactionType)
    case getAllCategories
    case getCategoryDisplayName(TransactionCategory)

    // Budget endpoints
    case getBudgets(childId: UUID)
    case getBudget(childId: UUID, category: TransactionCategory)
    case setBudget
    case deleteBudget(childId: UUID, category: TransactionCategory)

    // Analytics endpoints
    case categorySpending(childId: UUID, startDate: Date?, endDate: Date?)
    case budgetStatus(childId: UUID, period: BudgetPeriod)
    case checkBudget(childId: UUID, category: TransactionCategory, amount: Decimal)

    var path: String {
        switch self {
        case .getCategories(let type):
            return "/api/v1/categories?type=\(type.rawValue)"
        case .getAllCategories:
            return "/api/v1/categories/all"
        case .getCategoryDisplayName(let category):
            return "/api/v1/categories/\(category.rawValue)/display-name"
        case .getBudgets(let childId):
            return "/api/v1/children/\(childId.uuidString)/budgets"
        case .getBudget(let childId, let category):
            return "/api/v1/children/\(childId.uuidString)/budgets/\(category.rawValue)"
        case .setBudget:
            return "/api/v1/budgets"
        case .deleteBudget(let childId, let category):
            return "/api/v1/children/\(childId.uuidString)/budgets/\(category.rawValue)"
        case .categorySpending(let childId, let startDate, let endDate):
            var path = "/api/v1/categories/spending/children/\(childId.uuidString)"
            var params: [String] = []
            if let start = startDate {
                params.append("startDate=\(start.iso8601)")
            }
            if let end = endDate {
                params.append("endDate=\(end.iso8601)")
            }
            if !params.isEmpty {
                path += "?" + params.joined(separator: "&")
            }
            return path
        case .budgetStatus(let childId, let period):
            return "/api/v1/categories/budget-status/children/\(childId.uuidString)?period=\(period.rawValue)"
        case .checkBudget(let childId, let category, let amount):
            return "/api/v1/categories/check-budget/children/\(childId.uuidString)?category=\(category.rawValue)&amount=\(amount)"
        }
    }
}
```

### CategoryService
```swift
@MainActor
final class CategoryService: ObservableObject {
    private let apiService: APIService

    init(apiService: APIService = .shared) {
        self.apiService = apiService
    }

    func getCategories(for type: TransactionType) async throws -> [TransactionCategory] {
        try await apiService.request(
            endpoint: .getCategories(type: type),
            method: .get,
            body: nil as String?
        )
    }

    func getCategorySpending(
        for childId: UUID,
        startDate: Date? = nil,
        endDate: Date? = nil
    ) async throws -> [CategorySpending] {
        try await apiService.request(
            endpoint: .categorySpending(childId: childId, startDate: startDate, endDate: endDate),
            method: .get,
            body: nil as String?
        )
    }

    func getBudgetStatus(
        for childId: UUID,
        period: BudgetPeriod
    ) async throws -> [CategoryBudgetStatus] {
        try await apiService.request(
            endpoint: .budgetStatus(childId: childId, period: period),
            method: .get,
            body: nil as String?
        )
    }

    func checkBudget(
        for childId: UUID,
        category: TransactionCategory,
        amount: Decimal
    ) async throws -> BudgetCheckResult {
        try await apiService.request(
            endpoint: .checkBudget(childId: childId, category: category, amount: amount),
            method: .get,
            body: nil as String?
        )
    }
}
```

### BudgetService
```swift
@MainActor
final class BudgetService: ObservableObject {
    private let apiService: APIService

    init(apiService: APIService = .shared) {
        self.apiService = apiService
    }

    func getBudgets(for childId: UUID) async throws -> [CategoryBudget] {
        try await apiService.request(
            endpoint: .getBudgets(childId: childId),
            method: .get,
            body: nil as String?
        )
    }

    func getBudget(
        for childId: UUID,
        category: TransactionCategory
    ) async throws -> CategoryBudget? {
        try? await apiService.request(
            endpoint: .getBudget(childId: childId, category: category),
            method: .get,
            body: nil as String?
        )
    }

    func setBudget(_ budget: SetBudgetRequest) async throws -> CategoryBudget {
        try await apiService.request(
            endpoint: .setBudget,
            method: .put,
            body: budget
        )
    }

    func deleteBudget(
        for childId: UUID,
        category: TransactionCategory
    ) async throws {
        let _: EmptyResponse = try await apiService.request(
            endpoint: .deleteBudget(childId: childId, category: category),
            method: .delete,
            body: nil as String?
        )
    }
}

struct SetBudgetRequest: Codable {
    let childId: UUID
    let category: TransactionCategory
    let limit: Decimal
    let period: BudgetPeriod
    let alertThresholdPercent: Int
    let enforceLimit: Bool

    init(
        childId: UUID,
        category: TransactionCategory,
        limit: Decimal,
        period: BudgetPeriod,
        alertThresholdPercent: Int = 80,
        enforceLimit: Bool = false
    ) {
        self.childId = childId
        self.category = category
        self.limit = limit
        self.period = period
        self.alertThresholdPercent = alertThresholdPercent
        self.enforceLimit = enforceLimit
    }
}

struct EmptyResponse: Codable {}
```

## UI Components

### 1. Category Picker
```swift
struct CategoryPicker: View {
    @Binding var selectedCategory: TransactionCategory
    let transactionType: TransactionType

    private var categories: [TransactionCategory] {
        transactionType == .credit
            ? TransactionCategory.incomeCategories
            : TransactionCategory.spendingCategories
    }

    var body: some View {
        Picker("Category", selection: $selectedCategory) {
            ForEach(categories) { category in
                Label {
                    Text(category.displayName)
                } icon: {
                    Image(systemName: category.icon)
                }
                .tag(category)
            }
        }
        .pickerStyle(.menu)
    }
}
```

### 2. Budget Card View
```swift
struct BudgetCardView: View {
    let budget: CategoryBudget
    let status: CategoryBudgetStatus?
    let onEdit: () -> Void
    let onDelete: () -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            // Header
            HStack {
                Label {
                    Text(budget.category.displayName)
                        .font(.headline)
                } icon: {
                    Image(systemName: budget.category.icon)
                        .foregroundStyle(.blue)
                }

                Spacer()

                Badge(budget.enforceLimit ? "Enforced" : "Warning Only")
            }

            // Budget Info
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Budget Limit")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text(budget.formattedLimit)
                        .font(.title3)
                        .fontWeight(.semibold)
                        .fontDesign(.monospaced)
                }

                Spacer()

                VStack(alignment: .trailing, spacing: 4) {
                    Text(budget.period.rawValue)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Image(systemName: budget.period.icon)
                        .font(.title3)
                }
            }

            // Status (if available)
            if let status = status {
                Divider()

                VStack(spacing: 8) {
                    HStack {
                        Text("Spent:")
                            .font(.caption)
                        Spacer()
                        Text(status.currentSpending.currencyFormatted)
                            .fontDesign(.monospaced)
                    }

                    HStack {
                        Text("Remaining:")
                            .font(.caption)
                        Spacer()
                        Text(status.remaining.currencyFormatted)
                            .fontDesign(.monospaced)
                            .foregroundStyle(status.progressColor)
                    }

                    // Progress Bar
                    ProgressView(value: Double(status.percentUsed), total: 100)
                        .tint(status.progressColor)
                        .overlay(alignment: .trailing) {
                            Text("\(status.percentUsed)%")
                                .font(.caption2)
                                .foregroundStyle(.secondary)
                                .offset(y: 15)
                        }
                }
            }

            // Actions
            HStack(spacing: 8) {
                Button(action: onEdit) {
                    Label("Edit", systemImage: "pencil")
                        .font(.caption)
                }
                .buttonStyle(.bordered)
                .controlSize(.small)

                Button(role: .destructive, action: onDelete) {
                    Label("Delete", systemImage: "trash")
                        .font(.caption)
                }
                .buttonStyle(.bordered)
                .controlSize(.small)
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(12)
        .shadow(radius: 2)
    }
}

struct Badge: View {
    let text: String

    init(_ text: String) {
        self.text = text
    }

    var body: some View {
        Text(text)
            .font(.caption2)
            .fontWeight(.medium)
            .padding(.horizontal, 8)
            .padding(.vertical, 4)
            .background(Color.blue.opacity(0.1))
            .foregroundStyle(.blue)
            .cornerRadius(8)
    }
}
```

### 3. Category Spending Chart
```swift
import Charts

struct CategorySpendingChart: View {
    let spending: [CategorySpending]

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Spending by Category")
                .font(.headline)

            if spending.isEmpty {
                ContentUnavailableView(
                    "No Spending Data",
                    systemImage: "chart.bar.xaxis",
                    description: Text("Spending will appear here")
                )
                .frame(height: 200)
            } else {
                Chart(spending.prefix(8)) { item in
                    BarMark(
                        x: .value("Amount", item.totalAmount as NSDecimalNumber),
                        y: .value("Category", item.categoryName)
                    )
                    .foregroundStyle(by: .value("Category", item.categoryName))
                }
                .frame(height: CGFloat(spending.prefix(8).count * 40))
                .chartLegend(.hidden)

                // Details List
                ForEach(spending.prefix(8)) { item in
                    HStack {
                        Label {
                            Text(item.categoryName)
                                .font(.subheadline)
                        } icon: {
                            Image(systemName: item.category.icon)
                                .foregroundStyle(.blue)
                        }

                        Spacer()

                        VStack(alignment: .trailing, spacing: 2) {
                            Text(item.formattedAmount)
                                .font(.subheadline)
                                .fontWeight(.semibold)
                                .fontDesign(.monospaced)

                            Text("\(item.percentage.formatted(.number.precision(.fractionLength(1))))%")
                                .font(.caption2)
                                .foregroundStyle(.secondary)
                        }
                    }
                    .padding(.vertical, 4)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(12)
        .shadow(radius: 2)
    }
}
```

### 4. Budget Warning Alert
```swift
struct BudgetWarningView: View {
    let result: BudgetCheckResult

    var body: some View {
        HStack(spacing: 12) {
            Image(systemName: result.allowed ? "exclamationmark.triangle.fill" : "xmark.circle.fill")
                .foregroundStyle(result.allowed ? .orange : .red)
                .font(.title2)

            VStack(alignment: .leading, spacing: 4) {
                Text(result.allowed ? "Budget Warning" : "Budget Exceeded")
                    .font(.headline)

                Text(result.message)
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Spacer()
        }
        .padding()
        .background(
            (result.allowed ? Color.orange : Color.red)
                .opacity(0.1)
        )
        .cornerRadius(8)
    }
}
```

## View Models

### BudgetViewModel
```swift
@MainActor
final class BudgetViewModel: ObservableObject {
    @Published var budgets: [CategoryBudget] = []
    @Published var budgetStatuses: [CategoryBudgetStatus] = []
    @Published var isLoading = false
    @Published var errorMessage: String?
    @Published var showAddBudget = false

    private let budgetService: BudgetService
    private let categoryService: CategoryService

    init(
        budgetService: BudgetService = .init(),
        categoryService: CategoryService = .init()
    ) {
        self.budgetService = budgetService
        self.categoryService = categoryService
    }

    func loadBudgets(for childId: UUID) async {
        isLoading = true
        errorMessage = nil

        do {
            async let budgetsTask = budgetService.getBudgets(for: childId)
            async let weeklyStatusTask = categoryService.getBudgetStatus(for: childId, period: .weekly)
            async let monthlyStatusTask = categoryService.getBudgetStatus(for: childId, period: .monthly)

            budgets = try await budgetsTask
            let weeklyStatus = try await weeklyStatusTask
            let monthlyStatus = try await monthlyStatusTask
            budgetStatuses = weeklyStatus + monthlyStatus

        } catch {
            errorMessage = error.localizedDescription
        }

        isLoading = false
    }

    func createBudget(_ request: SetBudgetRequest) async -> Bool {
        do {
            let budget = try await budgetService.setBudget(request)
            budgets.append(budget)
            return true
        } catch {
            errorMessage = error.localizedDescription
            return false
        }
    }

    func updateBudget(_ request: SetBudgetRequest) async -> Bool {
        do {
            let updated = try await budgetService.setBudget(request)
            if let index = budgets.firstIndex(where: { $0.id == updated.id }) {
                budgets[index] = updated
            }
            return true
        } catch {
            errorMessage = error.localizedDescription
            return false
        }
    }

    func deleteBudget(_ budget: CategoryBudget) async -> Bool {
        do {
            try await budgetService.deleteBudget(for: budget.childId, category: budget.category)
            budgets.removeAll { $0.id == budget.id }
            return true
        } catch {
            errorMessage = error.localizedDescription
            return false
        }
    }

    func getStatus(for budget: CategoryBudget) -> CategoryBudgetStatus? {
        budgetStatuses.first {
            $0.category == budget.category && $0.period == budget.period
        }
    }
}
```

## Screens

### 1. Budget Management View
```swift
struct BudgetManagementView: View {
    @StateObject private var viewModel = BudgetViewModel()
    let child: Child

    var body: some View {
        List {
            if viewModel.budgets.isEmpty {
                ContentUnavailableView(
                    "No Budgets Set",
                    systemImage: "calendar.badge.exclamationmark",
                    description: Text("Create a budget to start tracking spending")
                )
            } else {
                ForEach(viewModel.budgets) { budget in
                    BudgetCardView(
                        budget: budget,
                        status: viewModel.getStatus(for: budget),
                        onEdit: {
                            // TODO: Show edit sheet
                        },
                        onDelete: {
                            Task {
                                await viewModel.deleteBudget(budget)
                            }
                        }
                    )
                    .listRowInsets(EdgeInsets())
                    .listRowBackground(Color.clear)
                }
            }
        }
        .listStyle(.plain)
        .navigationTitle("Budget Management")
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                Button {
                    viewModel.showAddBudget = true
                } label: {
                    Image(systemName: "plus")
                }
            }
        }
        .sheet(isPresented: $viewModel.showAddBudget) {
            AddBudgetSheet(childId: child.id)
        }
        .task {
            await viewModel.loadBudgets(for: child.id)
        }
        .refreshable {
            await viewModel.loadBudgets(for: child.id)
        }
    }
}
```

### 2. Add/Edit Budget Sheet
```swift
struct AddBudgetSheet: View {
    @Environment(\.dismiss) private var dismiss
    @StateObject private var viewModel = BudgetViewModel()

    let childId: UUID
    var existingBudget: CategoryBudget?

    @State private var selectedCategory: TransactionCategory = .toys
    @State private var limit: Decimal = 50
    @State private var period: BudgetPeriod = .weekly
    @State private var alertThreshold: Int = 80
    @State private var enforceLimit = false

    var body: some View {
        NavigationStack {
            Form {
                Section("Budget Details") {
                    Picker("Category", selection: $selectedCategory) {
                        ForEach(TransactionCategory.spendingCategories) { category in
                            Label {
                                Text(category.displayName)
                            } icon: {
                                Image(systemName: category.icon)
                            }
                            .tag(category)
                        }
                    }

                    HStack {
                        Text("Limit")
                        Spacer()
                        TextField("Amount", value: $limit, format: .currency(code: "USD"))
                            .keyboardType(.decimalPad)
                            .multilineTextAlignment(.trailing)
                            .fontDesign(.monospaced)
                    }

                    Picker("Period", selection: $period) {
                        ForEach(BudgetPeriod.allCases, id: \.self) { period in
                            Label {
                                Text(period.rawValue)
                            } icon: {
                                Image(systemName: period.icon)
                            }
                            .tag(period)
                        }
                    }
                }

                Section("Alert Settings") {
                    Stepper("Alert at \(alertThreshold)% used", value: $alertThreshold, in: 50...95, step: 5)

                    Toggle("Enforce Limit", isOn: $enforceLimit)
                }

                Section {
                    Text(enforceLimit
                        ? "Transactions will be blocked when budget is exceeded"
                        : "Warnings will be shown, but transactions are allowed"
                    )
                    .font(.caption)
                    .foregroundStyle(.secondary)
                }
            }
            .navigationTitle(existingBudget == nil ? "Add Budget" : "Edit Budget")
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
                            let request = SetBudgetRequest(
                                childId: childId,
                                category: selectedCategory,
                                limit: limit,
                                period: period,
                                alertThresholdPercent: alertThreshold,
                                enforceLimit: enforceLimit
                            )

                            if await viewModel.createBudget(request) {
                                dismiss()
                            }
                        }
                    }
                    .disabled(limit <= 0)
                }
            }
        }
        .onAppear {
            if let budget = existingBudget {
                selectedCategory = budget.category
                limit = budget.limit
                period = budget.period
                alertThreshold = budget.alertThresholdPercent
                enforceLimit = budget.enforceLimit
            }
        }
    }
}
```

### 3. Enhanced Transaction Form
Update `QuickTransactionSheet` to include category selection and budget warnings:

```swift
struct QuickTransactionSheet: View {
    // ... existing properties ...
    @State private var selectedCategory: TransactionCategory = .allowance
    @State private var budgetWarning: BudgetCheckResult?

    private let categoryService = CategoryService()

    var body: some View {
        NavigationStack {
            Form {
                // ... existing sections ...

                Section("Category") {
                    CategoryPicker(
                        selectedCategory: $selectedCategory,
                        transactionType: type
                    )
                    .onChange(of: selectedCategory) { _, _ in
                        checkBudget()
                    }
                    .onChange(of: amount) { _, _ in
                        checkBudget()
                    }
                }

                // Budget Warning
                if let warning = budgetWarning {
                    Section {
                        BudgetWarningView(result: warning)
                    }
                }
            }
            // ... rest of implementation ...
        }
    }

    private func checkBudget() {
        guard type == .debit, amount > 0 else {
            budgetWarning = nil
            return
        }

        Task {
            do {
                budgetWarning = try await categoryService.checkBudget(
                    for: child.id,
                    category: selectedCategory,
                    amount: amount
                )
            } catch {
                // Silently fail - budget checking is optional
                budgetWarning = nil
            }
        }
    }
}
```

## Navigation Integration

Add budget management to the child detail view:

```swift
struct ChildDetailView: View {
    let child: Child

    var body: some View {
        List {
            // ... existing sections ...

            Section {
                NavigationLink {
                    BudgetManagementView(child: child)
                } label: {
                    Label("Budget Management", systemImage: "chart.bar.doc.horizontal")
                }

                NavigationLink {
                    CategorySpendingView(childId: child.id)
                } label: {
                    Label("Spending by Category", systemImage: "chart.pie")
                }
            }
        }
    }
}
```

## Implementation Phases

### Phase 1: Models & API (Day 1) ✅ COMPLETE
- [x] Create TransactionCategory enum
- [x] Create CategoryBudget, CategorySpending models
- [x] Create BudgetCheckResult model
- [x] Add API endpoints via CategoryService and BudgetService
- [x] Implement CategoryService
- [x] Implement BudgetService
- [x] Models and services ready for testing

### Phase 2: UI Components (Day 2) ✅ COMPLETE
- [x] CategoryPicker component
- [x] BudgetCardView component with Badge
- [x] CategorySpendingChart component
- [x] BudgetWarningView component
- [x] Preview providers for all components (Xcode Canvas ready)

### Phase 3: Screens (Day 3)
- [ ] BudgetManagementView screen
- [ ] AddBudgetSheet
- [ ] Update QuickTransactionSheet with categories
- [ ] CategorySpendingView screen
- [ ] Integration tests

### Phase 4: ViewModel & Logic (Day 4)
- [ ] BudgetViewModel implementation
- [ ] Real-time budget checking
- [ ] Offline budget cache
- [ ] Unit tests for ViewModels

### Phase 5: Polish & Testing (Day 5)
- [ ] Error handling & user feedback
- [ ] Loading states & animations
- [ ] Accessibility labels
- [ ] Integration with Analytics
- [ ] E2E testing

## Testing Strategy

### Unit Tests
```swift
final class BudgetViewModelTests: XCTestCase {
    var sut: BudgetViewModel!
    var mockBudgetService: MockBudgetService!
    var mockCategoryService: MockCategoryService!

    func testLoadBudgets_Success() async {
        // Given
        let childId = UUID()
        mockBudgetService.budgets = [/* test data */]

        // When
        await sut.loadBudgets(for: childId)

        // Then
        XCTAssertEqual(sut.budgets.count, 2)
        XCTAssertFalse(sut.isLoading)
        XCTAssertNil(sut.errorMessage)
    }
}
```

### Snapshot Tests
```swift
func testBudgetCardView_Enforced() {
    let budget = CategoryBudget(/* ... */)
    let view = BudgetCardView(budget: budget, status: nil, onEdit: {}, onDelete: {})
    assertSnapshot(matching: view, as: .image)
}
```

## Accessibility

- Provide descriptive labels for all SF Symbols
- Support Dynamic Type for all text
- Ensure color is not the only indicator (use icons + text)
- VoiceOver support for budget status announcements

## Success Metrics

- Budget creation success rate: >95%
- Budget check response time: <200ms
- UI load time: <500ms
- Zero budget-related crashes
- Accessibility score: 100%
