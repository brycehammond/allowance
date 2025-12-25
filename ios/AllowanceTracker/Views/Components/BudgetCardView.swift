import SwiftUI

/// A card view displaying budget details with progress visualization
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
                        .foregroundStyle(Color.green600)
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
                        .foregroundStyle(Color.green600)
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
                        Text(status.formattedCurrentSpending)
                            .fontDesign(.monospaced)
                            .fontWeight(.medium)
                    }

                    HStack {
                        Text("Remaining:")
                            .font(.caption)
                        Spacer()
                        Text(status.formattedRemaining)
                            .fontDesign(.monospaced)
                            .fontWeight(.medium)
                            .foregroundStyle(status.progressColor)
                    }

                    // Progress Bar
                    VStack(alignment: .trailing, spacing: 4) {
                        ProgressView(value: Double(status.percentUsed), total: 100)
                            .tint(status.progressColor)

                        Text("\(status.percentUsed)%")
                            .font(.caption2)
                            .foregroundStyle(.secondary)
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
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }
}

/// A small badge component for displaying status labels
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
            .background(Color.green500.opacity(0.1))
            .foregroundStyle(Color.green600)
            .clipShape(RoundedRectangle(cornerRadius: 8))
    }
}

// MARK: - Preview Provider

#Preview("Budget Card - No Status") {
    BudgetCardView(
        budget: CategoryBudget(
            id: UUID(),
            childId: UUID(),
            category: .toys,
            limit: 50.00,
            period: .weekly,
            alertThresholdPercent: 80,
            enforceLimit: true,
            createdAt: Date(),
            updatedAt: Date()
        ),
        status: nil,
        onEdit: { print("Edit tapped") },
        onDelete: { print("Delete tapped") }
    )
    .padding()
    .previewLayout(.sizeThatFits)
}

#Preview("Budget Card - Safe Status") {
    BudgetCardView(
        budget: CategoryBudget(
            id: UUID(),
            childId: UUID(),
            category: .games,
            limit: 30.00,
            period: .monthly,
            alertThresholdPercent: 80,
            enforceLimit: false,
            createdAt: Date(),
            updatedAt: Date()
        ),
        status: CategoryBudgetStatus(
            category: .games,
            categoryName: "Games",
            budgetLimit: 30.00,
            currentSpending: 10.00,
            remaining: 20.00,
            percentUsed: 33,
            status: .safe,
            period: .monthly
        ),
        onEdit: { print("Edit tapped") },
        onDelete: { print("Delete tapped") }
    )
    .padding()
    .previewLayout(.sizeThatFits)
}

#Preview("Budget Card - Warning Status") {
    BudgetCardView(
        budget: CategoryBudget(
            id: UUID(),
            childId: UUID(),
            category: .snacks,
            limit: 25.00,
            period: .weekly,
            alertThresholdPercent: 80,
            enforceLimit: false,
            createdAt: Date(),
            updatedAt: Date()
        ),
        status: CategoryBudgetStatus(
            category: .snacks,
            categoryName: "Snacks",
            budgetLimit: 25.00,
            currentSpending: 22.00,
            remaining: 3.00,
            percentUsed: 88,
            status: .warning,
            period: .weekly
        ),
        onEdit: { print("Edit tapped") },
        onDelete: { print("Delete tapped") }
    )
    .padding()
    .previewLayout(.sizeThatFits)
}

#Preview("Budget Card - Over Budget") {
    BudgetCardView(
        budget: CategoryBudget(
            id: UUID(),
            childId: UUID(),
            category: .candy,
            limit: 15.00,
            period: .weekly,
            alertThresholdPercent: 80,
            enforceLimit: true,
            createdAt: Date(),
            updatedAt: Date()
        ),
        status: CategoryBudgetStatus(
            category: .candy,
            categoryName: "Candy",
            budgetLimit: 15.00,
            currentSpending: 18.50,
            remaining: -3.50,
            percentUsed: 123,
            status: .overBudget,
            period: .weekly
        ),
        onEdit: { print("Edit tapped") },
        onDelete: { print("Delete tapped") }
    )
    .padding()
    .previewLayout(.sizeThatFits)
}

#Preview("Badge Component") {
    VStack(spacing: 10) {
        Badge("Enforced")
        Badge("Warning Only")
        Badge("Test Badge")
    }
    .padding()
}
