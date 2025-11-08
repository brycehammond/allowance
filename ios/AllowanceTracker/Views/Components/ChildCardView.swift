import SwiftUI

/// A card view displaying child profile with balance and quick actions
struct ChildCardView: View {
    let child: Child
    let onTap: () -> Void

    var body: some View {
        Button(action: onTap) {
            VStack(alignment: .leading, spacing: 12) {
                // Header with name
                HStack {
                    Label {
                        Text(child.fullName)
                            .font(.scalable(.headline, weight: .semibold))
                    } icon: {
                        Image(systemName: "person.circle.fill")
                            .foregroundStyle(.blue)
                            .accessibilityHidden()
                    }

                    Spacer()

                    Image(systemName: "chevron.right")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .accessibilityHidden()
                }

                Divider()
                    .accessibilityHidden()

                // Balance section
                VStack(alignment: .leading, spacing: 8) {
                    Text("Current Balance")
                        .font(.scalable(.caption))
                        .foregroundStyle(.secondary)
                        .accessibilityHidden()

                    Text(child.formattedBalance)
                        .font(.scalable(.title2, weight: .bold))
                        .fontDesign(.monospaced)
                        .foregroundStyle(balanceColor)
                        .accessibilityHidden()
                }

                // Weekly allowance
                HStack {
                    Text("Weekly Allowance:")
                        .font(.scalable(.caption))
                        .foregroundStyle(.secondary)
                        .accessibilityHidden()

                    Spacer()

                    Text(child.weeklyAllowance.currencyFormatted)
                        .font(.scalable(.caption, weight: .medium))
                        .fontDesign(.monospaced)
                        .accessibilityHidden()
                }

                // Allowance day
                HStack {
                    Text("Allowance Schedule:")
                        .font(.scalable(.caption))
                        .foregroundStyle(.secondary)
                        .accessibilityHidden()

                    Spacer()

                    Text(child.allowanceDayDisplay)
                        .font(.scalable(.caption, weight: .medium))
                        .accessibilityHidden()
                }

                // Last allowance date (if available)
                if let lastDate = child.lastAllowanceDate {
                    HStack {
                        Text("Last Allowance:")
                            .font(.scalable(.caption))
                            .foregroundStyle(.secondary)
                            .accessibilityHidden()

                        Spacer()

                        Text(lastDate.formattedDisplay)
                            .font(.scalable(.caption, weight: .medium))
                            .accessibilityHidden()
                    }
                }
            }
            .padding()
            .background(Color(.systemBackground))
            .clipShape(RoundedRectangle(cornerRadius: 12))
            .shadow(radius: 2)
        }
        .buttonStyle(.plain)
        .accessibility(
            label: accessibilityLabel,
            hint: "Double tap to view details and transactions",
            traits: .isButton
        )
        .accessibilityIdentifier("\(AccessibilityIdentifier.childCard)\(child.id.uuidString)")
    }

    // MARK: - Computed Properties

    /// Color based on balance amount
    private var balanceColor: Color {
        if child.currentBalance < 0 {
            return .red
        } else if child.currentBalance == 0 {
            return .secondary
        } else {
            return .green
        }
    }

    /// Comprehensive accessibility label combining all information
    private var accessibilityLabel: String {
        var parts: [String] = []

        // Name
        parts.append(child.fullName)

        // Balance with spoken currency
        let balanceDescription = child.currentBalance.accessibilityCurrencyLabel
        if child.currentBalance < 0 {
            parts.append("Balance: negative \(balanceDescription)")
        } else if child.currentBalance == 0 {
            parts.append("Balance: zero dollars")
        } else {
            parts.append("Balance: \(balanceDescription)")
        }

        // Weekly allowance
        parts.append("Weekly allowance: \(child.weeklyAllowance.accessibilityCurrencyLabel)")

        // Allowance schedule
        parts.append("Allowance schedule: \(child.allowanceDayDisplay)")

        // Last allowance date
        if let lastDate = child.lastAllowanceDate {
            parts.append("Last allowance: \(lastDate.accessibilityLabel)")
        }

        return parts.joined(separator: ". ")
    }
}

// MARK: - Preview Provider

#Preview("Child Card - Positive Balance") {
    ChildCardView(
        child: Child(
            id: UUID(),
            firstName: "Alice",
            lastName: "Smith",
            weeklyAllowance: 10.00,
            currentBalance: 125.50,
            lastAllowanceDate: Date(),
            allowanceDay: .friday
        ),
        onTap: { print("Card tapped") }
    )
    .padding()
    .previewLayout(.sizeThatFits)
}

#Preview("Child Card - Zero Balance") {
    ChildCardView(
        child: Child(
            id: UUID(),
            firstName: "Bob",
            lastName: "Johnson",
            weeklyAllowance: 15.00,
            currentBalance: 0.00,
            lastAllowanceDate: nil,
            allowanceDay: nil
        ),
        onTap: { print("Card tapped") }
    )
    .padding()
    .previewLayout(.sizeThatFits)
}

#Preview("Child Card - Negative Balance") {
    ChildCardView(
        child: Child(
            id: UUID(),
            firstName: "Charlie",
            lastName: "Brown",
            weeklyAllowance: 20.00,
            currentBalance: -5.25,
            lastAllowanceDate: Date(),
            allowanceDay: .monday
        ),
        onTap: { print("Card tapped") }
    )
    .padding()
    .previewLayout(.sizeThatFits)
}

#Preview("Multiple Cards") {
    VStack(spacing: 16) {
        ChildCardView(
            child: Child(
                id: UUID(),
                firstName: "Alice",
                lastName: "Smith",
                weeklyAllowance: 10.00,
                currentBalance: 125.50,
                lastAllowanceDate: Date(),
                allowanceDay: .wednesday
            ),
            onTap: { print("Alice tapped") }
        )

        ChildCardView(
            child: Child(
                id: UUID(),
                firstName: "Bob",
                lastName: "Johnson",
                weeklyAllowance: 15.00,
                currentBalance: 45.00,
                lastAllowanceDate: Date(),
                allowanceDay: nil
            ),
            onTap: { print("Bob tapped") }
        )
    }
    .padding()
}
