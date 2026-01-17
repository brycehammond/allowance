import SwiftUI

/// View for creating a new gift link
@MainActor
struct CreateGiftLinkView: View {

    // MARK: - Properties

    let viewModel: GiftLinkViewModel
    let childName: String
    @Environment(\.dismiss) private var dismiss

    @State private var name = ""
    @State private var description = ""
    @State private var visibility: GiftLinkVisibility = .Minimal
    @State private var hasExpiration = false
    @State private var expiresAt = Calendar.current.date(byAdding: .month, value: 3, to: Date()) ?? Date()
    @State private var hasMaxUses = false
    @State private var maxUses = 10
    @State private var hasMinAmount = false
    @State private var minAmount = ""
    @State private var hasMaxAmount = false
    @State private var maxAmount = ""
    @State private var defaultOccasion: GiftOccasion?

    // MARK: - Body

    var body: some View {
        NavigationStack {
            Form {
                // Basic Info
                Section("Link Details") {
                    TextField("Link Name", text: $name)
                        .textInputAutocapitalization(.words)

                    TextField("Description (optional)", text: $description, axis: .vertical)
                        .lineLimit(2...4)
                }

                // Visibility
                Section {
                    Picker("Visibility", selection: $visibility) {
                        ForEach(GiftLinkVisibility.allCases, id: \.self) { option in
                            VStack(alignment: .leading) {
                                Text(option.displayName)
                                Text(option.description)
                                    .font(.caption)
                                    .foregroundStyle(.secondary)
                            }
                            .tag(option)
                        }
                    }
                } header: {
                    Text("Portal Visibility")
                } footer: {
                    Text("Controls what information family members can see when they visit the gift portal.")
                }

                // Amount Limits
                Section("Amount Limits") {
                    Toggle("Minimum Amount", isOn: $hasMinAmount)
                    if hasMinAmount {
                        TextField("Min Amount", text: $minAmount)
                            .keyboardType(.decimalPad)
                    }

                    Toggle("Maximum Amount", isOn: $hasMaxAmount)
                    if hasMaxAmount {
                        TextField("Max Amount", text: $maxAmount)
                            .keyboardType(.decimalPad)
                    }
                }

                // Usage Limits
                Section("Usage Limits") {
                    Toggle("Expiration Date", isOn: $hasExpiration)
                    if hasExpiration {
                        DatePicker("Expires", selection: $expiresAt, in: Date()..., displayedComponents: .date)
                    }

                    Toggle("Maximum Uses", isOn: $hasMaxUses)
                    if hasMaxUses {
                        Stepper("Max Uses: \(maxUses)", value: $maxUses, in: 1...100)
                    }
                }

                // Default Occasion
                Section {
                    Picker("Default Occasion", selection: $defaultOccasion) {
                        Text("None").tag(nil as GiftOccasion?)
                        ForEach(GiftOccasion.allCases, id: \.self) { occasion in
                            Label(occasion.displayName, systemImage: occasion.systemImage)
                                .tag(occasion as GiftOccasion?)
                        }
                    }
                } footer: {
                    Text("Pre-select an occasion for givers.")
                }
            }
            .navigationTitle("New Gift Link")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Create") {
                        Task { await createLink() }
                    }
                    .disabled(name.isEmpty || viewModel.isProcessing)
                }
            }
            .alert("Error", isPresented: .constant(viewModel.errorMessage != nil)) {
                Button("OK") { viewModel.clearError() }
            } message: {
                if let error = viewModel.errorMessage {
                    Text(error)
                }
            }
        }
    }

    // MARK: - Methods

    private func createLink() async {
        let success = await viewModel.createGiftLink(
            name: name,
            description: description.isEmpty ? nil : description,
            visibility: visibility,
            expiresAt: hasExpiration ? expiresAt : nil,
            maxUses: hasMaxUses ? maxUses : nil,
            minAmount: hasMinAmount ? Decimal(string: minAmount) : nil,
            maxAmount: hasMaxAmount ? Decimal(string: maxAmount) : nil,
            allowedOccasions: nil,
            defaultOccasion: defaultOccasion
        )

        if success {
            dismiss()
        }
    }
}

// MARK: - Preview

#Preview {
    CreateGiftLinkView(
        viewModel: GiftLinkViewModel(childId: UUID()),
        childName: "Emma"
    )
}
