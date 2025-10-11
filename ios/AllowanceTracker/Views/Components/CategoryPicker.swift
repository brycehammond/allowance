import SwiftUI

/// A picker component for selecting transaction categories with icons
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
                        .foregroundStyle(.blue)
                }
                .tag(category)
            }
        }
        .pickerStyle(.menu)
    }
}

// MARK: - Preview Provider

#Preview("Income Categories") {
    CategoryPickerPreview(type: .credit)
}

#Preview("Spending Categories") {
    CategoryPickerPreview(type: .debit)
}

private struct CategoryPickerPreview: View {
    let type: TransactionType
    @State private var selectedCategory: TransactionCategory

    init(type: TransactionType) {
        self.type = type
        _selectedCategory = State(initialValue: type == .credit ? .allowance : .toys)
    }

    var body: some View {
        Form {
            Section("Select Category") {
                CategoryPicker(
                    selectedCategory: $selectedCategory,
                    transactionType: type
                )

                Text("Selected: \(selectedCategory.displayName)")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
    }
}
