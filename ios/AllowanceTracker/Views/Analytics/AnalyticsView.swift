import SwiftUI
import Charts

/// View displaying financial analytics and insights for a child
@MainActor
struct AnalyticsView: View {

    // MARK: - Properties

    @State private var viewModel: AnalyticsViewModel

    // MARK: - Initialization

    init(childId: UUID, apiService: APIServiceProtocol = APIService()) {
        _viewModel = State(wrappedValue: AnalyticsViewModel(childId: childId, apiService: apiService))
    }

    // MARK: - Body

    var body: some View {
        ZStack {
            if viewModel.isLoading && viewModel.balanceHistory.isEmpty {
                // Loading state
                ProgressView("Loading analytics...")
            } else {
                // Analytics content
                analyticsScrollView
            }
        }
        .navigationTitle("Analytics")
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
            await viewModel.loadAllData()
        }
    }

    // MARK: - Subviews

    /// Main analytics scroll view
    private var analyticsScrollView: some View {
        ScrollView {
            VStack(spacing: 20) {
                // Balance history chart
                if !viewModel.balanceHistory.isEmpty {
                    balanceHistoryCard
                }

                // Income vs Spending summary
                if let summary = viewModel.incomeSpendingSummary {
                    incomeSpendingSummaryCard(summary)
                }

                // Spending breakdown
                if !viewModel.spendingBreakdown.isEmpty {
                    spendingBreakdownCard
                }

                // Monthly comparison
                if !viewModel.monthlyComparison.isEmpty {
                    monthlyComparisonCard
                }
            }
            .padding()
        }
    }

    /// Balance history chart card
    private var balanceHistoryCard: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Label("Balance History", systemImage: "chart.line.uptrend.xyaxis")
                    .font(.headline)
                    .foregroundStyle(Color.green600)

                Spacer()

                if let latest = viewModel.formattedLatestBalance {
                    Text(latest)
                        .font(.title3)
                        .fontWeight(.bold)
                        .fontDesign(.monospaced)
                        .foregroundStyle(Color.green500)
                }
            }

            // Simple chart using Swift Charts
            Chart(viewModel.balanceHistory) { point in
                LineMark(
                    x: .value("Date", point.date),
                    y: .value("Balance", Double(truncating: point.balance as NSDecimalNumber))
                )
                .foregroundStyle(Color.green500)

                AreaMark(
                    x: .value("Date", point.date),
                    y: .value("Balance", Double(truncating: point.balance as NSDecimalNumber))
                )
                .foregroundStyle(Color.green500.opacity(0.1))
            }
            .frame(height: 200)
            .chartXAxis {
                AxisMarks(values: .stride(by: .day, count: 5)) { value in
                    AxisValueLabel(format: .dateTime.month().day())
                }
            }
            .chartYAxis {
                AxisMarks { value in
                    AxisValueLabel {
                        if let doubleValue = value.as(Double.self) {
                            Text(Decimal(doubleValue).currencyFormatted)
                        }
                    }
                }
            }

            Text("Last 30 days")
                .font(.caption)
                .foregroundStyle(.secondary)
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }

    /// Income vs spending summary card
    private func incomeSpendingSummaryCard(_ summary: IncomeSpendingSummary) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Label("Income vs Spending", systemImage: "chart.bar.fill")
                    .font(.headline)
                    .foregroundStyle(Color.green600)

                Spacer()
            }

            Divider()

            // Income
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Total Income")
                        .font(.caption)
                        .foregroundStyle(.secondary)

                    Text(summary.formattedIncome)
                        .font(.title3)
                        .fontWeight(.bold)
                        .fontDesign(.monospaced)
                        .foregroundStyle(Color.green500)

                    Text("\(summary.incomeTransactionCount) transactions")
                        .font(.caption2)
                        .foregroundStyle(.secondary)
                }

                Spacer()

                // Spending
                VStack(alignment: .trailing, spacing: 4) {
                    Text("Total Spending")
                        .font(.caption)
                        .foregroundStyle(.secondary)

                    Text(summary.formattedSpending)
                        .font(.title3)
                        .fontWeight(.bold)
                        .fontDesign(.monospaced)
                        .foregroundStyle(Color.error)

                    Text("\(summary.spendingTransactionCount) transactions")
                        .font(.caption2)
                        .foregroundStyle(.secondary)
                }
            }

            Divider()

            // Net savings
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text("Net Savings")
                        .font(.caption)
                        .foregroundStyle(.secondary)

                    Text(summary.formattedSavings)
                        .font(.title2)
                        .fontWeight(.bold)
                        .fontDesign(.monospaced)
                        .foregroundStyle(summary.netSavings >= 0 ? Color.green500 : Color.error)
                }

                Spacer()

                VStack(alignment: .trailing, spacing: 4) {
                    Text("Savings Rate")
                        .font(.caption)
                        .foregroundStyle(.secondary)

                    Text(summary.formattedSavingsRate)
                        .font(.title2)
                        .fontWeight(.bold)
                        .foregroundStyle(summary.savingsRate >= 0 ? Color.green500 : Color.error)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }

    /// Spending breakdown by category card
    private var spendingBreakdownCard: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Label("Spending by Category", systemImage: "chart.pie.fill")
                    .font(.headline)
                    .foregroundStyle(Color.green600)

                Spacer()
            }

            Divider()

            // Category list
            ForEach(viewModel.topSpendingCategories) { category in
                VStack(spacing: 4) {
                    HStack {
                        Text(category.category)
                            .font(.subheadline)
                            .fontWeight(.medium)

                        Spacer()

                        Text(category.formattedAmount)
                            .font(.subheadline)
                            .fontWeight(.semibold)
                            .fontDesign(.monospaced)
                    }

                    HStack {
                        ProgressView(value: Double(truncating: category.percentage as NSDecimalNumber), total: 100)
                            .tint(Color.green500)

                        Text(category.formattedPercentage)
                            .font(.caption2)
                            .foregroundStyle(.secondary)
                            .frame(width: 50, alignment: .trailing)
                    }
                }
                .padding(.vertical, 4)
            }

            if viewModel.spendingBreakdown.count > 5 {
                Text("Showing top 5 categories")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }

    /// Monthly comparison card
    private var monthlyComparisonCard: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Label("Monthly Comparison", systemImage: "calendar")
                    .font(.headline)
                    .foregroundStyle(Color.green600)

                Spacer()

                if viewModel.isSavingsImproving {
                    Image(systemName: "arrow.up.circle.fill")
                        .foregroundStyle(Color.green500)
                }
            }

            Divider()

            // Monthly data
            ForEach(viewModel.monthlyComparison.prefix(3)) { month in
                VStack(alignment: .leading, spacing: 8) {
                    Text(month.monthName)
                        .font(.subheadline)
                        .fontWeight(.bold)

                    HStack(spacing: 16) {
                        VStack(alignment: .leading, spacing: 2) {
                            Text("Income")
                                .font(.caption2)
                                .foregroundStyle(.secondary)
                            Text(month.formattedIncome)
                                .font(.caption)
                                .fontDesign(.monospaced)
                                .foregroundStyle(Color.green500)
                        }

                        VStack(alignment: .leading, spacing: 2) {
                            Text("Spending")
                                .font(.caption2)
                                .foregroundStyle(.secondary)
                            Text(month.formattedSpending)
                                .font(.caption)
                                .fontDesign(.monospaced)
                                .foregroundStyle(Color.error)
                        }

                        VStack(alignment: .leading, spacing: 2) {
                            Text("Savings")
                                .font(.caption2)
                                .foregroundStyle(.secondary)
                            Text(month.formattedSavings)
                                .font(.caption)
                                .fontDesign(.monospaced)
                                .fontWeight(.semibold)
                        }
                    }
                }
                .padding(.vertical, 4)

                if month.id != viewModel.monthlyComparison.prefix(3).last?.id {
                    Divider()
                }
            }

            if viewModel.monthlyComparison.count > 3 {
                Text("Showing last 3 months")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }
}

// MARK: - Preview Provider

#Preview("Analytics View") {
    NavigationStack {
        AnalyticsView(childId: UUID())
    }
}
