import SwiftUI
import Charts

/// A chart component displaying spending breakdown by category
struct CategorySpendingChart: View {
    let spending: [CategorySpending]
    var maxCategories: Int = 8

    private var topSpending: [CategorySpending] {
        Array(spending.prefix(maxCategories))
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Spending by Category")
                .font(.headline)
                .padding(.horizontal)

            if spending.isEmpty {
                ContentUnavailableView(
                    "No Spending Data",
                    systemImage: "chart.bar.xaxis",
                    description: Text("Spending will appear here")
                )
                .frame(height: 200)
            } else {
                // Bar Chart
                Chart(topSpending) { item in
                    BarMark(
                        x: .value("Amount", item.totalAmount.doubleValue),
                        y: .value("Category", item.categoryName)
                    )
                    .foregroundStyle(by: .value("Category", item.categoryName))
                }
                .frame(height: CGFloat(topSpending.count * 40))
                .chartLegend(.hidden)
                .padding(.horizontal)

                // Details List
                VStack(spacing: 0) {
                    ForEach(topSpending) { item in
                        CategorySpendingRow(spending: item)

                        if item.id != topSpending.last?.id {
                            Divider()
                                .padding(.leading, 44)
                        }
                    }
                }
            }
        }
        .padding(.vertical)
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }
}

/// A row displaying individual category spending details
private struct CategorySpendingRow: View {
    let spending: CategorySpending

    var body: some View {
        HStack(spacing: 12) {
            // Icon
            Image(systemName: spending.category.icon)
                .foregroundStyle(Color.green600)
                .font(.title3)
                .frame(width: 32, height: 32)

            // Category name
            VStack(alignment: .leading, spacing: 2) {
                Text(spending.categoryName)
                    .font(.subheadline)
                    .fontWeight(.medium)

                Text("\(spending.transactionCount) transaction\(spending.transactionCount != 1 ? "s" : "")")
                    .font(.caption2)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            // Amount and percentage
            VStack(alignment: .trailing, spacing: 2) {
                Text(spending.formattedAmount)
                    .font(.subheadline)
                    .fontWeight(.semibold)
                    .fontDesign(.monospaced)

                Text("\(spending.percentage.doubleValue, specifier: "%.1f")%")
                    .font(.caption2)
                    .foregroundStyle(.secondary)
            }
        }
        .padding(.horizontal)
        .padding(.vertical, 8)
    }
}

// MARK: - Preview Provider

#Preview("With Data") {
    CategorySpendingChart(
        spending: [
            CategorySpending(
                category: .toys,
                categoryName: "Toys",
                totalAmount: 45.00,
                transactionCount: 5,
                percentage: 30.0
            ),
            CategorySpending(
                category: .games,
                categoryName: "Games",
                totalAmount: 35.00,
                transactionCount: 3,
                percentage: 23.3
            ),
            CategorySpending(
                category: .snacks,
                categoryName: "Snacks",
                totalAmount: 25.00,
                transactionCount: 8,
                percentage: 16.7
            ),
            CategorySpending(
                category: .books,
                categoryName: "Books",
                totalAmount: 20.00,
                transactionCount: 2,
                percentage: 13.3
            ),
            CategorySpending(
                category: .candy,
                categoryName: "Candy",
                totalAmount: 15.00,
                transactionCount: 6,
                percentage: 10.0
            ),
            CategorySpending(
                category: .crafts,
                categoryName: "Crafts",
                totalAmount: 10.00,
                transactionCount: 1,
                percentage: 6.7
            )
        ]
    )
    .padding()
}

#Preview("Empty State") {
    CategorySpendingChart(spending: [])
        .padding()
}

#Preview("Single Category") {
    CategorySpendingChart(
        spending: [
            CategorySpending(
                category: .toys,
                categoryName: "Toys",
                totalAmount: 100.00,
                transactionCount: 10,
                percentage: 100.0
            )
        ]
    )
    .padding()
}

#Preview("Many Categories") {
    CategorySpendingChart(
        spending: TransactionCategory.spendingCategories.enumerated().map { index, category in
            CategorySpending(
                category: category,
                categoryName: category.displayName,
                totalAmount: Decimal(Double(50 - index * 4)),
                transactionCount: 10 - index,
                percentage: Decimal(Double(100 - index * 8))
            )
        }
    )
    .padding()
}
