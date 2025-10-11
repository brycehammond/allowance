import SwiftUI

/// A warning/alert view for budget check results during transaction creation
struct BudgetWarningView: View {
    let result: BudgetCheckResult

    private var icon: String {
        result.allowed ? "exclamationmark.triangle.fill" : "xmark.circle.fill"
    }

    private var iconColor: Color {
        result.allowed ? .orange : .red
    }

    private var backgroundColor: Color {
        (result.allowed ? Color.orange : Color.red).opacity(0.1)
    }

    private var title: String {
        result.allowed ? "Budget Warning" : "Budget Exceeded"
    }

    var body: some View {
        HStack(spacing: 12) {
            // Icon
            Image(systemName: icon)
                .foregroundStyle(iconColor)
                .font(.title2)

            // Message
            VStack(alignment: .leading, spacing: 4) {
                Text(title)
                    .font(.headline)
                    .foregroundStyle(iconColor)

                Text(result.message)
                    .font(.caption)
                    .foregroundStyle(.secondary)

                // Budget details
                HStack(spacing: 16) {
                    VStack(alignment: .leading, spacing: 2) {
                        Text("Current")
                            .font(.caption2)
                            .foregroundStyle(.secondary)
                        Text(result.formattedCurrentSpending)
                            .font(.caption)
                            .fontWeight(.medium)
                            .fontDesign(.monospaced)
                    }

                    VStack(alignment: .leading, spacing: 2) {
                        Text("Limit")
                            .font(.caption2)
                            .foregroundStyle(.secondary)
                        Text(result.formattedBudgetLimit)
                            .font(.caption)
                            .fontWeight(.medium)
                            .fontDesign(.monospaced)
                    }

                    VStack(alignment: .leading, spacing: 2) {
                        Text("After")
                            .font(.caption2)
                            .foregroundStyle(.secondary)
                        Text(result.formattedRemainingAfter)
                            .font(.caption)
                            .fontWeight(.medium)
                            .fontDesign(.monospaced)
                            .foregroundStyle(result.remainingAfter < 0 ? .red : .primary)
                    }
                }
                .padding(.top, 4)
            }

            Spacer()
        }
        .padding()
        .background(backgroundColor)
        .clipShape(RoundedRectangle(cornerRadius: 8))
    }
}

// MARK: - Preview Provider

#Preview("Warning - Allowed") {
    VStack(spacing: 16) {
        BudgetWarningView(
            result: BudgetCheckResult(
                allowed: true,
                message: "This purchase will use 85% of your weekly budget for Toys.",
                currentSpending: 35.00,
                budgetLimit: 50.00,
                remainingAfter: 7.50
            )
        )

        BudgetWarningView(
            result: BudgetCheckResult(
                allowed: true,
                message: "Only $3.00 will remain in your Snacks budget.",
                currentSpending: 22.00,
                budgetLimit: 30.00,
                remainingAfter: 3.00
            )
        )
    }
    .padding()
}

#Preview("Error - Not Allowed") {
    VStack(spacing: 16) {
        BudgetWarningView(
            result: BudgetCheckResult(
                allowed: false,
                message: "This purchase exceeds your weekly budget for Games by $5.00.",
                currentSpending: 28.00,
                budgetLimit: 30.00,
                remainingAfter: -3.00
            )
        )

        BudgetWarningView(
            result: BudgetCheckResult(
                allowed: false,
                message: "Budget limit enforced. Transaction blocked.",
                currentSpending: 45.00,
                budgetLimit: 50.00,
                remainingAfter: -8.00
            )
        )
    }
    .padding()
}

#Preview("In Form Context") {
    Form {
        Section("Transaction Details") {
            Text("Amount: $15.00")
            Text("Category: Toys")
        }

        Section {
            BudgetWarningView(
                result: BudgetCheckResult(
                    allowed: true,
                    message: "This will use most of your weekly toy budget.",
                    currentSpending: 35.00,
                    budgetLimit: 50.00,
                    remainingAfter: 5.00
                )
            )
        }
        .listRowInsets(EdgeInsets())
        .listRowBackground(Color.clear)
    }
}
