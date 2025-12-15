import SwiftUI

/// Screen displaying spending analytics by category
struct CategorySpendingView: View {
    @State private var viewModel = CategorySpendingViewModel()

    let childId: UUID
    let childName: String

    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                // Date Range Picker
                DateRangeSection(
                    startDate: $viewModel.startDate,
                    endDate: $viewModel.endDate
                )
                .padding(.horizontal)

                // Spending Chart
                if viewModel.isLoading {
                    ProgressView("Loading spending data...")
                        .frame(height: 200)
                } else {
                    CategorySpendingChart(spending: viewModel.spending)
                        .padding(.horizontal)
                }

                // Summary Card
                if !viewModel.spending.isEmpty {
                    SummaryCard(spending: viewModel.spending)
                        .padding(.horizontal)
                }
            }
            .padding(.vertical)
        }
        .navigationTitle("Spending by Category")
        .navigationBarTitleDisplayMode(.large)
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                Menu {
                    Button {
                        viewModel.setPreset(.thisWeek)
                    } label: {
                        Label("This Week", systemImage: "calendar")
                    }

                    Button {
                        viewModel.setPreset(.thisMonth)
                    } label: {
                        Label("This Month", systemImage: "calendar")
                    }

                    Button {
                        viewModel.setPreset(.last30Days)
                    } label: {
                        Label("Last 30 Days", systemImage: "calendar")
                    }

                    Button {
                        viewModel.setPreset(.allTime)
                    } label: {
                        Label("All Time", systemImage: "calendar")
                    }
                } label: {
                    Label("Date Range", systemImage: "calendar.badge.clock")
                }
            }
        }
        .alert("Error", isPresented: .constant(viewModel.errorMessage != nil)) {
            Button("OK") {
                viewModel.clearError()
            }
        } message: {
            Text(viewModel.errorMessage ?? "")
        }
        .task {
            await viewModel.loadSpending(for: childId)
        }
        .refreshable {
            await viewModel.loadSpending(for: childId)
        }
    }
}

// MARK: - Date Range Section

private struct DateRangeSection: View {
    @Binding var startDate: Date?
    @Binding var endDate: Date?

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Date Range")
                .font(.headline)

            HStack(spacing: 12) {
                DatePicker(
                    "Start",
                    selection: Binding(
                        get: { startDate ?? Date().addingTimeInterval(-30 * 24 * 60 * 60) },
                        set: { startDate = $0 }
                    ),
                    displayedComponents: .date
                )
                .labelsHidden()
                .frame(maxWidth: .infinity)

                Text("to")
                    .foregroundStyle(.secondary)

                DatePicker(
                    "End",
                    selection: Binding(
                        get: { endDate ?? Date() },
                        set: { endDate = $0 }
                    ),
                    displayedComponents: .date
                )
                .labelsHidden()
                .frame(maxWidth: .infinity)
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }
}

// MARK: - Summary Card

private struct SummaryCard: View {
    let spending: [CategorySpending]

    private var totalSpent: Decimal {
        spending.reduce(0) { $0 + $1.totalAmount }
    }

    private var totalTransactions: Int {
        spending.reduce(0) { $0 + $1.transactionCount }
    }

    private var topCategory: CategorySpending? {
        spending.max(by: { $0.totalAmount < $1.totalAmount })
    }

    var body: some View {
        VStack(spacing: 12) {
            Text("Summary")
                .font(.headline)
                .frame(maxWidth: .infinity, alignment: .leading)

            HStack(spacing: 20) {
                // Total Spent
                VStack(spacing: 4) {
                    Text("Total Spent")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text(totalSpent.currencyFormatted)
                        .font(.title2)
                        .fontWeight(.bold)
                        .fontDesign(.monospaced)
                }
                .frame(maxWidth: .infinity)

                Divider()

                // Transactions
                VStack(spacing: 4) {
                    Text("Transactions")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text("\(totalTransactions)")
                        .font(.title2)
                        .fontWeight(.bold)
                }
                .frame(maxWidth: .infinity)

                Divider()

                // Top Category
                VStack(spacing: 4) {
                    Text("Top Category")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    if let top = topCategory {
                        Label {
                            Text(top.categoryName)
                                .font(.caption)
                                .fontWeight(.semibold)
                        } icon: {
                            Image(systemName: top.category.icon)
                                .foregroundStyle(.blue)
                        }
                    } else {
                        Text("—")
                            .font(.title2)
                    }
                }
                .frame(maxWidth: .infinity)
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }
}

// MARK: - View Model

@MainActor
final class CategorySpendingViewModel: ObservableObject {
    @Published var spending: [CategorySpending] = []
    @Published var isLoading = false
    @Published var errorMessage: String?
    @Published var startDate: Date?
    @Published var endDate: Date?

    private let categoryService: CategoryServiceProtocol

    init(categoryService: CategoryServiceProtocol = CategoryService.shared) {
        self.categoryService = categoryService
    }

    func loadSpending(for childId: UUID) async {
        isLoading = true
        errorMessage = nil

        do {
            spending = try await categoryService.getCategorySpending(
                for: childId,
                startDate: startDate,
                endDate: endDate
            )
        } catch {
            errorMessage = "Failed to load spending data: \(error.localizedDescription)"
            spending = []
        }

        isLoading = false
    }

    func setPreset(_ preset: DatePreset) {
        switch preset {
        case .thisWeek:
            startDate = Calendar.current.startOfWeek(for: Date())
            endDate = Date()
        case .thisMonth:
            startDate = Calendar.current.startOfMonth(for: Date())
            endDate = Date()
        case .last30Days:
            startDate = Calendar.current.date(byAdding: .day, value: -30, to: Date())
            endDate = Date()
        case .allTime:
            startDate = nil
            endDate = nil
        }
    }

    func clearError() {
        errorMessage = nil
    }

    enum DatePreset {
        case thisWeek
        case thisMonth
        case last30Days
        case allTime
    }
}

// MARK: - Calendar Extensions

private extension Calendar {
    func startOfWeek(for date: Date) -> Date {
        let components = dateComponents([.yearForWeekOfYear, .weekOfYear], from: date)
        return self.date(from: components) ?? date
    }

    func startOfMonth(for date: Date) -> Date {
        let components = dateComponents([.year, .month], from: date)
        return self.date(from: components) ?? date
    }
}

// MARK: - Preview Provider

#Preview("With Data") {
    NavigationStack {
        CategorySpendingView(
            childId: UUID(),
            childName: "Alice"
        )
    }
}

#Preview("Loading") {
    NavigationStack {
        CategorySpendingView(
            childId: UUID(),
            childName: "Bob"
        )
    }
}
