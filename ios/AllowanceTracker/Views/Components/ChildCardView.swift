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
                            .font(.headline)
                    } icon: {
                        Image(systemName: "person.circle.fill")
                            .foregroundStyle(.blue)
                    }

                    Spacer()

                    Image(systemName: "chevron.right")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Divider()

                // Balance section
                VStack(alignment: .leading, spacing: 8) {
                    Text("Current Balance")
                        .font(.caption)
                        .foregroundStyle(.secondary)

                    Text(child.formattedBalance)
                        .font(.title2)
                        .fontWeight(.bold)
                        .fontDesign(.monospaced)
                        .foregroundStyle(balanceColor)
                }

                // Weekly allowance
                HStack {
                    Text("Weekly Allowance:")
                        .font(.caption)
                        .foregroundStyle(.secondary)

                    Spacer()

                    Text(child.weeklyAllowance.currencyFormatted)
                        .font(.caption)
                        .fontWeight(.medium)
                        .fontDesign(.monospaced)
                }

                // Last allowance date (if available)
                if let lastDate = child.lastAllowanceDate {
                    HStack {
                        Text("Last Allowance:")
                            .font(.caption)
                            .foregroundStyle(.secondary)

                        Spacer()

                        Text(lastDate.formattedDisplay)
                            .font(.caption)
                            .fontWeight(.medium)
                    }
                }
            }
            .padding()
            .background(Color(.systemBackground))
            .clipShape(RoundedRectangle(cornerRadius: 12))
            .shadow(radius: 2)
        }
        .buttonStyle(.plain)
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
            lastAllowanceDate: Date()
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
            lastAllowanceDate: nil
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
            lastAllowanceDate: Date()
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
                lastAllowanceDate: Date()
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
                lastAllowanceDate: Date()
            ),
            onTap: { print("Bob tapped") }
        )
    }
    .padding()
}
