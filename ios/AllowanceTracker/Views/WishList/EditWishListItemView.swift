import SwiftUI

/// Form for editing an existing wish list item
struct EditWishListItemView: View {

    // MARK: - Properties

    @Environment(\.dismiss) private var dismiss
    var viewModel: WishListViewModel
    let item: WishListItem

    @State private var name: String
    @State private var price: String
    @State private var url: String
    @State private var notes: String
    @FocusState private var focusedField: Field?

    // MARK: - Field enum

    private enum Field {
        case name, price, url, notes
    }

    // MARK: - Initialization

    init(viewModel: WishListViewModel, item: WishListItem) {
        self.viewModel = viewModel
        self.item = item

        _name = State(initialValue: item.name)
        _price = State(initialValue: String(describing: item.price))
        _url = State(initialValue: item.url ?? "")
        _notes = State(initialValue: item.notes ?? "")
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
                }

                // Notes section
                Section {
                    TextField("Enter notes", text: $notes, axis: .vertical)
                        .lineLimit(3...6)
                        .focused($focusedField, equals: .notes)
                } header: {
                    Text("Notes (Optional)")
                }
            }
            .navigationTitle("Edit Item")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        dismiss()
                    }
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button("Save") {
                        Task {
                            await updateItem()
                        }
                    }
                    .disabled(!isValid || !hasChanges)
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
                    ProgressView("Updating item...")
                        .padding()
                        .background(Color(.systemBackground))
                        .clipShape(RoundedRectangle(cornerRadius: 12))
                        .shadow(radius: 4)
                }
            }
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

    /// Check if any changes were made
    private var hasChanges: Bool {
        guard let priceValue = priceDecimal else { return false }

        return name != item.name ||
               priceValue != item.price ||
               url != (item.url ?? "") ||
               notes != (item.notes ?? "")
    }

    // MARK: - Methods

    /// Update the wish list item
    private func updateItem() async {
        guard let priceValue = priceDecimal else { return }

        let success = await viewModel.updateItem(
            id: item.id,
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

#Preview("Edit Wish List Item") {
    EditWishListItemView(
        viewModel: WishListViewModel(childId: UUID()),
        item: WishListItem(
            id: UUID(),
            childId: UUID(),
            name: "LEGO Star Wars Set",
            price: 49.99,
            url: "https://example.com",
            notes: "The big one with the Millennium Falcon",
            isPurchased: false,
            purchasedAt: nil,
            createdAt: Date(),
            canAfford: true
        )
    )
}
