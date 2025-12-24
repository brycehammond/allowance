import SwiftUI

/// A card view displaying child profile with balance breakdown and quick actions
struct ChildCardView: View {
    let child: Child
    var onTap: (() -> Void)? = nil

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            // Header with avatar and name
            HStack(spacing: 12) {
                // Avatar
                ZStack {
                    Circle()
                        .fill(DesignSystem.Colors.primary.opacity(0.15))
                        .frame(width: 44, height: 44)
                    Text(String(child.firstName.prefix(1)))
                        .font(.headline)
                        .fontWeight(.semibold)
                        .foregroundStyle(DesignSystem.Colors.primary)
                }
                .accessibilityHidden(true)

                VStack(alignment: .leading, spacing: 2) {
                    Text(child.fullName)
                        .font(.scalable(.headline, weight: .semibold))
                    Text("Weekly: \(child.weeklyAllowance.currencyFormatted)")
                        .font(.scalable(.caption))
                        .foregroundStyle(.secondary)
                        .accessibilityHidden(true)
                }

                Spacer()

                Image(systemName: "chevron.right")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .accessibilityHidden(true)
            }

            Divider()
                .accessibilityHidden(true)

            // Total Balance
            VStack(alignment: .leading, spacing: 4) {
                Text("Total Balance")
                    .font(.scalable(.caption))
                    .foregroundStyle(.secondary)
                    .accessibilityHidden(true)

                Text(child.formattedTotalBalance)
                    .font(.scalable(.title2, weight: .bold))
                    .fontDesign(.monospaced)
                    .foregroundStyle(totalBalanceColor)
                    .accessibilityHidden(true)
            }

            // Balance breakdown
            HStack(spacing: 16) {
                // Spending balance
                VStack(alignment: .leading, spacing: 2) {
                    Text(child.formattedBalance)
                        .font(.scalable(.subheadline, weight: .semibold))
                        .fontDesign(.monospaced)
                        .foregroundStyle(.primary)
                    Text("Spending")
                        .font(.scalable(.caption2))
                        .foregroundStyle(.secondary)
                }
                .accessibilityHidden(true)

                // Savings balance
                VStack(alignment: .leading, spacing: 2) {
                    Text(child.formattedSavingsBalance)
                        .font(.scalable(.subheadline, weight: .semibold))
                        .fontDesign(.monospaced)
                        .foregroundStyle(DesignSystem.Colors.primary)
                    Text("Savings")
                        .font(.scalable(.caption2))
                        .foregroundStyle(.secondary)
                }
                .accessibilityHidden(true)

                Spacer()
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
        .contentShape(Rectangle())
        .accessibility(
            label: accessibilityLabel,
            hint: "Double tap to view details and transactions",
            traits: .isButton
        )
        .accessibilityIdentifier("\(AccessibilityIdentifier.childCard)\(child.id.uuidString)")
    }

    // MARK: - Computed Properties

    /// Color based on total balance amount
    private var totalBalanceColor: Color {
        if child.totalBalance < 0 {
            return .red
        } else if child.totalBalance == 0 {
            return .secondary
        } else {
            return DesignSystem.Colors.primary
        }
    }

    /// Comprehensive accessibility label combining all information
    private var accessibilityLabel: String {
        var parts: [String] = []

        // Name
        parts.append(child.fullName)

        // Total balance
        let totalBalanceDescription = child.totalBalance.accessibilityCurrencyLabel
        if child.totalBalance < 0 {
            parts.append("Total balance: negative \(totalBalanceDescription)")
        } else if child.totalBalance == 0 {
            parts.append("Total balance: zero dollars")
        } else {
            parts.append("Total balance: \(totalBalanceDescription)")
        }

        // Spending balance
        parts.append("Spending: \(child.currentBalance.accessibilityCurrencyLabel)")

        // Savings balance
        parts.append("Savings: \(child.savingsBalance.accessibilityCurrencyLabel)")

        // Weekly allowance
        parts.append("Weekly allowance: \(child.weeklyAllowance.accessibilityCurrencyLabel)")

        return parts.joined(separator: ". ")
    }
}

// MARK: - Preview Helpers

private extension Child {
    static func preview(
        firstName: String,
        lastName: String,
        weeklyAllowance: Decimal = 10.00,
        currentBalance: Decimal = 50.00,
        savingsBalance: Decimal = 25.00,
        lastAllowanceDate: Date? = Date(),
        allowanceDay: Weekday? = .friday
    ) -> Child {
        Child(
            id: UUID(),
            firstName: firstName,
            lastName: lastName,
            weeklyAllowance: weeklyAllowance,
            currentBalance: currentBalance,
            savingsBalance: savingsBalance,
            lastAllowanceDate: lastAllowanceDate,
            allowanceDay: allowanceDay,
            savingsAccountEnabled: savingsBalance > 0,
            savingsTransferType: .percentage,
            savingsTransferPercentage: 20,
            savingsTransferAmount: nil,
            savingsBalanceVisibleToChild: true,
            allowDebt: false
        )
    }
}

// MARK: - Preview Provider

#Preview("Child Card - With Savings") {
    ChildCardView(
        child: .preview(
            firstName: "Alice",
            lastName: "Smith",
            currentBalance: 125.50,
            savingsBalance: 45.00
        )
    )
    .padding()
    .previewLayout(.sizeThatFits)
}

#Preview("Child Card - Zero Balance") {
    ChildCardView(
        child: .preview(
            firstName: "Bob",
            lastName: "Johnson",
            weeklyAllowance: 15.00,
            currentBalance: 0.00,
            savingsBalance: 0.00,
            lastAllowanceDate: nil,
            allowanceDay: nil
        )
    )
    .padding()
    .previewLayout(.sizeThatFits)
}

#Preview("Multiple Cards") {
    VStack(spacing: 16) {
        ChildCardView(
            child: .preview(
                firstName: "Alice",
                lastName: "Smith",
                currentBalance: 125.50,
                savingsBalance: 45.00,
                allowanceDay: .wednesday
            )
        )

        ChildCardView(
            child: .preview(
                firstName: "Bob",
                lastName: "Johnson",
                weeklyAllowance: 15.00,
                currentBalance: 45.00,
                savingsBalance: 10.00,
                allowanceDay: nil
            )
        )
    }
    .padding()
}
