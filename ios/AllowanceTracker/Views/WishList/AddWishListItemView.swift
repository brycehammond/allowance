import SwiftUI

/// Form for adding a new wish list item
struct AddWishListItemView: View {

    // MARK: - Properties

    @Environment(\.dismiss) private var dismiss
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    var viewModel: WishListViewModel

    @State private var name: String = ""
    @State private var price: String = ""
    @State private var url: String = ""
    @State private var notes: String = ""
    @FocusState private var focusedField: Field?

    // MARK: - Computed Properties

    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    // MARK: - Field enum

    private enum Field {
        case name, price, url, notes
    }

    // MARK: - Body

    var body: some View {
        NavigationStack {
            Form {
                // Name section
                Section {
                    TextField("Enter item name", text: $name)
                        .focused($focusedField, equals: .name)
                } header: {
                    Text("Item Name")
                } footer: {
                    Text("What do you want to save for?")
                }

                // Price section
                Section {
                    HStack {
                        Text("$")
                            .font(.title3)
                            .foregroundStyle(.secondary)

                        TextField("0.00", text: $price)
                            .keyboardType(.decimalPad)
                            .font(.title3)
                            .fontDesign(.monospaced)
                            .focused($focusedField, equals: .price)
                    }
                } header: {
                    Text("Price")
                } footer: {
                    if let priceValue = priceDecimal {
                        Text("Price: \(priceValue.currencyFormatted)")
                            .foregroundStyle(.secondary)
                    }
                }

                // URL section
                Section {
                    TextField("https://example.com", text: $url)
                        .keyboardType(.URL)
                        .autocapitalization(.none)
                        .focused($focusedField, equals: .url)
                } header: {
                    Text("URL (Optional)")
                } footer: {
                    Text("Link to where this item can be purchased")
                }

                // Notes section
                Section {
                    TextField("Enter notes", text: $notes, axis: .vertical)
                        .lineLimit(3...6)
                        .focused($focusedField, equals: .notes)
                } header: {
                    Text("Notes (Optional)")
                } footer: {
                    Text("Any additional details about this item")
                }

                // Preview section
                if isValid {
                    Section {
                        VStack(alignment: .leading, spacing: 8) {
                            Text(name)
                                .font(.headline)

                            if let priceValue = priceDecimal {
                                Text(priceValue.currencyFormatted)
                                    .font(.title3)
                                    .fontWeight(.bold)
                                    .fontDesign(.monospaced)
                                    .foregroundStyle(.blue)
                            }

                            if !notes.isEmpty {
                                Text(notes)
                                    .font(.caption)
                                    .foregroundStyle(.secondary)
                            }

                            if !url.isEmpty {
                                Text(url)
                                    .font(.caption)
                                    .foregroundStyle(.blue)
                                    .lineLimit(1)
                            }
                        }
                    } header: {
                        Text("Preview")
                    }
                }
            }
            .navigationTitle("Add Wish List Item")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        dismiss()
                    }
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button("Add") {
                        Task {
                            await addItem()
                        }
                    }
                    .disabled(!isValid)
                }

                ToolbarItem(placement: .keyboard) {
                    Button("Done") {
                        focusedField = nil
                    }
                }
            }
            .disabled(viewModel.isProcessing)
            .overlay {
                if viewModel.isProcessing {
                    ProgressView("Adding item...")
                        .padding()
                        .background(Color(.systemBackground))
                        .clipShape(RoundedRectangle(cornerRadius: 12))
                        .shadow(radius: 4)
                }
            }
            .presentationDetents(isRegularWidth ? [.medium, .large] : [.large])
            .presentationDragIndicator(.visible)
        }
    }

    // MARK: - Computed Properties

    /// Convert price string to Decimal
    private var priceDecimal: Decimal? {
        guard !price.isEmpty else { return nil }
        return Decimal(string: price)
    }

    /// Check if form is valid
    private var isValid: Bool {
        guard !name.isEmpty else { return false }
        guard let priceValue = priceDecimal, priceValue > 0 else { return false }
        return true
    }

    // MARK: - Methods

    /// Add the wish list item
    private func addItem() async {
        guard let priceValue = priceDecimal else { return }

        let success = await viewModel.createItem(
            name: name,
            price: priceValue,
            url: url.isEmpty ? nil : url,
            notes: notes.isEmpty ? nil : notes
        )

        if success {
            dismiss()
        }
    }
}

// MARK: - Preview Provider

#Preview("Add Wish List Item") {
    AddWishListItemView(
        viewModel: WishListViewModel(childId: UUID())
    )
}
